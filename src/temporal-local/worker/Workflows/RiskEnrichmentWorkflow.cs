using B2B.RiskService.Activities;
using B2B.RiskService.Metrics;
using B2B.RiskService.Models;
using System.Text.Json.Serialization;
using Temporalio.Common;
using Temporalio.Workflows;

namespace B2B.RiskService.Workflows;

/// <summary>
/// Top-level workflow implementing the risk enrichment topology.
/// </summary>
[Workflow]
public sealed class RiskEnrichmentWorkflow
{
    private RiskEnrichmentStatus status = new()
    {
        State = "initialized",
        CurrentStep = "start",
        UpdatedAt = Workflow.UtcNow,
    };

    private bool cancelRequested;
    private string? cancelReason;

    /// <summary>
    /// Executes the full risk enrichment process.
    /// </summary>
    /// <param name="request">Workflow request.</param>
    /// <returns>Risk enrichment result.</returns>
    [WorkflowRun]
    public async Task<RiskEnrichmentResult> RunAsync(RiskEnrichmentRequest request)
    {
        try
        {
            SetState("running", "identify-company");
            var identified = await Workflow.ExecuteActivityAsync(
                (IdentifyCompanyActivity a) => a.RunAsync(request),
                CreateActivityOptions());
            if (cancelRequested)
            {
                return CreateCancelledResult(identified.CompanyId);
            }

            SetState("running", "enrich-dnb-data");
            var dnb = await Workflow.ExecuteActivityAsync(
                (EnrichDnbActivity a) => a.RunAsync(identified),
                CreateActivityOptions());
            if (cancelRequested)
            {
                return CreateCancelledResult(identified.CompanyId);
            }

            SetState("running", "calculate-risk");
            var risk = await Workflow.ExecuteChildWorkflowAsync(
                (CalculateRiskWorkflow wf) => wf.RunAsync(new CalculateRiskRequest
                {
                    CompanyId = identified.CompanyId,
                    Duns = identified.Duns,
                }),
                new ChildWorkflowOptions
                {
                    TaskQueue = "risk-enrichment",
                });
            if (cancelRequested)
            {
                return CreateCancelledResult(identified.CompanyId);
            }

            SetState("running", "document-pdf");
            var pdfInput = identified with
            {
                CompanyName = dnb.LegalName,
            };
            var documentId = await Workflow.ExecuteActivityAsync(
                (GeneratePdfActivity a) => a.RunAsync(pdfInput, risk.RiskClass ?? "unknown"),
                CreateActivityOptions());
            if (cancelRequested)
            {
                return CreateCancelledResult(identified.CompanyId);
            }

            var result = new RiskEnrichmentResult
            {
                CompanyId = identified.CompanyId,
                RiskClass = risk.RiskClass ?? "medium",
                Status = "completed",
                DocumentId = documentId,
            };

            SetState("running", "publish-result");
            _ = await Workflow.ExecuteActivityAsync(
                (PublishRiskResultActivity a) => a.RunAsync(result),
                CreateActivityOptions());

            SetState("completed", "end");
            return result;
        }
        finally
        {
            WorkerMetrics.RecordWorkflowExecution(nameof(RiskEnrichmentWorkflow));
        }
    }

    /// <summary>
    /// Returns current workflow status.
    /// </summary>
    /// <returns>Status projection.</returns>
    [WorkflowQuery("GetStatus")]
    public RiskEnrichmentStatus GetStatus() => status;

    /// <summary>
    /// Signals graceful cancellation.
    /// </summary>
    /// <param name="signal">Cancellation signal payload.</param>
    [WorkflowSignal("Cancel")]
    public Task CancelAsync(CancelSignal signal)
    {
        cancelRequested = true;
        cancelReason = signal.Reason;
        SetState("cancelled", status.CurrentStep ?? "running");
        return Task.CompletedTask;
    }

    private static ActivityOptions CreateActivityOptions() =>
        new()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(20),
            RetryPolicy = new RetryPolicy
            {
                MaximumAttempts = 5,
                InitialInterval = TimeSpan.FromMilliseconds(500),
                BackoffCoefficient = 2.0f,
                MaximumInterval = TimeSpan.FromSeconds(10),
            },
        };

    private RiskEnrichmentResult CreateCancelledResult(string companyId) =>
        new()
        {
            CompanyId = companyId,
            RiskClass = "unknown",
            Status = $"cancelled{(string.IsNullOrWhiteSpace(cancelReason) ? string.Empty : $": {cancelReason}")}",
            DocumentId = null,
        };

    private void SetState(string state, string step) =>
        status = status with
        {
            State = state,
            CurrentStep = step,
            UpdatedAt = Workflow.UtcNow,
        };
}

/// <summary>
/// Signal payload for cancellation.
/// </summary>
public sealed record CancelSignal
{
    /// <summary>
    /// Optional cancellation reason.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}

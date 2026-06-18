using B2B.RiskService.Activities;
using B2B.RiskService.Metrics;
using B2B.RiskService.Models;
using Temporalio.Common;
using Temporalio.Workflows;

namespace B2B.RiskService.Workflows;

/// <summary>
/// Child workflow for external compliance checks.
/// </summary>
[Workflow]
public sealed class ExternalChecksWorkflow
{
    /// <summary>
    /// Runs sanctions, PEP, and adverse media checks.
    /// </summary>
    /// <param name="request">External checks input.</param>
    /// <returns>Aggregated external checks result.</returns>
    [WorkflowRun]
    public async Task<ExternalChecksResult> RunAsync(ExternalChecksRequest request)
    {
        var startedAt = Workflow.UtcNow;
        try
        {
            var sanctionsHit = await Workflow.ExecuteActivityAsync(
                (SanctionsCheckActivity a) => a.RunAsync(request),
                CreateActivityOptions());
            var pepHit = await Workflow.ExecuteActivityAsync(
                (PoliticallyExposedPersonCheckActivity a) => a.RunAsync(request),
                CreateActivityOptions());
            var adverseMediaHit = await Workflow.ExecuteActivityAsync(
                (AdverseMediaCheckActivity a) => a.RunAsync(request),
                CreateActivityOptions());

            return new ExternalChecksResult
            {
                SanctionsHit = sanctionsHit,
                PepHit = pepHit,
                AdverseMediaHit = adverseMediaHit,
            };
        }
        finally
        {
            WorkerMetrics.RecordWorkflowExecution(nameof(ExternalChecksWorkflow));
            WorkerMetrics.RecordWorkflowLatency(nameof(ExternalChecksWorkflow), (Workflow.UtcNow - startedAt).TotalSeconds);
        }
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
}

using B2B.RiskService.Activities;
using B2B.RiskService.Metrics;
using B2B.RiskService.Models;
using Temporalio.Common;
using Temporalio.Workflows;

namespace B2B.RiskService.Workflows;

/// <summary>
/// Child workflow for risk calculation.
/// </summary>
[Workflow]
public sealed class CalculateRiskWorkflow
{
    /// <summary>
    /// Executes signal collection, scoring, external checks, and aggregation.
    /// </summary>
    /// <param name="request">Calculate risk input.</param>
    /// <returns>Risk score result.</returns>
    [WorkflowRun]
    public async Task<RiskScoreResult> RunAsync(CalculateRiskRequest request)
    {
        try
        {
            var paymentHistoryTask = Workflow.ExecuteActivityAsync(
                (FetchPaymentHistoryActivity a) => a.RunAsync(request.CompanyId),
                CreateActivityOptions());
            var creditLimitTask = Workflow.ExecuteActivityAsync(
                (FetchCreditLimitActivity a) => a.RunAsync(request.CompanyId),
                CreateActivityOptions());

            await Task.WhenAll(paymentHistoryTask, creditLimitTask);

            var normalisedSignals = await Workflow.ExecuteActivityAsync(
                (NormaliseRiskSignalsActivity a) => a.RunAsync(
                    new NormaliseRiskSignalsRequest(paymentHistoryTask.Result, creditLimitTask.Result)),
                CreateActivityOptions());

            var score = await Workflow.ExecuteActivityAsync(
                (ScoreCompanyRiskActivity a) => a.RunAsync(normalisedSignals),
                CreateActivityOptions());

            var externalChecks = await Workflow.ExecuteChildWorkflowAsync(
                (ExternalChecksWorkflow wf) => wf.RunAsync(new ExternalChecksRequest
                {
                    CompanyId = request.CompanyId,
                    Country = "DE",
                }),
                new ChildWorkflowOptions
                {
                    TaskQueue = "risk-enrichment",
                });

            return await Workflow.ExecuteActivityAsync(
                (AggregateRiskScoreActivity a) => a.RunAsync(
                    new AggregateRiskScoreRequest(score, externalChecks)),
                CreateActivityOptions());
        }
        finally
        {
            WorkerMetrics.RecordWorkflowExecution(nameof(CalculateRiskWorkflow));
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

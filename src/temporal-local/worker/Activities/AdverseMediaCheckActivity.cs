using B2B.RiskService.Models;
using B2B.RiskService.Metrics;
using Temporalio.Activities;

namespace B2B.RiskService.Activities;

/// <summary>
/// Performs mock adverse media checks.
/// </summary>
public sealed class AdverseMediaCheckActivity
{
    /// <summary>
    /// Returns adverse media hit with ~8% probability.
    /// </summary>
    /// <param name="request">External checks request.</param>
    /// <returns>True if adverse media hit detected.</returns>
    [Activity("AdverseMediaCheckActivity")]
    public async Task<bool> RunAsync(ExternalChecksRequest request)
    {
        _ = request;
        ActivityExecutionHelper.MaybeFail(nameof(AdverseMediaCheckActivity));
        await ActivityExecutionHelper.DelayAsync(800, 1200);
        var hit = Random.Shared.NextDouble() < 0.08d;
        WorkerMetrics.RecordActivityExecution(nameof(AdverseMediaCheckActivity));
        return hit;
    }
}

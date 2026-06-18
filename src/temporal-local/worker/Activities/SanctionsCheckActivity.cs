using B2B.RiskService.Models;
using B2B.RiskService.Metrics;
using System.Diagnostics;
using Temporalio.Activities;

namespace B2B.RiskService.Activities;

/// <summary>
/// Performs mock sanctions screening.
/// </summary>
public sealed class SanctionsCheckActivity
{
    /// <summary>
    /// Returns sanctions hit with ~5% probability.
    /// </summary>
    /// <param name="request">External checks request.</param>
    /// <returns>True if sanctions hit detected.</returns>
    [Activity("SanctionsCheckActivity")]
    public async Task<bool> RunAsync(ExternalChecksRequest request)
    {
        var sw = Stopwatch.StartNew();
        _ = request;
        ActivityExecutionHelper.MaybeFail(nameof(SanctionsCheckActivity));
        await ActivityExecutionHelper.DelayAsync(1000, 1500);
        var hit = Random.Shared.NextDouble() < 0.05d;
        WorkerMetrics.RecordActivityExecution(nameof(SanctionsCheckActivity), sw.Elapsed.TotalSeconds);
        return hit;
    }
}

using B2B.RiskService.Models;
using B2B.RiskService.Metrics;
using System.Diagnostics;
using Temporalio.Activities;

namespace B2B.RiskService.Activities;

/// <summary>
/// Simulates publishing final risk result to downstream systems.
/// </summary>
public sealed class PublishRiskResultActivity
{
    /// <summary>
    /// Publishes result and returns success flag.
    /// </summary>
    /// <param name="result">Result to publish.</param>
    /// <returns>Always true for simulation.</returns>
    [Activity("PublishRiskResultActivity")]
    public async Task<bool> RunAsync(RiskEnrichmentResult result)
    {
        var sw = Stopwatch.StartNew();
        _ = result;
        await ActivityExecutionHelper.DelayAsync(300, 800);
        WorkerMetrics.RecordActivityExecution(nameof(PublishRiskResultActivity), sw.Elapsed.TotalSeconds);
        return true;
    }
}

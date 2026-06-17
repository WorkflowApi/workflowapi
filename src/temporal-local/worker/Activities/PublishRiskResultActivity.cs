using B2B.RiskService.Models;
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
        _ = result;
        await ActivityExecutionHelper.DelayAsync(300, 800);
        return true;
    }
}

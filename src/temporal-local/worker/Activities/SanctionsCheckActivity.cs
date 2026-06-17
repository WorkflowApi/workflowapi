using B2B.RiskService.Models;
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
        _ = request;
        ActivityExecutionHelper.MaybeFail(nameof(SanctionsCheckActivity));
        await ActivityExecutionHelper.DelayAsync(1000, 1500);
        return Random.Shared.NextDouble() < 0.05d;
    }
}

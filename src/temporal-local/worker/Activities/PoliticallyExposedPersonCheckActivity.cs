using B2B.RiskService.Models;
using Temporalio.Activities;

namespace B2B.RiskService.Activities;

/// <summary>
/// Performs mock politically exposed person checks.
/// </summary>
public sealed class PoliticallyExposedPersonCheckActivity
{
    /// <summary>
    /// Returns PEP hit with ~3% probability.
    /// </summary>
    /// <param name="request">External checks request.</param>
    /// <returns>True if PEP hit detected.</returns>
    [Activity("PoliticallyExposedPersonCheckActivity")]
    public async Task<bool> RunAsync(ExternalChecksRequest request)
    {
        _ = request;
        ActivityExecutionHelper.MaybeFail(nameof(PoliticallyExposedPersonCheckActivity));
        await ActivityExecutionHelper.DelayAsync(1000, 1500);
        return Random.Shared.NextDouble() < 0.03d;
    }
}

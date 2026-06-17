using B2B.RiskService.Models;
using Temporalio.Activities;

namespace B2B.RiskService.Activities;

/// <summary>
/// Resolves company identifiers and fills missing DUNS data.
/// </summary>
public sealed class IdentifyCompanyActivity
{
    /// <summary>
    /// Enriches company identity details.
    /// </summary>
    /// <param name="request">Incoming request.</param>
    /// <returns>Enriched request payload.</returns>
    [Activity("IdentifyCompanyActivity")]
    public async Task<RiskEnrichmentRequest> RunAsync(RiskEnrichmentRequest request)
    {
        await ActivityExecutionHelper.DelayAsync(500, 1000);

        return request with
        {
            Duns = string.IsNullOrWhiteSpace(request.Duns)
                ? Random.Shared.Next(100000000, 999999999).ToString()
                : request.Duns,
        };
    }
}

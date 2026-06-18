using B2B.RiskService.Models;
using B2B.RiskService.Metrics;
using System.Diagnostics;
using Temporalio.Activities;

namespace B2B.RiskService.Activities;

/// <summary>
/// Simulates D&amp;B enrichment bridge interaction.
/// </summary>
public sealed class EnrichDnbActivity
{
    /// <summary>
    /// Performs D&amp;B enrichment and returns legal profile details.
    /// </summary>
    /// <param name="request">Current enrichment request.</param>
    /// <returns>Mock D&amp;B enrichment details.</returns>
    [Activity("EnrichDnbActivity")]
    public async Task<DnbEnrichmentResult> RunAsync(RiskEnrichmentRequest request)
    {
        var sw = Stopwatch.StartNew();
        ActivityExecutionHelper.MaybeFail(nameof(EnrichDnbActivity));
        await ActivityExecutionHelper.DelayAsync(1000, 2000);

        var legalName = string.IsNullOrWhiteSpace(request.CompanyName)
            ? $"Company {request.CompanyId[..Math.Min(request.CompanyId.Length, 8)].ToUpperInvariant()}"
            : request.CompanyName;

        var creditLimit = Random.Shared.Next(100_000, 5_000_000);
        WorkerMetrics.RecordActivityExecution(nameof(EnrichDnbActivity), sw.Elapsed.TotalSeconds);

        return new DnbEnrichmentResult(legalName, creditLimit);
    }
}

/// <summary>
/// Result payload for D&amp;B enrichment simulation.
/// </summary>
/// <param name="LegalName">Resolved legal entity name.</param>
/// <param name="CreditLimit">Mock credit limit.</param>
public sealed record DnbEnrichmentResult(string LegalName, decimal CreditLimit);

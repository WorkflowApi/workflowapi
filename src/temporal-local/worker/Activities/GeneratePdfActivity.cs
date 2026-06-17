using B2B.RiskService.Models;
using B2B.RiskService.Metrics;
using Temporalio.Activities;

namespace B2B.RiskService.Activities;

/// <summary>
/// Simulates document generation bridge.
/// </summary>
public sealed class GeneratePdfActivity
{
    /// <summary>
    /// Generates mock document identifier.
    /// </summary>
    /// <param name="request">Request to document.</param>
    /// <param name="riskClass">Computed risk class.</param>
    /// <returns>Document identifier.</returns>
    [Activity("GeneratePdfActivity")]
    public async Task<string> RunAsync(RiskEnrichmentRequest request, string riskClass)
    {
        _ = request;
        _ = riskClass;
        ActivityExecutionHelper.MaybeFail(nameof(GeneratePdfActivity));
        await ActivityExecutionHelper.DelayAsync(1500, 2000);
        var documentId = Guid.NewGuid().ToString();
        WorkerMetrics.RecordActivityExecution(nameof(GeneratePdfActivity));
        return documentId;
    }
}

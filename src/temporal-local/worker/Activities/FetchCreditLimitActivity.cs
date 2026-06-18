using Temporalio.Activities;
using B2B.RiskService.Metrics;
using System.Diagnostics;

namespace B2B.RiskService.Activities;

/// <summary>
/// Fetches mock credit limit signal.
/// </summary>
public sealed class FetchCreditLimitActivity
{
    /// <summary>
    /// Returns random credit limit between 10k and 1M.
    /// </summary>
    /// <param name="companyId">Company identifier.</param>
    /// <returns>Credit limit value.</returns>
    [Activity("FetchCreditLimitActivity")]
    public async Task<decimal> RunAsync(string? companyId)
    {
        var sw = Stopwatch.StartNew();
        _ = companyId;
        await ActivityExecutionHelper.DelayAsync(500, 1000);
        var limit = Random.Shared.Next(10_000, 1_000_001);
        WorkerMetrics.RecordActivityExecution(nameof(FetchCreditLimitActivity), sw.Elapsed.TotalSeconds);
        return limit;
    }
}

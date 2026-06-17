using Temporalio.Activities;

namespace B2B.RiskService.Activities;

/// <summary>
/// Fetches mock payment history signals.
/// </summary>
public sealed class FetchPaymentHistoryActivity
{
    /// <summary>
    /// Returns random payment history data.
    /// </summary>
    /// <param name="companyId">Company identifier.</param>
    /// <returns>Payment history signals.</returns>
    [Activity("FetchPaymentHistoryActivity")]
    public async Task<PaymentHistory> RunAsync(string? companyId)
    {
        _ = companyId;
        await ActivityExecutionHelper.DelayAsync(500, 1000);
        var count = Random.Shared.Next(3, 40);
        var amount = Math.Round((decimal)Random.Shared.NextDouble() * 2_000_000m, 2, MidpointRounding.AwayFromZero);
        return new PaymentHistory(count, amount);
    }
}

/// <summary>
/// Payment history snapshot used for scoring.
/// </summary>
/// <param name="PaymentCount">Number of recent payments observed.</param>
/// <param name="TotalAmount">Total recent payment amount.</param>
public sealed record PaymentHistory(int PaymentCount, decimal TotalAmount);

using Temporalio.Activities;
using B2B.RiskService.Metrics;
using System.Diagnostics;

namespace B2B.RiskService.Activities;

/// <summary>
/// Normalises raw risk signals into 0..1 ranges.
/// </summary>
public sealed class NormaliseRiskSignalsActivity
{
    /// <summary>
    /// Normalises payment and credit limit data.
    /// </summary>
    /// <param name="request">Raw signal payload.</param>
    /// <returns>Normalised signal values.</returns>
    [Activity("NormaliseRiskSignalsActivity")]
    public async Task<NormalisedSignals> RunAsync(NormaliseRiskSignalsRequest request)
    {
        var sw = Stopwatch.StartNew();
        await ActivityExecutionHelper.DelayAsync(200, 500);

        var paymentBehaviour = 1d - Math.Clamp(request.PaymentHistory.PaymentCount / 40d, 0d, 1d);
        var creditExposure = Math.Clamp((double)(request.CreditLimit / 1_000_000m), 0d, 1d);
        var externalRisk = Math.Clamp((double)(request.PaymentHistory.TotalAmount / 2_000_000m), 0d, 1d);

        var signals = new NormalisedSignals(paymentBehaviour, creditExposure, externalRisk);
        WorkerMetrics.RecordActivityExecution(nameof(NormaliseRiskSignalsActivity), sw.Elapsed.TotalSeconds);
        return signals;
    }
}

/// <summary>
/// Request payload for signal normalisation.
/// </summary>
/// <param name="PaymentHistory">Fetched payment history.</param>
/// <param name="CreditLimit">Fetched credit limit.</param>
public sealed record NormaliseRiskSignalsRequest(PaymentHistory PaymentHistory, decimal CreditLimit);

/// <summary>
/// Normalised risk signals.
/// </summary>
/// <param name="PaymentBehaviour">Normalised payment behaviour risk.</param>
/// <param name="CreditExposure">Normalised credit exposure risk.</param>
/// <param name="ExternalRisk">Normalised external risk proxy.</param>
public sealed record NormalisedSignals(double PaymentBehaviour, double CreditExposure, double ExternalRisk);

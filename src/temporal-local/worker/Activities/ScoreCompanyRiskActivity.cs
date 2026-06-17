using Temporalio.Activities;

namespace B2B.RiskService.Activities;

/// <summary>
/// Computes a risk score from normalised risk signals.
/// </summary>
public sealed class ScoreCompanyRiskActivity
{
    /// <summary>
    /// Calculates risk score (0..100).
    /// </summary>
    /// <param name="signals">Normalised signals.</param>
    /// <returns>Numeric risk score.</returns>
    [Activity("ScoreCompanyRiskActivity")]
    public async Task<double> RunAsync(NormalisedSignals signals)
    {
        await ActivityExecutionHelper.DelayAsync(800, 1500);

        var composite =
            (signals.CreditExposure * 0.35d) +
            (signals.PaymentBehaviour * 0.35d) +
            (signals.ExternalRisk * 0.30d);

        var noisy = composite + ((Random.Shared.NextDouble() - 0.5d) * 0.15d);
        var scaled = Math.Clamp(noisy, 0d, 1d) * 100d;
        return Math.Round(scaled, 2, MidpointRounding.AwayFromZero);
    }
}

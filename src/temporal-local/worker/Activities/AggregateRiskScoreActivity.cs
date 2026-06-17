using B2B.RiskService.Models;
using Temporalio.Activities;

namespace B2B.RiskService.Activities;

/// <summary>
/// Aggregates score and external checks into risk class.
/// </summary>
public sealed class AggregateRiskScoreActivity
{
    /// <summary>
    /// Maps score to class low/medium/high.
    /// </summary>
    /// <param name="request">Aggregation request.</param>
    /// <returns>Final risk score result.</returns>
    [Activity("AggregateRiskScoreActivity")]
    public async Task<RiskScoreResult> RunAsync(AggregateRiskScoreRequest request)
    {
        await ActivityExecutionHelper.DelayAsync(300, 600);

        var adjusted = request.BaseScore;
        if (request.ExternalChecks.SanctionsHit)
        {
            adjusted = Math.Max(adjusted, 85d);
        }

        if (request.ExternalChecks.PepHit || request.ExternalChecks.AdverseMediaHit)
        {
            adjusted = Math.Max(adjusted, 70d);
        }

        var riskClass = adjusted switch
        {
            < 30d => "low",
            <= 70d => "medium",
            _ => "high",
        };

        return new RiskScoreResult
        {
            Score = Math.Round(adjusted, 2, MidpointRounding.AwayFromZero),
            RiskClass = riskClass,
        };
    }
}

/// <summary>
/// Input payload for score aggregation.
/// </summary>
/// <param name="BaseScore">Base numeric score.</param>
/// <param name="ExternalChecks">External compliance results.</param>
public sealed record AggregateRiskScoreRequest(double BaseScore, ExternalChecksResult ExternalChecks);

using System.Text.Json.Serialization;

namespace B2B.RiskService.Models;

/// <summary>
/// Computed risk scoring result.
/// </summary>
public record RiskScoreResult
{
    /// <summary>
    /// Numeric score in the range 0..100.
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; init; }

    /// <summary>
    /// Derived risk class (low, medium, high).
    /// </summary>
    [JsonPropertyName("riskClass")]
    public string? RiskClass { get; init; }
}

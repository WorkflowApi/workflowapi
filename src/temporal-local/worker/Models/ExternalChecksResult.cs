using System.Text.Json.Serialization;

namespace B2B.RiskService.Models;

/// <summary>
/// External compliance and watchlist check outcomes.
/// </summary>
public record ExternalChecksResult
{
    /// <summary>
    /// Indicates sanctions list hit.
    /// </summary>
    [JsonPropertyName("sanctionsHit")]
    public bool SanctionsHit { get; init; }

    /// <summary>
    /// Indicates politically exposed person hit.
    /// </summary>
    [JsonPropertyName("pepHit")]
    public bool PepHit { get; init; }

    /// <summary>
    /// Indicates adverse media hit.
    /// </summary>
    [JsonPropertyName("adverseMediaHit")]
    public bool AdverseMediaHit { get; init; }
}

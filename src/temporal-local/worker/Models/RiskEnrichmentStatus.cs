using System.Text.Json.Serialization;

namespace B2B.RiskService.Models;

/// <summary>
/// Query response describing current workflow status.
/// </summary>
public record RiskEnrichmentStatus
{
    /// <summary>
    /// Current high-level state (for example: running, completed, cancelled).
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>
    /// Current topology step name.
    /// </summary>
    [JsonPropertyName("currentStep")]
    public string? CurrentStep { get; init; }

    /// <summary>
    /// Timestamp of most recent state update.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}

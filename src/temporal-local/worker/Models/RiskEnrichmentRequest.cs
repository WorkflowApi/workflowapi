using System.Text.Json.Serialization;

namespace B2B.RiskService.Models;

/// <summary>
/// Input for the risk enrichment workflow.
/// </summary>
public record RiskEnrichmentRequest
{
    /// <summary>
    /// Unique company identifier.
    /// </summary>
    [JsonPropertyName("companyId")]
    public required string CompanyId { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code.
    /// </summary>
    [JsonPropertyName("country")]
    public required string Country { get; init; }

    /// <summary>
    /// Optional company display name.
    /// </summary>
    [JsonPropertyName("companyName")]
    public string? CompanyName { get; init; }

    /// <summary>
    /// Optional D&amp;B DUNS number.
    /// </summary>
    [JsonPropertyName("duns")]
    public string? Duns { get; init; }

    /// <summary>
    /// Optional reason for the risk enrichment request.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}

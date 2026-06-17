using System.Text.Json.Serialization;

namespace B2B.RiskService.Models;

/// <summary>
/// Input payload for external compliance checks.
/// </summary>
public record ExternalChecksRequest
{
    /// <summary>
    /// Company identifier.
    /// </summary>
    [JsonPropertyName("companyId")]
    public string? CompanyId { get; init; }

    /// <summary>
    /// Country code for jurisdiction-specific checks.
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; init; }
}

using System.Text.Json.Serialization;

namespace B2B.RiskService.Models;

/// <summary>
/// Input for the calculate risk child workflow.
/// </summary>
public record CalculateRiskRequest
{
    /// <summary>
    /// Company identifier.
    /// </summary>
    [JsonPropertyName("companyId")]
    public string? CompanyId { get; init; }

    /// <summary>
    /// Optional DUNS identifier.
    /// </summary>
    [JsonPropertyName("duns")]
    public string? Duns { get; init; }
}

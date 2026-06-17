using System.Text.Json.Serialization;

namespace B2B.RiskService.Models;

/// <summary>
/// Final output of a risk enrichment workflow execution.
/// </summary>
public record RiskEnrichmentResult
{
    /// <summary>
    /// Company identifier.
    /// </summary>
    [JsonPropertyName("companyId")]
    public required string CompanyId { get; init; }

    /// <summary>
    /// Computed risk class.
    /// </summary>
    [JsonPropertyName("riskClass")]
    public required string RiskClass { get; init; }

    /// <summary>
    /// Workflow completion status.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Optional generated PDF document identifier.
    /// </summary>
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; init; }
}

using B2B.RiskService.Models;

namespace B2B.RiskService.Simulation;

/// <summary>
/// Provides randomised valid workflow input payloads.
/// </summary>
public static class MockDataGenerator
{
    private static readonly string[] Countries = ["DE", "US", "GB", "FR", "NL", "CH"];

    private static readonly string[] CompanyNames =
    [
        "Nordic Dynamics AG",
        "Riverstone Industries Ltd",
        "Helios Manufacturing GmbH",
        "Pioneer Credit Systems BV",
        "Aster Capital Holdings SA",
        "BluePeak Logistics GmbH",
        "Marlin Energy Trading Ltd",
        "Summit Retail Partners Inc",
        "Greenfield Components AG",
        "Silverline Procurement GmbH",
        "Arcadia Commerce Group SA",
        "Atlas Risk Solutions BV",
        "Meridian Automotive Ltd",
        "Crestpoint Equipment GmbH",
        "Orchid Wholesale Services AG",
        "Keystone Digital Ventures BV",
        "Lighthouse Global Markets Ltd",
        "Redwood Industrial Systems SA",
        "Everbridge Consumer Goods GmbH",
        "Union Harbor Finance AG",
    ];

    /// <summary>
    /// Creates a random valid risk enrichment request.
    /// </summary>
    /// <returns>Random request instance.</returns>
    public static RiskEnrichmentRequest CreateRiskEnrichmentRequest()
    {
        var companyId = Guid.NewGuid().ToString("N");
        var country = Countries[Random.Shared.Next(0, Countries.Length)];
        var companyName = CompanyNames[Random.Shared.Next(0, CompanyNames.Length)];

        return new RiskEnrichmentRequest
        {
            CompanyId = companyId,
            Country = country,
            CompanyName = companyName,
            Reason = "simulated-workload",
        };
    }
}

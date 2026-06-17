# Data Model: Local Temporal Environment

**Date**: 2026-06-17 | **Feature**: 001-local-temporal-environment

## Workflow Types

### RiskEnrichmentWorkflow

The top-level workflow implementing the risk enrichment pipeline.

| Property | Type | Description |
|----------|------|-------------|
| Input | `RiskEnrichmentRequest` | Company to enrich |
| Output | `RiskEnrichmentResult` | Final risk assessment |
| Signal | `Cancel` → `CancelSignal` | Graceful cancellation |
| Signal | `ManualReviewBypassed` → `ManualReviewBypassedSignal` | Bypass manual review |
| Query | `GetStatus` → `RiskEnrichmentStatus` | Current workflow step |

**Topology**: start → identify-company → enrich-dnb-data → calculate-risk → document-pdf → publish-result → end

### CalculateRiskWorkflow (Child)

Nested child workflow for risk scoring.

| Property | Type | Description |
|----------|------|-------------|
| Input | `CalculateRiskRequest` | Company ID + DUNS |
| Output | `RiskScoreResult` | Computed score and class |

**Topology**: start → fetch-signals (subflow) → score-engine → external-checks → aggregate → end

### ExternalChecksWorkflow (Child)

Nested child workflow for compliance checks.

| Property | Type | Description |
|----------|------|-------------|
| Input | `ExternalChecksRequest` | Company ID + country |
| Output | `ExternalChecksResult` | Hit flags |

**Topology**: start → sanctions-check → pep-check → adverse-media → end

## Models (C# Records)

### RiskEnrichmentRequest

```csharp
/// <summary>Input for the risk enrichment workflow.</summary>
public record RiskEnrichmentRequest
{
    /// <summary>Unique company identifier.</summary>
    public required string CompanyId { get; init; }

    /// <summary>ISO country code.</summary>
    public required string Country { get; init; }

    /// <summary>Human-readable company name.</summary>
    public string? CompanyName { get; init; }

    /// <summary>D&amp;B DUNS number, if known.</summary>
    public string? Duns { get; init; }

    /// <summary>Reason for enrichment request.</summary>
    public string? Reason { get; init; }
}
```

### RiskEnrichmentResult

```csharp
/// <summary>Final output of the risk enrichment workflow.</summary>
public record RiskEnrichmentResult
{
    /// <summary>Company that was enriched.</summary>
    public required string CompanyId { get; init; }

    /// <summary>Computed risk class: low, medium, or high.</summary>
    public required string RiskClass { get; init; }

    /// <summary>Workflow completion status.</summary>
    public required string Status { get; init; }

    /// <summary>Generated PDF document identifier.</summary>
    public string? DocumentId { get; init; }
}
```

### RiskEnrichmentStatus

```csharp
/// <summary>Current status returned by GetStatus query.</summary>
public record RiskEnrichmentStatus
{
    /// <summary>Workflow state: running, completed, cancelled, failed.</summary>
    public string? State { get; init; }

    /// <summary>Current topology node being executed.</summary>
    public string? CurrentStep { get; init; }

    /// <summary>Timestamp of last state transition.</summary>
    public DateTime? UpdatedAt { get; init; }
}
```

### CalculateRiskRequest

```csharp
/// <summary>Input for the calculate-risk child workflow.</summary>
public record CalculateRiskRequest
{
    /// <summary>Company identifier.</summary>
    public string? CompanyId { get; init; }

    /// <summary>D&amp;B DUNS number.</summary>
    public string? Duns { get; init; }
}
```

### RiskScoreResult

```csharp
/// <summary>Output of the risk scoring workflow.</summary>
public record RiskScoreResult
{
    /// <summary>Numeric risk score (0-100).</summary>
    public double Score { get; init; }

    /// <summary>Risk class derived from score: low (&lt;30), medium (30-70), high (&gt;70).</summary>
    public string? RiskClass { get; init; }
}
```

### ExternalChecksRequest

```csharp
/// <summary>Input for the external compliance checks workflow.</summary>
public record ExternalChecksRequest
{
    /// <summary>Company identifier.</summary>
    public string? CompanyId { get; init; }

    /// <summary>ISO country code for jurisdiction-specific checks.</summary>
    public string? Country { get; init; }
}
```

### ExternalChecksResult

```csharp
/// <summary>Results from external compliance checks.</summary>
public record ExternalChecksResult
{
    /// <summary>Whether a sanctions list hit was found.</summary>
    public bool SanctionsHit { get; init; }

    /// <summary>Whether a politically exposed person match was found.</summary>
    public bool PepHit { get; init; }

    /// <summary>Whether adverse media mentions were found.</summary>
    public bool AdverseMediaHit { get; init; }
}
```

### CancelSignal

```csharp
/// <summary>Signal payload for workflow cancellation.</summary>
public record CancelSignal
{
    /// <summary>Reason for cancellation.</summary>
    public string? Reason { get; init; }
}
```

## Activities

### Activity Inventory

| Activity | Workflow | Failure Sim | Delay Range | Description |
|----------|---------|-------------|-------------|-------------|
| `IdentifyCompanyActivity` | RiskEnrichment | No | 500-1000ms | Resolves company identifiers |
| `EnrichDnbActivity` | RiskEnrichment | **Yes (~10%)** | 1000-2000ms | Simulates D&B bridge call |
| `GeneratePdfActivity` | RiskEnrichment | **Yes (~10%)** | 1500-2000ms | Simulates PDF generation bridge |
| `PublishRiskResultActivity` | RiskEnrichment | No | 300-800ms | Publishes final result |
| `FetchPaymentHistoryActivity` | CalculateRisk | No | 500-1000ms | Fetches payment signals |
| `FetchCreditLimitActivity` | CalculateRisk | No | 500-1000ms | Fetches credit limit signals |
| `NormaliseRiskSignalsActivity` | CalculateRisk | No | 200-500ms | Normalises collected signals |
| `ScoreCompanyRiskActivity` | CalculateRisk | No | 800-1500ms | Runs scoring engine |
| `AggregateRiskScoreActivity` | CalculateRisk | No | 300-600ms | Aggregates final score |
| `SanctionsCheckActivity` | ExternalChecks | **Yes (~10%)** | 1000-1500ms | Sanctions list lookup |
| `PoliticallyExposedPersonCheckActivity` | ExternalChecks | **Yes (~10%)** | 1000-1500ms | PEP database check |
| `AdverseMediaCheckActivity` | ExternalChecks | **Yes (~10%)** | 800-1200ms | Media screening |

### Activity Return Types

| Activity | Returns | Mock Value Strategy |
|----------|---------|-------------------|
| `IdentifyCompanyActivity` | `RiskEnrichmentRequest` (enriched) | Adds generated DUNS if missing |
| `EnrichDnbActivity` | `DnbEnrichmentResult` | Mock legal name + random credit limit |
| `GeneratePdfActivity` | `string` (documentId) | Random GUID |
| `PublishRiskResultActivity` | `bool` | Always `true` |
| `FetchPaymentHistoryActivity` | `PaymentHistory` | Random payment count/amounts |
| `FetchCreditLimitActivity` | `decimal` | Random 10,000-1,000,000 |
| `NormaliseRiskSignalsActivity` | `NormalisedSignals` | Normalised 0-1 values |
| `ScoreCompanyRiskActivity` | `double` (score 0-100) | Weighted random from signals |
| `AggregateRiskScoreActivity` | `RiskScoreResult` | Score → risk class mapping |
| `SanctionsCheckActivity` | `bool` (hit) | ~5% true |
| `PoliticallyExposedPersonCheckActivity` | `bool` (hit) | ~3% true |
| `AdverseMediaCheckActivity` | `bool` (hit) | ~8% true |

## State Transitions

### RiskEnrichmentWorkflow States

```
initialized → identifying → enriching → calculating-risk → generating-pdf → publishing → completed
                                                                                       → cancelled (via Cancel signal)
```

Each state is tracked via the `currentStep` field in `RiskEnrichmentStatus` and exposed through the `GetStatus` query.

## Project Configuration (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <RootNamespace>WorkflowApi.Temporal.RiskWorker</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Temporalio" Version="1.15.0" />
    <PackageReference Include="Temporalio.Extensions.Hosting" Version="1.15.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
  </ItemGroup>
</Project>
```

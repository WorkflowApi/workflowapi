# 02 — .NET Implementation Design

Version: **0.1 draft**  
Audience: .NET agents, package design agents, source-generation agents, test agents  
Status: proposal

---

## 1. Purpose

This document defines the proposed .NET implementation for WorkflowAPI.

The design intentionally follows the modern ASP.NET Core OpenAPI model:

```csharp
builder.Services.AddOpenApi();
app.MapOpenApi();
app.MapScalarApiReference();
```

WorkflowAPI should feel similarly native:

```csharp
builder.Services.AddWorkflowApi("v1")
    .ScanFromAssemblyOf<RiskEnrichmentWorkflow>()
    .WithTemporal(options =>
    {
        options.Namespace = "B2B.RiskService";
        options.TaskQueue = "risk-service";
    });

app.MapWorkflowApi();
app.MapWorkflowApiReference();
```

---

## 2. Package architecture

Recommended packages:

```text
WorkflowApi.Abstractions
WorkflowApi.AspNetCore
WorkflowApi.Temporal
WorkflowApi.Reference
WorkflowApi.MSBuild
WorkflowApi.Cli
WorkflowApi.Catalog
Aspire.Hosting.WorkflowApiCatalog
```

### 2.1 `WorkflowApi.Abstractions`

Contains:

- core spec model types;
- attributes;
- extension interfaces;
- transformer interfaces;
- common enums;
- no ASP.NET dependency;
- no Temporal dependency;
- no Scrutor dependency.

Examples:

```csharp
WorkflowApiDocument
WorkflowApiInfo
WorkflowApiHost
WorkflowApiWorkflow
WorkflowApiOperation
WorkflowApiStep
WorkflowApiBinding
WorkflowApiAttribute
WorkflowApiRunAttribute
WorkflowApiSignalAttribute
```

### 2.2 `WorkflowApi.AspNetCore`

Contains:

- `AddWorkflowApi(...)`;
- `MapWorkflowApi(...)`;
- document provider;
- document generation pipeline;
- XML documentation support;
- Scrutor-based scanning;
- runtime endpoint support;
- default JSON serialization;
- validation diagnostics.

May depend on:

- `WorkflowApi.Abstractions`;
- `Scrutor`;
- `Microsoft.AspNetCore.*`;
- `System.Text.Json`.

### 2.3 `WorkflowApi.Temporal`

Contains:

- Temporal SDK metadata readers;
- Temporal binding model;
- `WithTemporal(...)` extension;
- Temporal-specific validation;
- optional runtime overlay adapters later.

Depends on:

- `WorkflowApi.Abstractions`;
- `Temporalio` where necessary;
- optionally `Temporalio.Extensions.Hosting` integration in a separate subpackage if needed.

### 2.4 `WorkflowApi.Reference`

Contains:

- `MapWorkflowApiReference()`;
- embedded UI assets;
- React-based viewer;
- optional EventCatalog visualiser adapter;
- no document generation logic.

This package consumes a WorkflowAPI document endpoint.

### 2.5 `WorkflowApi.MSBuild`

Contains build-time document generation support.

Goal:

```bash
dotnet build /p:GenerateWorkflowApi=true
```

Output:

```text
artifacts/workflow-api/v1.workflow-api.json
```

### 2.6 `WorkflowApi.Cli`

CLI for:

```bash
dotnet workflowapi export
dotnet workflowapi validate
dotnet workflowapi diff
dotnet workflowapi publish
```

### 2.7 `WorkflowApi.Catalog`

Central catalog app for many WorkflowAPI documents.

### 2.8 `Aspire.Hosting.WorkflowApiCatalog`

Aspire hosting extension for the catalog container.

---

## 3. Public developer experience

### 3.1 Single-service reference UI

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWorkflowApi("v1")
    .ScanFromAssemblyOf<RiskEnrichmentWorkflow>()
    .WithTemporal(options =>
    {
        options.Namespace = builder.Configuration["Temporal:Namespace"] ?? "default";
        options.TaskQueue = "risk-service";
    });

var app = builder.Build();

app.MapWorkflowApi();
app.MapWorkflowApiReference();

app.Run();
```

Exposes:

```text
/workflow-api/v1.json
/.well-known/workflow-api.json
/workflow-api/reference
```

### 3.2 Document-only mode

```csharp
builder.Services.AddWorkflowApi("v1")
    .ScanFromAssemblyOf<RiskEnrichmentWorkflow>();

app.MapWorkflowApi();
```

No UI required.

### 3.3 UI-only mode

```csharp
app.MapWorkflowApiReference(options =>
{
    options.DocumentUrl = "/workflow-api/v1.json";
});
```

This mirrors Scalar: UI consumes a document endpoint.

### 3.4 Multiple named documents

```csharp
builder.Services.AddWorkflowApi("public")
    .ScanFromAssemblyOf<RiskEnrichmentWorkflow>()
    .ExcludeInternalSteps();

builder.Services.AddWorkflowApi("internal")
    .ScanFromAssemblyOf<RiskEnrichmentWorkflow>()
    .IncludeInternalSteps();
```

Endpoints:

```text
/workflow-api/public.json
/workflow-api/internal.json
```

---

## 4. Attribute design

### 4.1 Naming rule

All WorkflowAPI-owned attributes should start with `WorkflowApi`.

This avoids confusion with runtime SDK attributes such as Temporal's `[Workflow]`, `[WorkflowRun]`, `[WorkflowSignal]`, `[WorkflowQuery]`, `[WorkflowUpdate]`, and `[Activity]`.

Recommended names:

```csharp
[WorkflowApi]
[WorkflowApiRun]
[WorkflowApiSignal]
[WorkflowApiQuery]
[WorkflowApiUpdate]
[WorkflowApiActivity]
[WorkflowApiStep]
[WorkflowApiEdge]
[WorkflowApiDependsOn]
[WorkflowApiSearchAttribute]
[WorkflowApiExample]
[WorkflowApiDeprecated]
[WorkflowApiTag]
```

Temporal-specific WorkflowAPI metadata:

```csharp
[WorkflowApiTemporal]
[WorkflowApiTemporalNexusService]
[WorkflowApiTemporalNexusOperation]
```

Avoid:

```csharp
[TemporalWorkflowBinding]
[TemporalActivityBinding]
```

because those sound like Temporal SDK runtime attributes.

### 4.2 `[WorkflowApi]`

Applied to workflow classes or workflow modules.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class WorkflowApiAttribute : Attribute
{
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Owner { get; set; }
    public string? Domain { get; set; }
    public string[]? Tags { get; set; }
    public string? Lifecycle { get; set; }
    public string? Visibility { get; set; }
}
```

Example:

```csharp
[Workflow("RiskEnrichmentWorkflow")]
[WorkflowApi(
    Name = "risk-enrichment",
    Title = "B2B Risk Enrichment",
    Summary = "Enriches a company with D&B data and calculates risk.",
    Owner = "Team ECR",
    Domain = "Risk")]
public sealed class RiskEnrichmentWorkflow
{
}
```

### 4.3 `[WorkflowApiRun]`

Applied to the workflow run/start method.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class WorkflowApiRunAttribute : Attribute
{
    public string? OperationId { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string[]? Tags { get; set; }
}
```

### 4.4 `[WorkflowApiSignal]`, `[WorkflowApiQuery]`, `[WorkflowApiUpdate]`

Applied to message handler methods.

```csharp
public sealed class WorkflowApiSignalAttribute : Attribute
{
    public string? Name { get; set; }
    public string? OperationId { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
}
```

Same pattern for query/update.

### 4.5 `[WorkflowApiActivity]`

Applied to activity classes or methods.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class WorkflowApiActivityAttribute : Attribute
{
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? Group { get; set; }
    public string? ExpectedDuration { get; set; }
    public string? Sla { get; set; }
    public WorkflowApiCriticality Criticality { get; set; } = WorkflowApiCriticality.Unspecified;
    public string? Visibility { get; set; }
    public string[]? Tags { get; set; }
}
```

### 4.6 `[WorkflowApiStep]`

For business-visible steps that do not map 1:1 to an activity attribute.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class WorkflowApiStepAttribute : Attribute
{
    public string Name { get; }
    public WorkflowApiStepKind Kind { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? Group { get; set; }

    public WorkflowApiStepAttribute(string name)
    {
        Name = name;
    }
}
```

### 4.7 `[WorkflowApiEdge]`

For simple topology declarations.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class WorkflowApiEdgeAttribute : Attribute
{
    public string From { get; }
    public string To { get; }
    public string? Label { get; set; }
    public string? Condition { get; set; }

    public WorkflowApiEdgeAttribute(string from, string to)
    {
        From = from;
        To = to;
    }
}
```

Use attributes for small graphs only. For non-trivial topology, use a fluent definition.

### 4.8 `[WorkflowApiSearchAttribute]`

Documents intended business search attributes.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class WorkflowApiSearchAttributeAttribute : Attribute
{
    public string Name { get; }
    public string Type { get; set; } = "Keyword";
    public string? Summary { get; set; }
    public bool ContainsPersonalData { get; set; }

    public WorkflowApiSearchAttributeAttribute(string name)
    {
        Name = name;
    }
}
```

The analyzer should warn if `ContainsPersonalData = true` and the Temporal binding/documentation indicates the runtime does not encrypt search attributes.

---

## 5. Fluent definitions

Attributes are not ideal for large topology graphs.

Provide a fluent model:

```csharp
public sealed class RiskEnrichmentWorkflowDefinition
    : WorkflowApiDefinition<RiskEnrichmentWorkflow>
{
    public override void Define(IWorkflowApiBuilder builder)
    {
        builder.Workflow("risk-enrichment")
            .Title("B2B Risk Enrichment")
            .Owner("Team ECR")
            .Domain("Risk");

        builder.Step("identify-company")
            .Activity("IdentifyCompanyActivity")
            .Title("Identify company")
            .Sla(TimeSpan.FromSeconds(30));

        builder.Step("enrich-dnb")
            .Activity("EnrichDunsDataActivity")
            .Title("Enrich D&B data")
            .Sla(TimeSpan.FromMinutes(2));

        builder.Edge("identify-company", "enrich-dnb");
    }
}
```

Discovery:

```csharp
builder.Services.AddWorkflowApi()
    .ScanFromAssemblyOf<RiskEnrichmentWorkflow>();
```

Scanner should find:

```text
WorkflowApiDefinition<TWorkflow>
IWorkflowApiDefinition
[WorkflowApi]
[WorkflowApiActivity]
Temporal SDK [Workflow]/[Activity] attributes
```

---

## 6. Scrutor-based discovery

### 6.1 Use Scrutor internally

`WorkflowApi.AspNetCore` should depend on Scrutor for assembly scanning.

Public API should hide Scrutor complexity:

```csharp
builder.Services.AddWorkflowApi()
    .ScanFromAssemblyOf<RiskEnrichmentWorkflow>();
```

Advanced API can expose scanning customization:

```csharp
builder.Services.AddWorkflowApi()
    .ScanAssemblies(
        typeof(RiskEnrichmentWorkflow).Assembly,
        typeof(GeneratePdfWorkflow).Assembly);
```

### 6.2 Discovery responsibilities

Discovery should identify:

- workflow classes;
- workflow run methods;
- signal/query/update methods;
- activity classes/methods;
- WorkflowAPI fluent definitions;
- WorkflowAPI attributes;
- Temporal SDK attributes if the Temporal package is referenced;
- XML documentation comment sources.

### 6.3 Discovery should not necessarily register services

Scrutor can scan types, but WorkflowAPI does not need to register every discovered workflow/activity as executable DI services.

Better model:

```csharp
public interface IWorkflowApiDescriptorProvider
{
    ValueTask<IReadOnlyList<WorkflowApiDescriptor>> GetDescriptorsAsync(
        CancellationToken cancellationToken);
}
```

The scanner builds descriptors, not runtime registrations.

### 6.4 Modes

Recommended options:

```csharp
public sealed class WorkflowApiDiscoveryOptions
{
    public bool IncludeWorkflowApiAttributedTypes { get; set; } = true;
    public bool IncludeTemporalAttributedTypes { get; set; } = true;
    public bool IncludeInternalSteps { get; set; } = true;
    public bool IncludeHiddenSteps { get; set; } = false;
    public bool FailOnDuplicateOperationIds { get; set; } = true;
}
```

---

## 7. Metadata aggregation pipeline

WorkflowAPI generation should combine metadata from multiple sources in deterministic order.

Recommended order, lowest to highest precedence:

1. convention defaults;
2. Temporal SDK attributes;
3. method/type signatures;
4. XML documentation comments;
5. WorkflowAPI attributes;
6. fluent definitions;
7. options/configuration;
8. transformers;
9. environment/catalog overlays.

This mirrors the modern OpenAPI pattern: framework metadata first, customization pipeline last.

---

## 8. XML documentation comments

Support XML docs from day one.

Example:

```csharp
/// <summary>
/// Enriches a company with D&B data and calculates risk.
/// </summary>
/// <remarks>
/// Used by broker, website, sales portal, and CRM lead routes.
/// </remarks>
[Workflow("RiskEnrichmentWorkflow")]
[WorkflowApi(Name = "risk-enrichment", Owner = "Team ECR")]
public sealed class RiskEnrichmentWorkflow
{
    /// <summary>
    /// Starts risk enrichment for a company lead.
    /// </summary>
    [WorkflowRun]
    public Task<RiskResult> RunAsync(RiskRequest request) => ...;
}
```

Generated:

```yaml
workflows:
  risk-enrichment:
    summary: Enriches a company with D&B data and calculates risk.
    description: Used by broker, website, sales portal, and CRM lead routes.
    run:
      summary: Starts risk enrichment for a company lead.
```

Implementation notes:

- Find XML doc files near loaded assemblies.
- Support MSBuild property `GenerateDocumentationFile=true`.
- Provide opt-in/out behavior.
- Merge XML docs with attributes predictably.

---

## 9. Transformers

Copy the broad idea from `Microsoft.AspNetCore.OpenApi` transformers.

Interfaces:

```csharp
public interface IWorkflowApiDocumentTransformer
{
    ValueTask TransformAsync(
        WorkflowApiDocument document,
        WorkflowApiDocumentTransformerContext context,
        CancellationToken cancellationToken);
}

public interface IWorkflowApiWorkflowTransformer
{
    ValueTask TransformAsync(
        WorkflowApiWorkflow workflow,
        WorkflowApiWorkflowTransformerContext context,
        CancellationToken cancellationToken);
}

public interface IWorkflowApiOperationTransformer
{
    ValueTask TransformAsync(
        WorkflowApiOperation operation,
        WorkflowApiOperationTransformerContext context,
        CancellationToken cancellationToken);
}

public interface IWorkflowApiStepTransformer
{
    ValueTask TransformAsync(
        WorkflowApiStep step,
        WorkflowApiStepTransformerContext context,
        CancellationToken cancellationToken);
}

public interface IWorkflowApiSchemaTransformer
{
    ValueTask TransformAsync(
        WorkflowApiSchema schema,
        WorkflowApiSchemaTransformerContext context,
        CancellationToken cancellationToken);
}
```

Registration:

```csharp
builder.Services.AddWorkflowApi("v1", options =>
{
    options.AddDocumentTransformer<OwnershipTransformer>();
    options.AddWorkflowTransformer<SlaWorkflowTransformer>();
    options.AddStepTransformer<GroupInternalActivitiesTransformer>();
    options.AddSchemaTransformer<MoneySchemaTransformer>();
});
```

Use cases:

- add owner/team metadata from config;
- apply default SLA rules by domain;
- hide technical activities;
- group activities into business steps;
- mark PII-sensitive schemas;
- normalize names;
- add Backstage entity links;
- add Git SHA/build metadata.

---

## 10. Schema generation

### 10.1 Requirements

Schema generation should respect:

- `System.Text.Json` property names;
- nullable reference types;
- required members;
- enums and configured enum converters;
- generic DTOs;
- XML documentation comments;
- examples;
- custom converters where feasible;
- source-generated JSON metadata where provided.

### 10.2 Strategy

Initial implementation may use a pragmatic schema generator, but should abstract it behind:

```csharp
public interface IWorkflowApiSchemaGenerator
{
    WorkflowApiSchema GenerateSchema(Type type, WorkflowApiSchemaGenerationContext context);
}
```

This prevents coupling to one JSON schema library.

### 10.3 References

Use `components.schemas` and `$ref` for repeated DTOs.

---

## 11. Endpoints

### 11.1 `MapWorkflowApi()`

Default behavior:

```csharp
app.MapWorkflowApi();
```

Maps:

```text
/workflow-api/{documentName}.json
/.well-known/workflow-api.json
```

Options:

```csharp
app.MapWorkflowApi(options =>
{
    options.RoutePattern = "/workflow-api/{documentName}.json";
    options.WellKnownRoute = "/.well-known/workflow-api.json";
    options.DefaultDocumentName = "v1";
    options.RequireAuthorization = true;
});
```

### 11.2 Authorization

In development, anonymous access is acceptable.

In production, internal services may require auth:

```csharp
app.MapWorkflowApi()
    .RequireAuthorization("WorkflowApiRead");
```

### 11.3 Content negotiation

Support JSON first. YAML may be added later:

```text
/workflow-api/v1.json
/workflow-api/v1.yaml
```

---

## 12. Reference UI endpoint

```csharp
app.MapWorkflowApiReference();
```

Options:

```csharp
app.MapWorkflowApiReference(options =>
{
    options.RoutePrefix = "/workflow-api/reference";
    options.DocumentUrl = "/workflow-api/v1.json";
    options.Title = "Risk Service Workflow API";
});
```

The UI package must not generate the document. It consumes the endpoint.

---

## 13. Build-time generation

### 13.1 Why build-time matters

Corporate catalogs often ingest artifacts from CI rather than querying live services.

Support:

```bash
dotnet workflowapi export \
  --assembly ./bin/Release/net10.0/Risk.Workflows.dll \
  --document v1 \
  --output ./artifacts/workflow-api/v1.json
```

### 13.2 MSBuild properties

```xml
<PropertyGroup>
  <GenerateWorkflowApi>true</GenerateWorkflowApi>
  <WorkflowApiDocumentName>v1</WorkflowApiDocumentName>
  <WorkflowApiOutputPath>$(OutputPath)workflow-api</WorkflowApiOutputPath>
</PropertyGroup>
```

### 13.3 Difference from runtime endpoint

Runtime endpoint can include local runtime config.

Build-time artifact may need environment-neutral bindings or placeholders:

```yaml
bindings:
  temporal:
    namespace: ${TEMPORAL_NAMESPACE}
    taskQueues:
      - risk-service
```

---

## 14. Diagnostics and analyzers

Create Roslyn analyzers later.

Useful diagnostics:

| Code | Severity | Description |
|---|---|---|
| WFA001 | Warning | Workflow has `[Workflow]` but no `[WorkflowApi]` metadata. |
| WFA002 | Error | Duplicate WorkflowAPI workflow name. |
| WFA003 | Error | Duplicate operationId. |
| WFA004 | Warning | Activity registered in Temporal but missing WorkflowAPI metadata. |
| WFA005 | Warning | WorkflowAPI activity declared but not registered by worker. |
| WFA006 | Warning | Search attribute may contain PII. |
| WFA007 | Warning | Step references unknown activity/workflow/Nexus operation. |
| WFA008 | Error | Invalid ISO-8601 duration in SLA/expectedDuration. |
| WFA009 | Warning | Temporal namespace/task queue missing from binding. |
| WFA010 | Warning | XML docs missing for public workflow operation. |

---

## 15. Validation CLI

```bash
dotnet workflowapi validate ./workflow-api.json
```

Should validate:

- JSON schema correctness;
- duplicate keys;
- unresolved `$ref`s;
- missing required fields;
- invalid durations;
- invalid binding shape;
- missing schemas;
- incompatible lifecycle/deprecation metadata.

Diff:

```bash
dotnet workflowapi diff old.json new.json
```

Should classify changes:

```text
breaking
potentially-breaking
non-breaking
metadata-only
```

---

## 16. Source generator vs runtime scanner

### Runtime scanner

Use for MVP and local development.

Pros:

- simple;
- works with Scrutor;
- good for Aspire local preview;
- fast iteration.

Cons:

- reflection/trimming/AOT considerations;
- harder to use in CI artifact-only pipelines;
- less compile-time diagnostics.

### Source generator

Use later for enterprise maturity.

Pros:

- build-time artifacts;
- stronger diagnostics;
- no runtime reflection;
- easier for CI/catalog ingestion.

Cons:

- more complex;
- needs careful incremental generator design;
- must track source symbols and XML docs.

Recommendation:

```text
MVP: runtime scanner + CLI export.
V1: add MSBuild/source-generator path.
```

---

## 17. Rubber-duck review

### What works

- Mirrors modern .NET OpenAPI shape.
- Keeps Scrutor hidden behind a clean API.
- Keeps UI optional.
- Supports both local runtime and CI artifact generation.
- Avoids attribute overuse by adding transformers and fluent definitions.
- Avoids pretending Temporal has assembly scanning.

### Risks

1. **Duplicate registration burden**  
   Developers may register Temporal workflows explicitly and scan WorkflowAPI separately. Mitigate with validation and future integration hooks.

2. **Reflection/AOT concerns**  
   Runtime scanning may not be AOT-friendly. Mitigate with source generator later.

3. **Schema generation complexity**  
   Matching runtime serialization exactly is hard. Abstract schema generator and test heavily.

4. **Metadata precedence confusion**  
   Document precedence rules clearly and expose diagnostics.

5. **Too many packages too early**  
   MVP can combine `AspNetCore`, `Temporal`, and `Reference` temporarily, but final architecture should split them.

### MVP recommendation

Build first:

```text
WorkflowApi.Abstractions
WorkflowApi.AspNetCore
WorkflowApi.Temporal
WorkflowApi.Reference
```

Later:

```text
WorkflowApi.MSBuild
WorkflowApi.Cli
WorkflowApi.Catalog
Aspire.Hosting.WorkflowApiCatalog
```

> **v1 tightened scope note**  
> This file is retained as a post-v1 design note unless a section explicitly says otherwise. WorkflowAPI v1 is limited to the compact durable execution workflow API specification, .NET generation from workflow attributes or fluent definitions, Temporal static binding metadata, and static workflow display. Runtime overlays, catalogue collation, source polling, workflow control-plane actions, BPMN-style modelling and runtime observability are outside v1 scope.

# 02 - .NET Implementation Design

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
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>()
    .WithTemporal(options =>
    {
        options.Namespace = "Commerce.OrderService";
        options.TaskQueue = "order-service";
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
WorkflowApi.AspNetCore.Temporal
WorkflowApi.Reference
WorkflowApi.MSBuild
WorkflowApi.Cli
WorkflowApi.Catalog
Aspire.Hosting.WorkflowApiCatalog
```

Dependency graph:

```
WorkflowApi.Abstractions
    ↑                       ↑
WorkflowApi.AspNetCore    WorkflowApi.Temporal
    ↑           ↑               ↑
WorkflowApi.Reference   WorkflowApi.AspNetCore.Temporal
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
- fluent document builder API (`AddWorkflow(...)`, `AddDefinition<T>(...)`, `Configure(...)`);
- document provider;
- document generation pipeline;
- provider-based scanner (`ScanFromAssemblyOf<T>()`, `IWorkflowApiDescriptorProvider`);
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

This package is standalone complete for the **fluent authoring** use case. No Temporal dependency required.

### 2.3 `WorkflowApi.Temporal`

Contains:

- Temporal SDK attribute readers (`[Workflow]`, `[WorkflowRun]`, `[WorkflowSignal]`, `[WorkflowQuery]`, `[WorkflowUpdate]`, `[Activity]`);
- Temporal binding model (`workflowType`, `taskQueue`, `namespace`, `signalName`, `queryName`, `updateName`);
- Temporal-specific validation rules;
- activity type, signal, query, and update name inference from SDK attributes;
- Nexus bridge binding model.

Depends on:

- `WorkflowApi.Abstractions`;
- `Temporalio` (attribute reading only - does not require the full Temporal runtime).

**No dependency on `Microsoft.AspNetCore.*`.** A worker-only process or file-export tool can use this package without hosting a web server.

### 2.4 `WorkflowApi.AspNetCore.Temporal`

Contains:

- `WithTemporal(IWorkflowApiBuilder, Action<TemporalWorkflowApiOptions>)` extension;
- Temporal-aware assembly scanner (`IWorkflowApiDescriptorProvider`) that discovers `[Workflow]`-decorated types without requiring `[WorkflowApi*]` attributes;
- wires Temporal metadata reader into the document generation pipeline.

**Scope: spec production only.** This package has no runtime metrics, overlay, or history dependencies. Runtime overlay for Temporal is a separate concern addressed by `WorkflowApi.Overlay.Temporal`.

Depends on:

- `WorkflowApi.AspNetCore`;
- `WorkflowApi.Temporal`.

This is the package most Temporal .NET developers will install. It transitively provides everything needed to generate and serve a WorkflowAPI document.

### 2.5 `WorkflowApi.Reference`

Contains:

- `MapWorkflowApiReference()`;
- embedded UI assets;
- React-based viewer;
- optional EventCatalog visualiser adapter;
- no document generation logic.

This package consumes a WorkflowAPI document endpoint. It renders declared topology from the document. Runtime overlay (metrics, history, SLA decorations) is an independent plugin concern - the UI can fetch overlay data from a separately configured overlay provider, but this package has no overlay dependency.

### 2.6 `WorkflowApi.MSBuild`

Contains build-time document generation support.

Goal:

```bash
dotnet build /p:GenerateWorkflowApi=true
```

Output:

```text
artifacts/workflow-api/v1.workflow-api.json
```

### 2.7 `WorkflowApi.Cli`

CLI for:

```bash
dotnet workflowapi export
dotnet workflowapi validate
dotnet workflowapi diff
dotnet workflowapi publish
```

### 2.8 `WorkflowApi.Catalog`

Central catalog app for many WorkflowAPI documents.

### 2.9 `Aspire.Hosting.WorkflowApiCatalog`

Aspire hosting extension for the catalog container.

---

## 3. Public developer experience

WorkflowAPI supports two first-class spec-authoring paths. Runtime overlay (metrics, history) is a completely separate concern - see `WorkflowApi.Overlay.*` packages.

| Configuration | UI renders |
|---|---|
| `WorkflowApi.AspNetCore` alone | Declared workflow topology map. |
| `+ WorkflowApi.AspNetCore.Temporal` | Topology + Temporal binding details (task queue, workflow type) in detail panels. |

Runtime overlay decoration (counts, durations, SLA badges, history links) requires a separately installed and configured overlay provider. It is not part of spec registration.

### 3.1 Path A - Fluent document authoring (no Temporal)

For teams that want to declare and display a workflow map without Temporal, or for any workflow engine that does not have an auto-generator.

```csharp
using WorkflowApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWorkflowApi("v1")
    .Configure(doc =>
    {
        doc.Info.Title = "Order Workflows";
        doc.Info.Version = "1.0.0";
    })
    .AddDefinition<OrderProcessingWorkflowDefinition>();

var app = builder.Build();

app.MapWorkflowApi();
app.MapWorkflowApiReference();

app.Run();
```

Or inline without a definition class:

```csharp
builder.Services.AddWorkflowApi("v1")
    .AddWorkflow("order-processing", wf =>
    {
        wf.Title("Order Processing")
          .Run(run => run
              .OperationId("startOrderProcessing")
              .Input<OrderRequest>()
              .Output<OrderResult>())
          .Signal("cancel", s => s.Title("Cancel order").Input<CancelRequest>())
          .Step("validate", s => s.Kind(StepKind.Activity).Title("Validate order"))
          .Step("fulfil", s => s.Kind(StepKind.Activity).Title("Fulfil order"))
          .Edge("validate", "fulfil");
    });
```

No Temporal dependency. The UI renders the declared topology map with no metrics overlay.

### 3.2 Path B - Temporal auto-generation

For Temporal .NET applications that want to generate the WorkflowAPI document from SDK attributes.

**Important - what Temporal attributes can and cannot provide:**

Temporal SDK attributes expose the **public workflow API surface**:
- `[Workflow]` → workflow identity and name
- `[WorkflowRun]` → run operation with input/output types
- `[WorkflowSignal]` → signal operations
- `[WorkflowQuery]` → query operations
- `[WorkflowUpdate]` → update operations
- `[Activity]` → activity definitions

Temporal SDK attributes do **not** expose the **internal step/edge topology** (the workflow map). For a full topology, supplement with:
- `[WorkflowApiStep]` / `[WorkflowApiEdge]` attributes on the workflow class, or
- a `WorkflowApiDefinition<T>` fluent definition.

Path B without supplemental topology metadata produces a valid WorkflowAPI document with the complete public API surface - but without declared steps and edges.

```csharp
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using TemporalRiskService.Workflows;
using WorkflowApi;
using WorkflowApi.Temporal; // WorkflowApi.AspNetCore.Temporal

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTemporalClient(options =>
{
    options.TargetHost = builder.Configuration["Temporal:TargetHost"] ?? "localhost:7233";
    options.Namespace = builder.Configuration["Temporal:Namespace"] ?? "default";
});

builder.Services.AddHostedTemporalWorker("order-service")
    .AddWorkflow<OrderFulfilmentWorkflow>()
    .AddActivity<IdentifyCompanyActivity>()
    .AddActivity<TakePaymentActivity>()
    .AddActivity<CalculateRiskActivity>();

builder.Services.AddWorkflowApi("v1")
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>()
    .WithTemporal(options =>
    {
        options.Namespace = builder.Configuration["Temporal:Namespace"] ?? "default";
        options.TaskQueue = "order-service";
        options.WebUrl = builder.Configuration["Temporal:WebUrl"];
    });

var app = builder.Build();

app.MapWorkflowApi();
app.MapWorkflowApiReference();

app.Run();
```

### 3.3 Document-only mode (export without UI)

```csharp
builder.Services.AddWorkflowApi("v1")
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>();

app.MapWorkflowApi();
```

No UI required.

### 3.4 UI-only mode

```csharp
app.MapWorkflowApiReference(options =>
{
    options.DocumentUrl = "/workflow-api/v1.json";
});
```

This mirrors Scalar: UI consumes a document endpoint.

### 3.5 Multiple named documents

```csharp
builder.Services.AddWorkflowApi("public")
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>()
    .ExcludeInternalSteps();

builder.Services.AddWorkflowApi("internal")
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>()
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
[WorkflowApiSearchDimension]
[WorkflowApiExample]
[WorkflowApiDeprecated]
[WorkflowApiTag]
```

Temporal-specific attributes live in `WorkflowApi.Temporal` and use the Temporal concept names directly, since they are explicitly in the Temporal package namespace:

```csharp
// In WorkflowApi.Temporal - Nexus bridge metadata
[WorkflowApiNexusBridge]
[WorkflowApiNexusService]
[WorkflowApiNexusOperation]
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
[Workflow("OrderFulfilmentWorkflow")]
[WorkflowApi(
    Name = "order-fulfilment",
    Title = "Order Fulfilment",
    Summary = "Fulfils an e-commerce order by reserving inventory, taking payment, registering shipping and sending confirmation email.",
    Owner = "Commerce Platform Team",
    Domain = "Risk")]
public sealed class OrderFulfilmentWorkflow
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

### 4.8 `[WorkflowApiSearchDimension]`

Documents intended business search dimensions for catalog filtering and runtime search. Named `SearchDimension` rather than `SearchAttribute` to avoid the double-suffix `SearchAttributeAttribute` and to align with the generic terminology (the spec uses "search dimension"; Temporal calls these "search attributes").

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class WorkflowApiSearchDimensionAttribute : Attribute
{
    public string Name { get; }
    public string? Description { get; set; }
    public bool ContainsPersonalData { get; set; }

    public WorkflowApiSearchDimensionAttribute(string name)
    {
        Name = name;
    }
}
```

Runtime-specific index types (e.g. Temporal's `Keyword`, `Text`, `Int`, `DateTime`) are declared in `WorkflowApi.Temporal` and mapped through the Temporal binding, not through this generic attribute.

The analyzer should warn if `ContainsPersonalData = true` and the document is exported without redaction.

---

## 5. Fluent definitions

Attributes are not ideal for large topology graphs.

Provide a fluent model:

```csharp
public sealed class OrderFulfilmentWorkflowDefinition
    : WorkflowApiDefinition<OrderFulfilmentWorkflow>
{
    public override void Define(IWorkflowApiBuilder builder)
    {
        builder.Workflow("order-fulfilment")
            .Title("Order Fulfilment")
            .Owner("Commerce Platform Team")
            .Domain("Risk");

        builder.Step("check-and-block-inventory")
            .Activity("IdentifyCompanyActivity")
            .Title("Check and block inventory")
            .Sla(TimeSpan.FromSeconds(30));

        builder.Step("take-payment")
            .Activity("TakePaymentActivity")
            .Title("Take payment")
            .Sla(TimeSpan.FromMinutes(2));

        builder.Edge("check-and-block-inventory", "take-payment");
    }
}
```

Discovery:

```csharp
builder.Services.AddWorkflowApi()
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>();
```

Scanner should find:

```text
WorkflowApiDefinition<TWorkflow>
IWorkflowApiDefinition
[WorkflowApi]
[WorkflowApiActivity]
```

Temporal SDK types (`[Workflow]`, `[Activity]`) are discovered exclusively by the Temporal provider registered via `WithTemporal(...)` - not by the core scanner.

Activity string references (e.g. `.Activity("IdentifyCompanyActivity")`) resolve in this order: attribute `Name` property → class name → method name. The first non-null/non-empty match wins. Duplicate resolution produces a `WFA002`-equivalent diagnostic.

---

## 6. Scrutor-based discovery

### 6.1 Use Scrutor internally

`WorkflowApi.AspNetCore` should depend on Scrutor for assembly scanning.

Public API should hide Scrutor complexity:

```csharp
builder.Services.AddWorkflowApi()
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>();
```

Advanced API can expose scanning customization:

```csharp
builder.Services.AddWorkflowApi()
    .ScanAssemblies(
        typeof(OrderFulfilmentWorkflow).Assembly,
        typeof(GeneratePdfWorkflow).Assembly);
```

### 6.2 Discovery responsibilities

The scanner is provider-based. Each provider contributes different parts of the document.

**Core provider** (always active) discovers via WorkflowAPI's own attributes:

- `WorkflowApiDefinition<T>` implementations (fluent topology);
- `[WorkflowApi]`-decorated workflow classes → workflow identity and metadata;
- `[WorkflowApiRun/Signal/Query/Update]`-decorated methods → operation surface;
- `[WorkflowApiActivity]`-decorated activity classes → activity registry;
- `[WorkflowApiStep]`/`[WorkflowApiEdge]` on workflow classes → **declared topology**;
- XML documentation comment sources.

**Temporal provider** (registered by `WithTemporal(...)`) additionally discovers via Temporal SDK attributes:

- `[Workflow]`-decorated types → workflow type name and metadata enrichment;
- `[WorkflowRun]`, `[WorkflowSignal]`, `[WorkflowQuery]`, `[WorkflowUpdate]` methods → operation surface (supplements WorkflowAPI attributes);
- `[Activity]`-decorated types → activity type name and binding metadata;
- `[NexusService]`-decorated interfaces → bridge service definition (service name, operations);
- `[NexusOperation]`-decorated methods on Nexus service interfaces → bridge operation definitions;
- `[NexusServiceHandler]`-decorated classes → links bridge service to the handler workflow.

**Key boundary:** Temporal SDK attributes provide the **public operation surface** and **bridge service definitions** (`run`, `signal`, `query`, `update`, bridge services/operations). WorkflowAPI attributes (`[WorkflowApiStep]`, `[WorkflowApiEdge]`) declare the **topology** (which steps a workflow calls and in what order). Neither reads the workflow's implementation code - execution sequence must be explicitly declared.

**Bridge discoverability note:** Unlike activity call steps (which are anonymous inline `ExecuteActivityAsync(...)` calls), Nexus bridge services are defined on typed interfaces with `[NexusService]`/`[NexusOperation]` attributes. This means the `bridges` map **can be auto-populated** from attribute scanning - analogous to how `[Activity]` populates the activities map. The `kind: bridge` step in a calling workflow's topology is still explicitly declared.

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

Recommended core options (runtime-neutral):

```csharp
public sealed class WorkflowApiDiscoveryOptions
{
    public bool IncludeWorkflowApiAttributedTypes { get; set; } = true;
    public bool IncludeInternalSteps { get; set; } = true;
    public bool IncludeHiddenSteps { get; set; } = false;
    public bool FailOnDuplicateOperationIds { get; set; } = true;
}
```

Temporal-specific discovery options live in `WorkflowApi.Temporal`:

```csharp
public sealed class TemporalWorkflowApiOptions
{
    public string? Namespace { get; set; }
    public string? TaskQueue { get; set; }
    public string? WebUrl { get; set; }
    public bool IncludeTemporalAttributedTypes { get; set; } = true;
}
```

---

## 7. Metadata aggregation pipeline

WorkflowAPI generation should combine metadata from multiple sources in deterministic order.

Recommended order, lowest to highest precedence:

1. convention defaults;
2. Temporal SDK attributes - workflow/activity type names, operation surface (when Temporal provider is active);
3. method/type signatures - input/output types from parameter reflection;
4. WorkflowAPI attributes - `[WorkflowApi]`, `[WorkflowApiActivity]`, `[WorkflowApiStep]`, `[WorkflowApiEdge]`, etc.;
5. XML documentation comments - fill gaps only, do not override explicit annotations;
6. fluent definitions - override any attribute-derived value;
7. options/configuration;
8. transformers.

**What each layer provides:**
- Steps 2-3: public API surface inferred from code
- Step 4: explicit declarations including topology (steps/edges cannot come from step 2)
- Steps 5-8: enrichment and customisation pipeline

The pipeline produces the declared spec document. Runtime overlay data is completely separate and never merged into this pipeline.

---

## 8. XML documentation comments

Support XML docs from day one.

Example:

```csharp
/// <summary>
/// Fulfils an e-commerce order by reserving inventory, taking payment, registering shipping and sending confirmation email.
/// </summary>
/// <remarks>
/// Used by broker, website, sales portal, and CRM lead routes.
/// </remarks>
[Workflow("OrderFulfilmentWorkflow")]
[WorkflowApi(Name = "order-fulfilment", Owner = "Commerce Platform Team")]
public sealed class OrderFulfilmentWorkflow
{
    /// <summary>
    /// Starts risk enrichment for a company lead.
    /// </summary>
    [WorkflowRun]
    public Task<OrderFulfilmentResult> RunAsync(OrderFulfilmentRequest request) => ...;
}
```

Generated:

```yaml
workflows:
  order-fulfilment:
    summary: Fulfils an e-commerce order by reserving inventory, taking payment, registering shipping and sending confirmation email.
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
    options.Title = "Commerce Order Workflow API";
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
      - order-service
```

---

## 14. Diagnostics and analyzers

Create Roslyn analyzers later.

Useful diagnostics:

| Code | Severity | Scope | Description |
|---|---|---|---|
| WFA001 | Info | Temporal provider | Workflow has `[Workflow]` but no `[WorkflowApi*]` metadata - API surface generated from SDK attributes only; no topology. |
| WFA002 | Error | Core | Duplicate WorkflowAPI workflow name. |
| WFA003 | Error | Core | Duplicate operationId. |
| WFA004 | Warning | Temporal runtime integration | Activity registered in worker but missing WorkflowAPI metadata. Requires runtime registration data - only fires when worker integration is available. |
| WFA005 | Warning | Temporal runtime integration | WorkflowAPI activity declared but not registered in any worker. Requires runtime registration data. |
| WFA006 | Warning | Core | Search dimension declared with `ContainsPersonalData = true`. |
| WFA007 | Warning | Core | Step references unknown activity, workflow, or bridge key. |
| WFA008 | Error | Core | Invalid ISO-8601 duration in SLA or expectedDuration. |
| WFA009 | Warning | Temporal binding | Temporal namespace or task queue missing from binding when runtime-capable document is expected. |
| WFA010 | Warning | Core | XML docs missing for public workflow operation. |

WFA004 and WFA005 require knowledge of Temporal worker registration - they can only fire in a context where that registration data is accessible (e.g. via a future `WorkflowApi.Temporal.Hosting` integration), not in attribute-reading-only mode.

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
- Completely separates spec production from runtime overlay - no mixed concerns.

### Risks

1. **Path B topology gap**  
   Temporal SDK attributes don't expose the internal workflow graph. Mitigate by documenting clearly that topology requires supplemental `[WorkflowApiStep]`/`[WorkflowApiEdge]` attributes or a fluent definition.

2. **"Activity" as a core concept**  
   `[WorkflowApiActivity]`, `StepKind.Activity`, and the fluent `.Activity()` API use Temporal-conventional terminology. This is intentional and defensible - "activity" appears in WS-BPEL, Microsoft Durable Functions, and other workflow standards. `task` is the truly generic alias in the spec; `activity` is the conventional name in Temporal-family systems. The design acknowledges both.

3. **Reflection/AOT concerns**  
   Runtime scanning may not be AOT-friendly. Mitigate with source generator later.

4. **Schema generation complexity**  
   Matching runtime serialization exactly is hard. Abstract schema generator and test heavily.

5. **Metadata precedence confusion**  
   Document precedence rules clearly and expose diagnostics.

### MVP recommendation

Build first (spec production):

```text
WorkflowApi.Abstractions
WorkflowApi.AspNetCore
WorkflowApi.Temporal
WorkflowApi.AspNetCore.Temporal
WorkflowApi.Reference
```

Later (tooling):

```text
WorkflowApi.MSBuild
WorkflowApi.Cli
WorkflowApi.Catalog
Aspire.Hosting.WorkflowApiCatalog
```

Later (runtime overlay - separate milestone):

```text
WorkflowApi.Overlay.Abstractions
WorkflowApi.Overlay.AspNetCore
WorkflowApi.Overlay.Temporal
```

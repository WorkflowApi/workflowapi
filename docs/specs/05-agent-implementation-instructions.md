> **v1 tightened scope note**  
> This file is retained as a post-v1 design note unless a section explicitly says otherwise. WorkflowAPI v1 is limited to the compact durable execution workflow API specification, .NET generation from workflow attributes or fluent definitions, Temporal static binding metadata, and static workflow display. Runtime overlays, catalogue collation, source polling, workflow control-plane actions, BPMN-style modelling and runtime observability are outside v1 scope.

# 05 - Agent Implementation Instructions

Version: **0.1 draft**  
Audience: agentic AI engineering team, repo bootstrap agent, specialist sub-agents  
Status: implementation planning

---

## 1. Objective

Build an experimental WorkflowAPI ecosystem for .NET and Temporal.

The implementation should produce:

1. a WorkflowAPI document model;
2. .NET attributes and fluent definitions;
3. Scrutor-based discovery;
4. runtime document endpoint;
5. single-service reference UI;
6. Temporal .NET binding support;
7. validation diagnostics;
8. example Temporal .NET service;
9. later: catalog container and Aspire hosting extension.

---

## 2. Prime directives

1. **Do not couple the spec to Temporal.**  
   Temporal-specific metadata belongs in `bindings.temporal`.

2. **Do not couple the spec to the UI.**  
   The UI consumes WorkflowAPI documents.

3. **Do not claim Temporal APIs provide a full workflow graph.**  
   Topology must be declared, inferred best-effort, or observed as runtime overlay.

4. **Do not invent fake Temporal registration APIs.**  
   Temporal runtime registration remains explicit.

5. **Use Scrutor as an internal discovery dependency.**  
   Public API should be clean and convention-driven.

6. **Follow .NET 10 OpenAPI design lessons.**  
   Generation, endpoint, transformers, XML comments, UI separation.

7. **Prefer boring names.**  
   `AddWorkflowApi`, `MapWorkflowApi`, `MapWorkflowApiReference`.

8. **Avoid producer/provider language.**  
   Use host, implements, dependsOn, calls, bindings.

---

## 3. Suggested repository layout

```text
workflowapi/
  src/
    WorkflowApi.Abstractions/
    WorkflowApi.AspNetCore/
    WorkflowApi.Temporal/
    WorkflowApi.Reference/
    WorkflowApi.Catalog/
    Aspire.Hosting.WorkflowApiCatalog/
    WorkflowApi.Cli/
    WorkflowApi.MSBuild/

  samples/
    TemporalRiskService/
      TemporalRiskService.AppHost/
      TemporalRiskService.ApiWorker/
      TemporalRiskService.Workflows/

  tests/
    WorkflowApi.Abstractions.Tests/
    WorkflowApi.AspNetCore.Tests/
    WorkflowApi.Temporal.Tests/
    WorkflowApi.Reference.Tests/
    WorkflowApi.Catalog.Tests/

  docs/
    spec/
    adr/
    examples/
```

For MVP, implement only:

```text
WorkflowApi.Abstractions
WorkflowApi.AspNetCore
WorkflowApi.Temporal
WorkflowApi.Reference
samples/TemporalRiskService
```

---

## 4. Workstream split

### 4.1 Core spec agent

Owns:

- `WorkflowApi.Abstractions`;
- model classes;
- JSON serialization;
- schema validation skeleton;
- extension support;
- examples.

Deliverables:

- `WorkflowApiDocument` model;
- `WorkflowApiInfo`;
- `WorkflowApiHost`;
- `WorkflowApiWorkflow`;
- `WorkflowApiOperation`;
- `WorkflowApiActivity`;
- `WorkflowApiStep`;
- `WorkflowApiEdge`;
- binding dictionary model;
- components/schemas model;
- unit tests.

Acceptance criteria:

- can serialize minimal valid document;
- can deserialize example document;
- preserves unknown `x-*` extensions;
- stable JSON property names;
- nullable annotations enabled;
- no ASP.NET or Temporal dependencies.

### 4.2 .NET discovery agent

Owns:

- `WorkflowApi.AspNetCore` discovery;
- Scrutor integration;
- attributes;
- XML docs loader;
- descriptor providers.

Deliverables:

- `AddWorkflowApi()`;
- `.ScanFromAssemblyOf<T>()`;
- attribute readers;
- XML comment reader;
- descriptor registry;
- document provider.

Acceptance criteria:

- discovers `[WorkflowApi]` workflows;
- discovers `[WorkflowApiActivity]` activities;
- discovers fluent definitions;
- merges XML comments;
- does not register workflow classes as app services unless required;
- unit tests for precedence rules.

### 4.3 Temporal binding agent

Owns:

- `WorkflowApi.Temporal`;
- Temporal SDK attribute readers;
- binding models;
- validation rules;
- sample integration.

Deliverables:

- `WithTemporal(...)`;
- infer `[Workflow]`, `[WorkflowRun]`, `[WorkflowSignal]`, `[WorkflowQuery]`, `[WorkflowUpdate]`, `[Activity]`;
- bind namespace/task queue from options;
- Temporal-specific diagnostics.

Acceptance criteria:

- extracts workflow type names;
- extracts run/signal/query/update method signatures;
- extracts activity type names;
- handles missing namespace/task queue as warnings;
- does not pretend to register Temporal workers.

### 4.4 Endpoint agent

Owns:

- `MapWorkflowApi()`;
- route conventions;
- authorization hooks;
- named documents.

Deliverables:

- `/workflow-api/{documentName}.json`;
- `/.well-known/workflow-api.json`;
- JSON response with content type;
- document provider injection.

Acceptance criteria:

- endpoint returns generated doc;
- supports default doc name;
- returns 404 for unknown doc;
- supports auth via normal ASP.NET endpoint conventions.

### 4.5 Reference UI agent

Owns:

- `WorkflowApi.Reference`;
- embedded static assets;
- React viewer;
- graph adapter;
- UI routing.

Deliverables:

- `MapWorkflowApiReference()`;
- document fetch;
- workflow list;
- workflow detail;
- activity list;
- basic visual map;
- raw JSON view;
- diagnostics view.

Acceptance criteria:

- works with one service, no catalog;
- no backend dependency beyond document endpoint;
- UI can be disabled in production;
- graph rendering does not mutate document shape.

### 4.6 Sample app agent

Owns:

- Temporal sample app;
- Aspire AppHost;
- workflows/activities/contracts assembly;
- local docs.

Deliverables:

- `TemporalRiskService.Workflows`;
- `TemporalRiskService.ApiWorker`;
- `TemporalRiskService.AppHost`;
- sample workflow-api output.

Acceptance criteria:

- `dotnet run`/Aspire run starts service;
- WorkflowAPI endpoint available;
- reference UI loads;
- sample Temporal worker registration explicit;
- docs explain API+worker combined and split modes.

### 4.7 Catalog/Aspire agent, later

Owns:

- central catalog container;
- Aspire hosting extension;
- multi-source ingestion.

Deliverables:

- `AddWorkflowApiCatalog(...)`;
- `.WithCatalogSource(project)` convention;
- catalog UI importing multiple sources.

Acceptance criteria:

- local Aspire catalog reads two services;
- default source path is `/.well-known/workflow-api.json`;
- override path works;
- catalog graph joins dependencies.

---

## 5. Milestones

### Milestone 0 - repo bootstrap

- solution created;
- projects added;
- nullable enabled;
- analyzers enabled;
- formatting/editorconfig;
- README;
- basic CI running `dotnet test`.

### Milestone 1 - core document model

- model types;
- JSON serialization;
- minimal valid document test;
- sample JSON fixture.

### Milestone 2 - attributes and scanning

- attributes;
- Scrutor scanner;
- descriptor registry;
- XML docs basic support;
- generated doc from sample workflow.

### Milestone 3 - Temporal binding

- Temporal attribute reader;
- `WithTemporal` options;
- workflow/activity type inference;
- diagnostics.

### Milestone 4 - ASP.NET endpoints

- `MapWorkflowApi`;
- well-known route;
- named document route;
- auth hooks.

### Milestone 5 - reference UI

- static React UI;
- document fetch;
- workflow detail;
- simple graph;
- raw document.

### Milestone 6 - sample app

- Temporal .NET sample;
- Aspire AppHost;
- docs;
- screenshot/gif optional.

### Milestone 7 - catalog preview

- containerized catalog;
- multi-source import;
- Aspire hosting extension.

---

## 6. Coding standards

- Target .NET 10 LTS for primary packages.
- Enable nullable reference types.
- Treat warnings as errors in CI where practical.
- Use `System.Text.Json`.
- Avoid global static mutable state.
- Keep abstractions free from ASP.NET/Temporal dependencies.
- Use dependency injection for providers/transformers.
- Use clear diagnostics instead of silent failure.
- Avoid reflection-only code paths that cannot be replaced by source generation later.
- Preserve unknown extension fields.

---

## 7. Test strategy

### 7.1 Unit tests

- model serialization/deserialization;
- attribute extraction;
- XML doc extraction;
- precedence rules;
- transformers;
- Temporal attribute inference;
- diagnostics.

### 7.2 Integration tests

- ASP.NET test host returns `/workflow-api/v1.json`;
- `/.well-known/workflow-api.json` points to default document;
- reference UI serves assets;
- sample app generation.

### 7.3 Snapshot tests

Use stable JSON snapshots for generated documents.

Snapshots should be reviewed carefully because they become contract examples.

### 7.4 UI tests

- Playwright for reference UI;
- document loads;
- workflow list renders;
- graph view renders;
- raw JSON view works;
- diagnostics display.

### 7.5 Future catalog tests

- import multiple docs;
- duplicate workflow key handling;
- dependency graph join;
- missing dependency diagnostics;
- version diff classification.

---

## 8. Initial sample domain

Use a B2B risk enrichment workflow.

Workflows:

```text
OrderFulfilmentWorkflow
MonthlyRiskRefreshWorkflow
GenerateDnbPdfWorkflow
```

Activities:

```text
IdentifyCompanyActivity
TakePaymentActivity
CalculateRiskActivity
GenerateDnbPdfActivity
PublishCompanyEnrichedEventActivity
```

Signals:

```text
PaymentAuthorised
ManualReviewCompleted
```

Queries:

```text
GetStatus
GetRiskSummary
```

Updates:

```text
RecalculateRisk
```

Search Attributes:

```text
BusinessProcess
OrderId
SalesChannel
RiskClass
CustomerNumber
```

---

## 9. Minimum viable C# API

```csharp
builder.Services.AddWorkflowApi("v1")
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>()
    .WithTemporal(options =>
    {
        options.Namespace = "default";
        options.TaskQueue = "order-service";
    });

app.MapWorkflowApi();
app.MapWorkflowApiReference();
```

Attributes:

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
    [WorkflowRun]
    [WorkflowApiRun(Summary = "Start risk enrichment")]
    public Task<OrderFulfilmentResult> RunAsync(OrderFulfilmentRequest request) => ...;
}
```

---

## 10. Anti-patterns to avoid

1. **Generating the spec only from Temporal history.**  
   Histories show observed execution, not full intended contract.

2. **Making the catalog mandatory for local development.**  
   Single-service reference UI should work alone.

3. **Using EventCatalog as the runtime product.**  
   Use visualiser package/adapters if useful, not EventCatalog itself.

4. **Treating WorkflowAPI as Temporal-only.**  
   Temporal-specific data belongs in bindings.

5. **Using provider/consumer language.**  
   Use host/implements/dependsOn/calls.

6. **Overusing attributes for topology.**  
   Use fluent definitions for complex maps.

7. **Duplicating OpenAPI UI mistakes.**  
   Generation and UI must be separate.

8. **Exposing specs publicly by accident.**  
   Production endpoints need auth/visibility controls.

---

## 11. Rubber-duck checklist before every PR

Ask:

- Does this change keep WorkflowAPI generic?
- Does this change leak Temporal-specific terms into the core model?
- Does this change couple document generation to UI?
- Does this change work for a combined API+worker process?
- Does this change work for a separate worker process?
- Does this change preserve unknown extensions?
- Does this change have tests for both minimal and realistic documents?
- Does this change use terms consistently: host, implements, dependsOn, bindings?
- Does this change align with .NET 10 OpenAPI-style patterns?
- Does this change avoid claiming unsupported Temporal API capabilities?


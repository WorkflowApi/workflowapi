> **v1 tightened scope note**  
> This file is retained as a post-v1 design note unless a section explicitly says otherwise. WorkflowAPI v1 is limited to the compact durable execution workflow API specification, .NET generation from workflow attributes or fluent definitions, Temporal static binding metadata, and static workflow display. Runtime overlays, catalogue collation, source polling, workflow control-plane actions, BPMN-style modelling and runtime observability are outside v1 scope.

# 04 - Reference UI and Catalog Design

Version: **0.1 draft**  
Audience: UI agents, catalog agents, Aspire agents, product agents  
Status: proposal

---

## 1. Purpose

WorkflowAPI needs two UI modes:

1. **Per-service WorkflowAPI Reference UI**  
   Similar to Scalar for OpenAPI. Embedded in a service, reads one WorkflowAPI document, and helps developers inspect workflows hosted by that service.

2. **Central WorkflowAPI Catalog**  
   Similar in spirit to EventCatalog or Backstage for durable workflows. Collates many WorkflowAPI documents, shows connectivity, ownership, Nexus relationships, and optional runtime overlays.

The single-service UI should not require a separate Docker container. The catalog may be a containerized app and an Aspire resource.

---

## 2. Product positioning

### 2.1 Per-service reference UI

Comparable to:

```text
OpenAPI document + Scalar UI
```

WorkflowAPI equivalent:

```csharp
app.MapWorkflowApi();
app.MapWorkflowApiReference();
```

Purpose:

- local developer inspection;
- service-level documentation;
- workflow contract exploration;
- quick visual map;
- no central infrastructure required.

### 2.2 Central catalog

Comparable to:

```text
EventCatalog / Backstage for durable workflows
```

Purpose:

- multiple workflow hosts;
- cross-service/cross-namespace relationships;
- Nexus service/operation maps;
- ownership and impact analysis;
- runtime overlays;
- CI-published artifact ingestion;
- governance.

---

## 3. Per-service reference UI

### 3.1 Developer API

```csharp
builder.Services.AddWorkflowApi("v1")
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>()
    .WithTemporal(...);

app.MapWorkflowApi();
app.MapWorkflowApiReference();
```

Default routes:

```text
/workflow-api/v1.json
/.well-known/workflow-api.json
/workflow-api/reference
```

Options:

```csharp
app.MapWorkflowApiReference(options =>
{
    options.RoutePrefix = "/workflow-api/reference";
    options.DocumentUrl = "/workflow-api/v1.json";
    options.Title = "Commerce Order Workflow API";
    options.DefaultView = WorkflowApiReferenceView.Workflows;
});
```

### 3.2 UI sections

Recommended first version:

```text
Overview
Workflows
Activities
Nexus services
Runtime bindings
Schemas
Visual map
Raw document
Validation diagnostics
```

### 3.3 Workflow detail page

Shows:

- title/summary/description;
- owner/domain/tags;
- run input/output schemas;
- signals;
- queries;
- updates;
- steps;
- edges;
- dependencies;
- Temporal binding;
- examples;
- validation warnings.

### 3.4 Visual map

Use `@eventcatalog/visualiser` as a dependency where useful.

Implementation pattern:

```text
WorkflowAPI steps/edges
    -> adapter
EventCatalog visualiser graph model / React Flow graph
    -> NodeGraph/Flow visualisation
```

Do not couple the WorkflowAPI spec to EventCatalog's internal schema. Treat it as a rendering adapter.

### 3.5 Single-service UI limitations

The single-service UI does not need to:

- discover other services;
- resolve all Nexus endpoints;
- show global ownership maps;
- index Temporal histories at scale;
- store layout positions centrally;
- provide enterprise governance workflows.

Those belong to the catalog.

---

## 4. Central catalog

### 4.1 Purpose

A central application gathers WorkflowAPI documents from many catalog sources.

Sources may be:

- live service endpoints;
- files;
- Git repositories;
- OCI artifacts;
- CI-published package artifacts;
- Aspire project references in local development.

### 4.2 Main catalog capabilities

```text
Import many WorkflowAPI documents
Validate documents
Index workflow hosts
Index workflows/activities/Nexus operations
Build dependency graph
Show ownership/team/domain views
Show Nexus connectivity
Support search/filter
Show runtime overlays where configured
Show document/version diffs
Export graph views
```

### 4.3 Core catalog views

#### 4.3.1 Workflow hosts

```text
order-service
  owner: Commerce Platform Team
  runtime: temporal
  namespace: Commerce.OrderService
  taskQueues: order-service, risk-refresh
  workflows: 4
  activities: 18
  nexus services: 1
```

#### 4.3.2 Workflow catalog

```text
OrderFulfilmentWorkflow
  host: order-service
  owner: Commerce Platform Team
  domain: Risk
  run input: OrderFulfilmentRequest
  signals: 2
  queries: 1
  updates: 1
  steps: 7
```

#### 4.3.3 Nexus map

```text
OfferCalculationWorkflow
  -> calls Nexus RiskService.CalculateRisk
      -> implemented by order-service
      -> handled by OrderFulfilmentWorkflow
```

#### 4.3.4 Dependency graph

```text
Offer Worker
  OfferCalculationWorkflow
    -> RiskService.CalculateRisk
       -> Commerce Order Worker
          OrderFulfilmentWorkflow
             -> payment provider external system
             -> DocumentService.GeneratePdf
```

#### 4.3.5 Runtime overlay

```text
OrderFulfilmentWorkflow
  executions last 7d: 2,431
  failed: 12
  p95 duration: 2m 14s
  slowest step: Take payment
```

---

## 5. EventCatalog visualiser usage

### 5.1 Why use it

The package `@eventcatalog/visualiser` provides framework-agnostic ReactFlow visualiser components. EventCatalog’s NodeGraph/Flow visualisations provide interaction patterns such as React Flow and Mermaid modes, controls, and export/share capabilities.

WorkflowAPI can use this package as a renderer without using EventCatalog as the product/runtime.

### 5.2 Adapter boundary

Keep this boundary explicit:

```text
WorkflowAPI document
  -> WorkflowApiGraphModel
  -> EventCatalogVisualiserAdapter
  -> React UI
```

Do not make WorkflowAPI documents contain EventCatalog visualiser-specific node payloads.

### 5.3 When to use React Flow directly

If the EventCatalog visualiser is not flexible enough for:

- metric badges;
- custom temporal nodes;
- layout persistence;
- special step types;
- accessibility;
- theming;

then the UI may use React Flow directly. The spec and document generation should not care.

### 5.4 Graph rendering rules

For WorkflowAPI maps:

- business-visible steps first;
- internal/hidden steps collapsed by default;
- failed/slow runtime overlay states shown only when metrics loaded;
- edges labeled with conditions where available;
- grouped steps for technical clusters;
- deep links to raw document and runtime execution details.

---

## 6. Runtime overlays

### 6.1 Optional by design

The reference UI can run without runtime metrics.

Runtime overlays require:

- Temporal connection;
- permissions;
- namespace visibility;
- optional history indexing;
- metrics backend integration.

### 6.2 Overlay sources

Possible sources:

```text
Temporal Visibility API
Temporal CountWorkflowExecutions
Temporal GetWorkflowExecutionHistory
OpenTelemetry metrics/traces
Prometheus
DataDog
Postgres/ClickHouse runtime indexer
```

### 6.3 Overlay API

For the catalog/reference UI, expose a separate API:

```http
GET /api/runtime/workflows/{workflowKey}/metrics?from=...&to=...
GET /api/runtime/workflows/{workflowKey}/steps/{stepKey}/metrics?from=...&to=...
GET /api/runtime/workflows/{workflowKey}/instances?status=failed&from=...&to=...
```

Do not embed runtime metrics in the static WorkflowAPI document.

### 6.4 Runtime graph overlay

Attach metrics by stable IDs:

```text
workflow key
step key
activity type
Temporal workflowType
Temporal activityType
Nexus service/operation
```

---

## 7. Aspire integration

### 7.1 Per-service UI in Aspire

No extra resource required.

The service exposes its own reference UI:

```csharp
app.MapWorkflowApi();
app.MapWorkflowApiReference();
```

Aspire dashboard shows the normal service HTTP endpoint.

### 7.2 Multi-service catalog in Aspire

Add a catalog container only when collation is desired:

```csharp
var temporal = builder.AddTemporalServer("temporal");

var riskWorker = builder.AddProject<Projects.Risk_Worker>("commerce-order-worker")
    .WithReference(temporal);

var offerWorker = builder.AddProject<Projects.Offer_Worker>("offer-worker")
    .WithReference(temporal);

builder.AddWorkflowApiCatalog("workflow-catalog")
    .WithCatalogSource(riskWorker)
    .WithCatalogSource(offerWorker)
    .WithTemporal(temporal, namespaceName: "default");
```

### 7.3 Aspire extension API

Package:

```text
Aspire.Hosting.WorkflowApiCatalog
```

Methods:

```csharp
AddWorkflowApiCatalog(name)
WithCatalogSource(project)
WithCatalogSource(project, path)
WithCatalogSourceFile(path)
WithCatalogSourceDirectory(path)
WithTemporal(temporalResource, namespaceName)
WithRuntimeOverlay()
WithExternalHttpEndpoints()
```

Default convention:

```text
WithCatalogSource(project) -> /.well-known/workflow-api.json
```

Override only when needed.

---

## 8. Catalog ingestion modes

### 8.1 Live endpoint ingestion

```yaml
sources:
  - name: order-service
    url: http://order-service/.well-known/workflow-api.json
```

Good for local Aspire and dynamic environments.

### 8.2 CI artifact ingestion

```yaml
sources:
  - name: order-service
    file: artifacts/order-service/workflow-api.json
    version: 1.4.2
    commit: abc123
```

Good for enterprise governance.

### 8.3 Git repo ingestion

```yaml
sources:
  - name: order-service
    repository: https://github.com/company/order-service
    path: artifacts/workflow-api/v1.json
```

### 8.4 OCI/package artifact ingestion

```yaml
sources:
  - name: order-service
    oci: ghcr.io/company/order-service-workflowapi:1.4.2
```

This can be added later.

---

## 9. UI information architecture

### 9.1 Per-service UI navigation

```text
Commerce Order Workflow API
  Overview
  Workflows
    OrderFulfilmentWorkflow
    MonthlyRiskRefreshWorkflow
  Activities
  Nexus Services
  Schemas
  Runtime Binding
  Raw JSON
  Diagnostics
```

### 9.2 Catalog UI navigation

```text
WorkflowAPI Catalog
  Search
  Hosts
  Workflows
  Nexus
  Dependencies
  Domains
  Owners
  Runtime Health
  Drift
  Documents
  Settings
```

### 9.3 Search

Search fields:

- workflow name/title;
- activity name/title;
- Nexus service/operation;
- owner;
- domain;
- tag;
- namespace;
- task queue;
- DTO/schema name;
- external system dependency.

### 9.4 Filters

- runtime binding;
- host;
- namespace;
- task queue;
- domain;
- owner;
- lifecycle;
- visibility;
- data classification;
- runtime status if overlay enabled.

---

## 10. Security model

### 10.1 Per-service UI

Default development:

- anonymous allowed in Development environment;
- disabled or authorization-required in Production.

Example:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapWorkflowApiReference();
}

app.MapWorkflowApi()
    .RequireAuthorization("WorkflowApiRead");
```

### 10.2 Central catalog

Must support:

- SSO/OIDC;
- team/role based access;
- document visibility policies;
- hiding sensitive bindings/secrets;
- audit logging for runtime drill-down;
- environment separation.

### 10.3 Sensitive data

Do not expose:

- secrets;
- API keys;
- private certificate paths;
- example payloads containing PII;
- raw workflow histories with sensitive data unless explicitly authorized.

---

## 11. AI-agent readiness

The catalog should be AI-friendly later:

- structured JSON documents;
- stable IDs;
- owner/domain metadata;
- relationship graph;
- raw spec download;
- generated Markdown summaries;
- optional MCP/search endpoint.

Potential future endpoint:

```http
GET /api/catalog/search?q=risk enrichment dnb
GET /api/catalog/workflows/order-fulfilment/context
```

For now, focus on deterministic documents and searchable catalog data.

---

## 12. Rubber-duck review

### What works

- Single-service UI mirrors Scalar/OpenAPI pattern.
- Catalog container is optional for local single-service development.
- Aspire integration uses conventions.
- EventCatalog visualiser is used as a renderer, not as a spec dependency.
- Runtime overlays stay separate from static contract documents.

### Risks

1. **UI becoming the spec**  
   Avoid leaking visualiser node shape into WorkflowAPI.

2. **Catalog becoming mandatory too early**  
   Keep per-service reference UI usable alone.

3. **Runtime overlay scope creep**  
   Do not require history indexing in MVP.

4. **Security oversights**  
   Specs can reveal internals; production exposure must be controlled.

5. **Visual clutter**  
   Business maps should collapse internal steps and reveal detail on demand.

### MVP UI recommendation

First build:

```text
MapWorkflowApiReference()
  reads one document
  shows workflows/operations/schemas
  renders simple step graph
  validates and shows diagnostics
```

Then build:

```text
WorkflowApiCatalog container
  imports multiple document URLs
  shows hosts/workflows/dependencies
  supports Aspire catalog source conventions
```

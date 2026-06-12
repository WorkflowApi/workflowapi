> **v1 tightened scope note**  
> This file is retained as a post-v1 design note unless a section explicitly says otherwise. WorkflowAPI v1 is limited to the compact durable execution workflow API specification, .NET generation from workflow attributes or fluent definitions, Temporal static binding metadata, and static workflow display. Runtime overlays, catalogue collation, source polling, workflow control-plane actions, BPMN-style modelling and runtime observability are outside v1 scope.

# WorkflowAPI UI Core and Runtime Plugin Architecture

## Status

Proposed v0.1 design supplement.

This document clarifies the final UI architecture for WorkflowAPI. The important decision is that the user interface must be **generic for WorkflowAPI documents** and must not be coupled to Temporal. Temporal is the first runtime binding and the first runtime overlay plugin, but the UI core must remain capable of rendering any valid WorkflowAPI document.

```text
WorkflowAPI document
        ↓
Generic WorkflowAPI UI Core
        ↓
Reference / graph / schema / topology views
        ↓
Optional runtime overlay plugin
        ↓
Temporal metrics, errors, durations, Nexus/bridge status, deep links
```

This is intentionally analogous to the modern OpenAPI ecosystem:

```text
OpenAPI document generation      !=  OpenAPI UI
WorkflowAPI document generation  !=  WorkflowAPI UI
WorkflowAPI UI core              !=  Temporal runtime overlay
```

## Goals

The WorkflowAPI UI architecture must support:

1. A **single-service reference UI**, similar in usage style to Scalar or Swagger UI for OpenAPI.
2. A **standalone multi-service catalog UI**, similar in spirit to EventCatalog or HealthChecks.UI-style endpoint collation.
3. Static operation with **no runtime credentials**.
4. Optional live/cached runtime overlays when a runtime plugin is configured.
5. A generic graph renderer for WorkflowAPI topology, independent of Temporal.
6. Temporal as the first runtime overlay plugin.
7. A future path for additional runtimes such as Durable Functions, Dapr Workflow, Camunda, Conductor, or custom engines.

## Non-goals

The WorkflowAPI UI must not become:

- a Temporal Web replacement;
- a Temporal control plane;
- a workflow execution editor;
- a low-code workflow designer;
- a write-capable Temporal admin tool;
- a UI that requires live runtime access to be useful.

The UI may link to Temporal Web for technical drill-down, but it should not duplicate Temporal Web's full execution-history/debugging experience.

## Package split

Recommended packages:

```text
WorkflowApi.Abstractions
  Spec model, attributes, validation primitives.

WorkflowApi.AspNetCore
  AddWorkflowApi(), MapWorkflowApi(), document generation.

WorkflowApi.Reference
  MapWorkflowApiReference(), embedded single-service reference UI.

WorkflowApi.Catalog.Server
  Standalone .NET app for configured multi-source catalog collation.

WorkflowApi.Catalog.Core
  Catalog collation, graph merge, source cache, diagnostics.

WorkflowApi.Runtime.Abstractions
  Runtime overlay provider contracts and DTOs.

WorkflowApi.Runtime.Temporal
  Temporal runtime overlay provider.

WorkflowApi.Ui.Core
  Frontend WorkflowAPI document viewer and graph model.

WorkflowApi.Ui.TemporalOverlay
  Frontend overlay widgets for Temporal-specific runtime data.

Aspire.Hosting.WorkflowApiCatalog
  Aspire hosting extension for the standalone catalog container.
```

Recommended npm packages, if the frontend is split independently:

```text
@workflowapi/ui-core
@workflowapi/visualiser-adapter
@workflowapi/runtime-overlays
@workflowapi/temporal-overlay
```

The exact package names can evolve, but the dependency direction must remain:

```text
UI Core       -> WorkflowAPI document model
Temporal UI   -> UI Core + runtime overlay abstractions
Catalog UI    -> UI Core + catalog APIs
```

The generic UI core must not depend directly on `Temporalio.*`, Temporal credentials, or Temporal runtime APIs.

## UI modes

### 1. Static reference mode

Input:

```text
One workflow-api.json document
```

No runtime credentials. No live metrics. No catalog server required.

Used by:

```text
app.MapWorkflowApiReference()
local service debugging
CI-published static documentation
read-only internal reference pages
```

Capabilities:

- document overview;
- workflow list;
- workflow detail pages;
- run/signal/query/update documentation;
- schema viewer;
- topology graph;
- steps and edges;
- nested workflows/subflows;
- bridge documentation;
- binding metadata display;
- copy/download raw WorkflowAPI document.

### 2. Cached catalog mode

Input:

```text
Many workflow-api.json documents
Catalog source configuration
Memory or persistent cache
```

No live runtime credentials required.

Used by:

```text
standalone Docker image
Kubernetes-hosted catalog
Aspire local multi-service collation
central internal catalog without Temporal access
```

Capabilities:

- federated WorkflowAPI source list;
- source fetch status;
- stale-source handling;
- merged host/workflow/bridge graph;
- cross-host dependency map;
- duplicate/conflict diagnostics;
- environment partitioning;
- search across all workflows, steps, bridges, owners, domains, tags.

### 3. Runtime overlay mode

Input:

```text
WorkflowAPI documents
Runtime plugin configuration
Runtime credentials / endpoint configuration
Time range selected by user
```

Used by:

```text
business observability
development diagnostics
operations dashboards
runtime drift detection
```

Capabilities:

- execution counts;
- success/failure/timed-out/running counts;
- average/p50/p95/p99 durations;
- retry counts;
- SLA breach counts;
- recent failed examples;
- bridge/Nexus operation metrics where available;
- Temporal Web deep links;
- runtime-vs-spec drift warnings.

## Graph ownership rule

The WorkflowAPI document owns the declared graph.

The runtime overlay plugin decorates that graph.

```text
Declared topology  -> from WorkflowAPI document
Runtime metrics    -> from plugin
Observed topology  -> optional diagnostics view only
```

A runtime plugin must not silently replace the declared graph. If runtime histories suggest a different observed path, the UI may expose an explicit **Observed Runtime Graph** or **Drift** view, but the primary graph remains the declared WorkflowAPI graph.

## Generic UI core responsibilities

The UI core must understand the WorkflowAPI DSL concepts:

```text
Document
Host
Workflow
Run operation
Signal operation
Query operation
Update operation
Step
Edge
Subflow
Nested workflow
Bridge
Dependency
Binding
Schema
Example
Policy
Security
Tag
Owner/domain metadata
```

It should provide generic views:

1. **Document overview**
   - title, version, description;
   - host/application metadata;
   - bindings summary;
   - workflow count;
   - bridge count;
   - source and validation status.

2. **Workflow list**
   - workflow id;
   - title/summary;
   - owner/domain/tags;
   - binding badges;
   - deprecation status;
   - optional overlay health badge.

3. **Workflow detail**
   - run input/output;
   - signals;
   - queries;
   - updates;
   - steps;
   - dependencies;
   - bridges called;
   - child workflows/subflows;
   - schemas/examples.

4. **Graph view**
   - declared topology;
   - nested/expandable workflows;
   - subflow grouping;
   - bridges as cross-boundary nodes/edges;
   - dependency edges;
   - optional metric overlays.

5. **Schema view**
   - JSON Schema objects;
   - request/result payloads;
   - examples;
   - schema references.

6. **Diagnostics view**
   - validation errors;
   - warnings;
   - missing references;
   - duplicate IDs;
   - stale source data in catalog mode;
   - runtime overlay errors.

## EventCatalog visualiser usage

`@eventcatalog/visualiser` should be treated as a graph renderer/visualisation dependency, not as the catalog product runtime.

Recommended adapter pipeline:

```text
WorkflowAPI graph model
        ↓
WorkflowApiGraphAdapter
        ↓
EventCatalog visualiser graph node/edge model
        ↓
@eventcatalog/visualiser NodeGraph / graph renderer
        ↓
WorkflowAPI side panels and overlays
```

The adapter should map generic WorkflowAPI concepts to visual concepts:

| WorkflowAPI concept | Visual concept |
|---|---|
| workflow | flow / grouped graph |
| run | start node / operation |
| activity step | step node |
| child workflow | nested workflow node |
| inline subflow | collapsed/expandable group |
| bridge | bridge/cross-boundary node |
| external system | external-system node |
| signal | inbound event/control node |
| update | command/control node |
| query | read/query node |
| timer/wait | wait/timer node |
| dependency edge | dependency edge |
| bridge edge | cross-boundary edge |

The UI must not expose EventCatalog internals as WorkflowAPI public model. The public model is WorkflowAPI; the renderer can be replaced later.

## Runtime overlay abstraction

Runtime overlays should be provided through a runtime-neutral backend abstraction. The browser should not connect directly to Temporal or other runtimes.

### Backend interface concept

```csharp
public interface IWorkflowRuntimeOverlayProvider
{
    string Id { get; }
    string DisplayName { get; }

    bool Supports(WorkflowApiDocument document);

    Task<WorkflowRuntimeCapabilities> GetCapabilitiesAsync(
        WorkflowRuntimeContext context,
        CancellationToken cancellationToken);

    Task<WorkflowMetricsResult> GetWorkflowMetricsAsync(
        WorkflowMetricsRequest request,
        CancellationToken cancellationToken);

    Task<StepMetricsResult> GetStepMetricsAsync(
        StepMetricsRequest request,
        CancellationToken cancellationToken);

    Task<BridgeMetricsResult> GetBridgeMetricsAsync(
        BridgeMetricsRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RuntimeDeepLink>> GetDeepLinksAsync(
        RuntimeDeepLinkRequest request,
        CancellationToken cancellationToken);
}
```

### Frontend provider concept

```ts
export interface WorkflowRuntimeOverlayProvider {
  id: string;
  displayName: string;
  supports(document: WorkflowApiDocument): boolean;

  getWorkflowMetrics(request: WorkflowMetricsRequest): Promise<WorkflowMetrics>;
  getStepMetrics(request: StepMetricsRequest): Promise<StepMetrics>;
  getBridgeMetrics(request: BridgeMetricsRequest): Promise<BridgeMetrics>;
  getDeepLinks(request: DeepLinkRequest): Promise<RuntimeDeepLink[]>;
}
```

The actual frontend may call catalog/reference backend APIs rather than invoking plugins directly. The important design boundary is that the overlay contract is runtime-neutral.

## Runtime overlay DTOs

### Time range

```json
{
  "from": "2026-06-01T00:00:00Z",
  "to": "2026-06-10T00:00:00Z",
  "preset": "last-7-days"
}
```

### Workflow metrics

```json
{
  "workflowId": "order-fulfilment",
  "runtime": "temporal",
  "timeRange": {
    "from": "2026-06-01T00:00:00Z",
    "to": "2026-06-10T00:00:00Z"
  },
  "counts": {
    "started": 2431,
    "completed": 2353,
    "failed": 12,
    "timedOut": 3,
    "terminated": 1,
    "running": 62
  },
  "durations": {
    "averageMs": 84200,
    "p50Ms": 41000,
    "p95Ms": 188000,
    "p99Ms": 382000
  },
  "sla": {
    "thresholdMs": 120000,
    "breaches": 182,
    "breachRate": 0.0748
  },
  "links": [
    {
      "label": "Open in Temporal Web",
      "url": "https://temporal.example/namespaces/Commerce.OrderService/workflows?query=..."
    }
  ]
}
```

### Step metrics

```json
{
  "workflowId": "order-fulfilment",
  "stepId": "take-payment",
  "runtime": "temporal",
  "counts": {
    "scheduled": 2398,
    "started": 2398,
    "completed": 2353,
    "failed": 12,
    "timedOut": 1,
    "retried": 73
  },
  "durations": {
    "averageMs": 1840,
    "p50Ms": 1200,
    "p95Ms": 8400,
    "p99Ms": 21100
  },
  "topFailures": [
    {
      "type": "DnbRateLimitedException",
      "count": 7
    },
    {
      "type": "TimeoutException",
      "count": 2
    }
  ]
}
```

### Bridge metrics

```json
{
  "bridgeId": "risk-to-document-generate-pdf",
  "runtime": "temporal",
  "kind": "nexus-operation",
  "counts": {
    "calls": 1288,
    "completed": 1274,
    "failed": 14
  },
  "durations": {
    "averageMs": 2200,
    "p95Ms": 9800
  },
  "source": {
    "workflowId": "order-fulfilment",
    "stepId": "generate-pdf"
  },
  "target": {
    "service": "DocumentService",
    "operation": "GeneratePdf"
  }
}
```

## UI overlay rendering rules

The generic UI should define common overlay slots:

```text
nodeBadge
nodeStatus
nodeFooter
edgeBadge
edgeStatus
sidePanelSection
detailMetricCards
workflowSummaryCards
bridgeStatusPanel
runtimeLinks
```

Runtime plugins should provide data, not arbitrary uncontrolled UI whenever possible. Plugin-specific UI components may be allowed in designated extension slots.

Example rendering:

```text
Node: Take payment
Base spec:
  kind: activity
  activityType: TakePaymentActivity
  summary: Calls payment provider and stores enrichment results.

Temporal overlay:
  2,398 scheduled
  12 failed
  73 retries
  p95 8.4s
```

## Temporal runtime overlay plugin

The Temporal overlay plugin is the first runtime implementation.

### Supports condition

The plugin supports a document if:

```text
document.bindings.temporal exists
or workflow.bindings.temporal exists
or host.runtime == "temporal"
```

### Configuration

Typical configuration:

```json
{
  "WorkflowApi": {
    "RuntimeOverlays": {
      "Temporal": {
        "Enabled": true,
        "Address": "temporal-frontend:7233",
        "Namespace": "Commerce.OrderService",
        "WebUrl": "https://temporal.example",
        "Authentication": {
          "Mode": "ApiKey"
        },
        "Metrics": {
          "Mode": "VisibilityAndHistory",
          "MaxHistorySamplesPerWorkflow": 500,
          "CacheDuration": "00:05:00"
        }
      }
    }
  }
}
```

The plugin must support at least:

```text
Disabled mode
Visibility-only mode
Visibility + sampled history mode
Cached/indexed mode
```

### Temporal data sources

The plugin may use:

1. Temporal Visibility APIs for workflow-level listing/counting/filtering.
2. Workflow histories for detailed step/activity/child-workflow/timer data.
3. A cache or analytics store for fast repeated queries.
4. Existing metrics backends in future versions.

### Temporal metric limitations

The plugin must clearly distinguish:

```text
exact values
sampled/estimated values
unavailable values
values requiring history indexing
```

Example UI labels:

```text
p95 activity duration: estimated from 500 sampled executions
retry count: inferred from histories
workflow count: from Temporal visibility
```

### Temporal Web deep links

The plugin should generate deep links when `Temporal:WebUrl` is configured.

Deep links should include:

```text
workflow list filtered by workflow type/time/status when possible
specific workflow execution links for failed examples
namespace-specific links
```

The plugin should never expose Temporal credentials to the browser.

## Catalog server plugin loading

For v0.1, plugin loading should be static and DI-based.

```csharp
builder.Services.AddWorkflowApiCatalog()
    .AddRuntimeOverlay<TemporalRuntimeOverlayProvider>();
```

Avoid dynamic arbitrary plugin loading in v0.1. It creates security, versioning, deployment, and support complexity.

Later versions may support plugin packages, but v0.1 should prefer compile-time package references.

## Reference UI plugin loading

For single-service `MapWorkflowApiReference()`, runtime overlays should be optional.

```csharp
app.MapWorkflowApiReference(options =>
{
    options.EnableRuntimeOverlays = true;
});
```

If no overlay provider is registered, the reference UI should still work as a static DSL viewer.

## Backend API for UI overlays

The UI should talk to generic backend endpoints:

```http
GET /workflow-api/{documentName}.json
GET /workflow-api/ui/config
GET /workflow-api/runtime/capabilities
GET /workflow-api/runtime/workflows/{workflowId}/metrics?from=...&to=...
GET /workflow-api/runtime/workflows/{workflowId}/steps/{stepId}/metrics?from=...&to=...
GET /workflow-api/runtime/bridges/{bridgeId}/metrics?from=...&to=...
GET /workflow-api/runtime/workflows/{workflowId}/executions?status=failed&from=...&to=...
```

The standalone catalog may expose equivalent catalog-prefixed endpoints:

```http
GET /api/catalog/documents
GET /api/catalog/graph
GET /api/runtime/workflows/{workflowId}/metrics?from=...&to=...
GET /api/runtime/steps/{stepId}/metrics?from=...&to=...
GET /api/runtime/bridges/{bridgeId}/metrics?from=...&to=...
```

The frontend should not need to know whether the data came from Temporal, a cache, or another runtime.

## Graph overlay data flow

```text
WorkflowAPI document
        ↓
Build declared graph
        ↓
Render generic graph
        ↓
User selects time range
        ↓
UI requests overlay metrics from backend
        ↓
Runtime provider returns metrics
        ↓
UI decorates nodes/edges/side panels
```

The UI should support delayed/partial overlay loading. The graph should render immediately from the spec, then metric overlays can hydrate asynchronously.

## Time range selection

The UI should provide standard time presets:

```text
Last 15 minutes
Last 1 hour
Last 24 hours
Last 7 days
Last 30 days
Custom range
```

Time range state should apply consistently to:

```text
workflow summary cards
node metrics
edge metrics
bridge metrics
failed execution lists
runtime deep links
```

## Runtime overlay cache

Runtime overlay providers should cache expensive queries.

For standalone catalog v0.1:

```text
Memory cache is sufficient.
Cache key includes:
  runtime provider id
  document/source id
  workflow id
  step id or bridge id
  time range
  filter set
```

Cache entries should record:

```text
createdAt
expiresAt
source
freshness
isPartial
errors/warnings
```

The UI should be able to show stale/partial overlay warnings.

## Security model

Runtime overlays introduce sensitive data risks. The design must enforce:

1. Runtime credentials stay server-side.
2. UI receives only sanitized metrics and approved deep links.
3. Failed execution examples should avoid exposing payloads by default.
4. Search Attributes may contain business identifiers; display must be configurable.
5. Runtime overlays must be read-only in v0.1.
6. Catalog source specs and runtime metrics should be separately permissioned if RBAC is added.

The generic WorkflowAPI document may be less sensitive than live execution data, but both are internal by default unless explicitly published.

## Drift detection

A runtime overlay plugin can optionally report drift:

```text
Observed workflow type not present in spec
Observed activity type not declared as a step
Declared step never observed in selected time range
Bridge declared but no runtime calls observed
Runtime failures concentrated on undeclared/technical activity
Task queue mismatch between spec binding and observed execution
```

Drift should be displayed as diagnostics, not by mutating the declared graph.

## Product positioning

The UI product line should be described as:

```text
WorkflowAPI Reference
  Single-service reference UI for a WorkflowAPI document.

WorkflowAPI Catalog
  Multi-service catalog that collates many WorkflowAPI documents.

WorkflowAPI Runtime Overlay
  Optional plugin system that decorates WorkflowAPI documents with runtime facts.

WorkflowAPI Temporal Overlay
  First runtime overlay implementation for Temporal.
```

Avoid describing the UI as simply:

```text
Temporal visualizer
```

That would weaken the generic WorkflowAPI positioning.

## Implementation roadmap

### Phase 1: Generic static reference UI

- Render one WorkflowAPI document.
- List workflows, operations, steps, bridges, schemas.
- Render declared graph via adapter.
- No runtime plugin support required.

### Phase 2: Static catalog UI

- Load many documents.
- Merge graph.
- Show source status and diagnostics.
- Still no runtime plugin required.

### Phase 3: Runtime overlay abstractions

- Add backend runtime overlay provider interface.
- Add generic runtime overlay API endpoints.
- Add frontend overlay slots.
- Add time-range selector.

### Phase 4: Temporal overlay plugin

- Implement visibility-based workflow metrics.
- Add Temporal Web deep links.
- Add sampled history parsing for step metrics.
- Add cache and partial result warnings.

### Phase 5: Drift diagnostics

- Compare declared topology to observed runtime data.
- Display warnings and suggested spec improvements.

## Acceptance criteria

A v0.1 implementation is acceptable when:

1. A service can expose `workflow-api.json` and `MapWorkflowApiReference()` without any Temporal runtime credentials.
2. The reference UI can render the declared workflow graph from the DSL only.
3. The standalone catalog can load multiple WorkflowAPI sources and render a merged graph.
4. Runtime overlays are optional and do not block static rendering.
5. The Temporal overlay can be enabled by configuration and displays workflow counts over a selected time range.
6. Temporal credentials never reach the browser.
7. The same UI core is used by the reference UI and the catalog UI.
8. The graph generated from the spec remains the primary graph; runtime-observed graphs are explicitly labelled diagnostics.


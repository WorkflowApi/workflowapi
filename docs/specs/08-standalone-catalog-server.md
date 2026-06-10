# 08 — Standalone WorkflowAPI Catalog Server

Status: proposed v0.1 design  
Audience: catalog/runtime/UI implementation sub-agents  
Scope: a standalone .NET application/container that can run without hosting workflows itself, gather WorkflowAPI documents from other services, merge them into a graph, and optionally overlay Temporal runtime statistics.

---

## 1. Purpose

WorkflowAPI supports two complementary UI modes:

1. **Per-service WorkflowAPI reference**  
   A workflow-owning .NET application exposes its own `/.well-known/workflow-api.json` and optional local reference UI, similar to how an ASP.NET Core app exposes OpenAPI plus Scalar.

2. **Standalone WorkflowAPI Catalog Server**  
   A separate .NET application that does not need to contain workflows. It is configured with a list of WorkflowAPI source endpoints/files, periodically fetches and caches them, merges them into a single catalog graph, and optionally connects to Temporal to show runtime statistics.

This document defines the second mode.

The design is intentionally similar in spirit to the older `AspNetCore.Diagnostics.HealthChecks.UI` pattern: a standalone UI process can be configured with named endpoints to poll, cache, display, and refresh. The HealthChecks.UI sample uses a configuration section containing `HealthChecks` endpoint entries with `Name` and `Uri`, plus evaluation/notification timing settings. WorkflowAPI Catalog should use the same simple operational pattern, but for WorkflowAPI documents rather than health check endpoints.

---

## 2. Non-goals

The standalone catalog must not be required for single-service local development.

The standalone catalog must not require workflow implementations in its own process.

The standalone catalog must not mutate Temporal state.

The standalone catalog must not be the source of truth for WorkflowAPI definitions. Source services or build artifacts own their WorkflowAPI documents.

The standalone catalog must not require persistent storage for local/Aspire usage. A memory-cache-only mode is required.

---

## 3. Use cases

### 3.1 Local Aspire collation

A developer runs several services locally via Aspire:

```csharp
var riskWorker = builder.AddProject<Projects.Risk_Worker>("risk-worker");
var offerWorker = builder.AddProject<Projects.Offer_Worker>("offer-worker");

builder.AddWorkflowApiCatalog("workflow-catalog")
    .WithCatalogSource(riskWorker)
    .WithCatalogSource(offerWorker);
```

The catalog fetches:

```text
http://risk-worker/.well-known/workflow-api.json
http://offer-worker/.well-known/workflow-api.json
```

and renders a combined graph.

### 3.2 Docker-based local testing

A developer runs the catalog as a Docker image and passes sources via environment variables or mounted configuration:

```bash
docker run --rm -p 8080:8080 \
  -e WorkflowApiCatalog__Sources__0__Name=risk-worker \
  -e WorkflowApiCatalog__Sources__0__Uri=http://host.docker.internal:5010/.well-known/workflow-api.json \
  -e WorkflowApiCatalog__Sources__1__Name=offer-worker \
  -e WorkflowApiCatalog__Sources__1__Uri=http://host.docker.internal:5020/.well-known/workflow-api.json \
  ghcr.io/workflowapi/workflowapi-catalog:latest
```

### 3.3 Kubernetes internal catalog

A platform team deploys the catalog into a cluster. It is configured with internal service URLs:

```json
{
  "WorkflowApiCatalog": {
    "Sources": [
      {
        "Name": "risk-worker",
        "Uri": "http://risk-worker.risk.svc.cluster.local/.well-known/workflow-api.json"
      },
      {
        "Name": "offer-worker",
        "Uri": "http://offer-worker.offer.svc.cluster.local/.well-known/workflow-api.json"
      }
    ]
  }
}
```

### 3.4 Temporal runtime overlay

The catalog is configured with read-only Temporal connection details. Users can select a time range such as last hour, 24 hours, 7 days, 30 days, or custom dates. The UI overlays execution counts, failures, retries, duration statistics, and status breakdowns onto the declared WorkflowAPI graph.

---

## 4. Application architecture

```text
┌───────────────────────────────────────────────────────────────┐
│ WorkflowAPI Catalog Server                                     │
│                                                               │
│  ┌────────────────────┐      ┌─────────────────────────────┐  │
│  │ Source Poller       │─────▶│ In-memory Catalog Cache      │  │
│  └────────────────────┘      └─────────────────────────────┘  │
│          │                                │                    │
│          ▼                                ▼                    │
│  ┌────────────────────┐      ┌─────────────────────────────┐  │
│  │ WorkflowAPI Parser  │─────▶│ Catalog Graph Builder        │  │
│  └────────────────────┘      └─────────────────────────────┘  │
│                                           │                    │
│                                           ▼                    │
│  ┌────────────────────┐      ┌─────────────────────────────┐  │
│  │ Temporal Overlay    │─────▶│ API + UI Models              │  │
│  │ Adapter             │      └─────────────────────────────┘  │
│  └────────────────────┘                    │                    │
│          │                                ▼                    │
│          ▼                       React UI / Visualiser          │
│  Temporal Visibility/History                                  │
└───────────────────────────────────────────────────────────────┘
```

### 4.1 Source Poller

Responsible for fetching configured WorkflowAPI documents from:

- HTTP/HTTPS URLs;
- convention-based service endpoints;
- local files;
- mounted directories;
- optional future Git/OCI/package sources.

### 4.2 In-memory Catalog Cache

The default local mode uses memory only. It stores:

- raw document content;
- parsed WorkflowAPI documents;
- fetch status;
- ETag/Last-Modified metadata where available;
- last successful fetch time;
- last error;
- merged graph projection;
- runtime overlay cache by time-range key.

### 4.3 Catalog Graph Builder

Responsible for merging many WorkflowAPI documents into a single graph of:

- workflow hosts;
- workflows;
- activities;
- steps;
- signals;
- queries;
- updates;
- Nexus services;
- Nexus operations;
- external systems;
- Temporal namespaces;
- task queues;
- dependency edges.

### 4.4 Temporal Overlay Adapter

Optional. Responsible for read-only Temporal queries and derived metrics. It must be separable from the static catalog path so the catalog remains useful without Temporal credentials.

---

## 5. Configuration model

The configuration should be intentionally simple and appsettings-friendly.

```json
{
  "WorkflowApiCatalog": {
    "Title": "Local WorkflowAPI Catalog",
    "Sources": [
      {
        "Name": "risk-worker",
        "Uri": "http://risk-worker/.well-known/workflow-api.json"
      },
      {
        "Name": "offer-worker",
        "Uri": "http://offer-worker/.well-known/workflow-api.json"
      }
    ],
    "Polling": {
      "Enabled": true,
      "IntervalSeconds": 30,
      "InitialFetchTimeoutSeconds": 10,
      "UseConditionalRequests": true
    },
    "Cache": {
      "Mode": "Memory",
      "KeepLastSuccessfulDocument": true,
      "OverlayTtlSeconds": 60
    },
    "Temporal": {
      "Enabled": true,
      "DefaultConnection": "local",
      "Connections": [
        {
          "Name": "local",
          "TargetHost": "temporal:7233",
          "Namespace": "default",
          "WebUrl": "http://temporal-ui:8233"
        }
      ]
    }
  }
}
```

### 5.1 Source configuration

```json
{
  "Name": "risk-worker",
  "Uri": "http://risk-worker/.well-known/workflow-api.json",
  "Enabled": true,
  "Kind": "Http",
  "Tags": ["risk", "local"],
  "TimeoutSeconds": 10
}
```

Required:

- `Name`
- one of `Uri`, `File`, `Directory`, or future source-specific options

Optional:

- `Enabled`
- `Kind`
- `Tags`
- `TimeoutSeconds`
- `Headers`
- `Auth`

### 5.2 Convention-based source configuration

For Aspire and service-discovery scenarios, callers may provide only a base URI. The catalog app should append the default well-known path.

```json
{
  "Name": "risk-worker",
  "BaseUri": "http://risk-worker",
  "UseWellKnown": true
}
```

Equivalent resolved URI:

```text
http://risk-worker/.well-known/workflow-api.json
```

### 5.3 File source configuration

```json
{
  "Name": "risk-worker",
  "File": "/workflowapi/sources/risk-worker.workflow-api.json"
}
```

### 5.4 Directory source configuration

```json
{
  "Name": "local-artifacts",
  "Directory": "/workflowapi/sources",
  "Pattern": "*.workflow-api.json"
}
```

### 5.5 Temporal credentials

The catalog must support at least:

- no Temporal overlay;
- local unsecured Temporal dev server;
- TLS/mTLS;
- API-key style cloud connection;
- environment-variable secrets;
- mounted certificate files.

Example mTLS shape:

```json
{
  "WorkflowApiCatalog": {
    "Temporal": {
      "Enabled": true,
      "Connections": [
        {
          "Name": "prod-risk",
          "TargetHost": "risk.x.tmprl.cloud:7233",
          "Namespace": "b2b-risk.prod",
          "Tls": {
            "Enabled": true,
            "ClientCertPath": "/secrets/temporal/client.crt",
            "ClientKeyPath": "/secrets/temporal/client.key",
            "ServerName": "risk.x.tmprl.cloud"
          },
          "WebUrl": "https://cloud.temporal.io/namespaces/b2b-risk.prod"
        }
      ]
    }
  }
}
```

Secrets must never be written into fetched WorkflowAPI documents or exported catalog graph JSON.

---

## 6. Fetching and cache behavior

### 6.1 Startup behavior

On startup:

1. Load configuration.
2. Resolve each source URI/file path.
3. Fetch all enabled sources concurrently with bounded parallelism.
4. Parse and validate each WorkflowAPI document.
5. Store successful documents in memory.
6. Build the merged graph.
7. Start background polling if enabled.

The UI must be available even if some sources fail. It should show partial results and source health.

### 6.2 Polling behavior

The catalog should periodically refresh source documents.

Recommended defaults:

```json
{
  "Polling": {
    "Enabled": true,
    "IntervalSeconds": 30,
    "UseConditionalRequests": true,
    "MaxParallelism": 8
  }
}
```

Conditional HTTP requests should use `ETag` and `Last-Modified` when available.

### 6.3 Failure behavior

If a source fails:

- keep the last successful document if configured;
- mark the source as stale/degraded;
- surface the error in the UI;
- do not remove graph nodes immediately unless explicitly configured.

Suggested status states:

```text
Healthy
Stale
Failed
Disabled
InvalidDocument
Unauthorized
Timeout
```

### 6.4 Memory cache keys

Recommended keys:

```text
source:{sourceName}:raw
source:{sourceName}:document
catalog:merged:v1
runtime:{connection}:{namespace}:{workflowType}:{from}:{to}:{filtersHash}
```

---

## 7. Merge and overlay algorithm

The catalog merges documents in stages.

### 7.1 Parse stage

Each source document becomes a `WorkflowApiDocument` with a source identity:

```json
{
  "sourceName": "risk-worker",
  "sourceUri": "http://risk-worker/.well-known/workflow-api.json",
  "document": { }
}
```

### 7.2 Normalize stage

Normalize identities:

- workflow host key: `host.name` plus optional environment;
- workflow key: `runtime + namespace + workflowType` when a runtime binding exists, otherwise `documentId + workflowId`;
- activity key: `runtime + activityType` scoped by host where necessary;
- Nexus service key: `runtime + serviceName`;
- Nexus operation key: `serviceKey + operationName`.

### 7.3 Merge stage

The merged catalog contains:

```text
Sources
Hosts
Workflows
Activities
Steps
Nexus services
Nexus operations
External systems
Runtime bindings
Edges
Conflicts
Warnings
```

### 7.4 Conflict handling

Examples:

- two documents define the same workflow key with incompatible input schemas;
- a workflow depends on a Nexus operation that no document implements;
- a host declares a Temporal namespace but no task queue;
- a workflow has steps but no edges;
- an activity appears in a graph but no activity declaration exists.

Conflicts should be visible in the UI. They should not break the whole catalog.

### 7.5 Overlay stage

Runtime overlays should be attached after the static merge.

Attach metrics using stable binding keys:

```text
Temporal workflow overlay:
  namespace + workflowType + timeRange + filters

Temporal activity overlay:
  namespace + workflowType + activityType + timeRange + filters

Nexus overlay:
  namespace + endpoint/service/operation + timeRange + filters, where available
```

---

## 8. Temporal runtime overlay

### 8.1 Time range selection

The UI should support:

- last 15 minutes;
- last hour;
- last 24 hours;
- last 7 days;
- last 30 days;
- custom absolute range.

All range calculations should be explicit and time-zone aware. The API should use UTC instants.

### 8.2 Query strategy

The overlay adapter should start with workflow-level visibility queries:

- execution count;
- status breakdown;
- running/closed distribution;
- average and percentile durations where derivable from closed executions;
- latest failures;
- links to Temporal UI.

Activity-level overlays require history indexing or sampling. They must be marked as sampled/derived unless the indexer has full coverage.

### 8.3 Read-only posture

The catalog must only use read-only Temporal operations:

- list/count workflow executions;
- get workflow histories if activity-level overlays are enabled;
- no starts;
- no signals;
- no updates;
- no terminations;
- no cancellations.

### 8.4 Overlay API example

```http
GET /api/runtime/temporal/workflows/RiskEnrichmentWorkflow/metrics?connection=local&namespace=default&from=2026-06-10T00:00:00Z&to=2026-06-10T12:00:00Z
```

Example response:

```json
{
  "workflowType": "RiskEnrichmentWorkflow",
  "namespace": "default",
  "from": "2026-06-10T00:00:00Z",
  "to": "2026-06-10T12:00:00Z",
  "executions": {
    "started": 241,
    "completed": 220,
    "failed": 8,
    "running": 13
  },
  "duration": {
    "averageMs": 18400,
    "p50Ms": 12100,
    "p95Ms": 92300
  },
  "quality": {
    "coverage": "workflow-visibility",
    "activityMetrics": "not-indexed"
  }
}
```

---

## 9. Server API surface

The standalone catalog should expose internal JSON APIs consumed by the UI.

```http
GET /api/sources
GET /api/sources/{sourceName}
POST /api/sources/{sourceName}/refresh
GET /api/catalog
GET /api/catalog/graph
GET /api/catalog/conflicts
GET /api/workflows
GET /api/workflows/{workflowId}
GET /api/runtime/temporal/metrics
GET /api/runtime/temporal/workflows/{workflowType}/metrics
GET /api/runtime/temporal/workflows/{workflowType}/executions
```

For local/dev mode, mutation endpoints such as `refresh` are acceptable. For enterprise mode, admin endpoints should be protected.

---

## 10. UI design

The standalone catalog UI should have these pages:

### 10.1 Overview

- source count;
- host count;
- workflow count;
- Nexus service/operation count;
- source health;
- stale/failed source warnings;
- runtime overlay connection state.

### 10.2 Source health

Shows each configured source:

- name;
- URI/file;
- last successful fetch;
- last attempted fetch;
- status;
- document version;
- error details;
- manual refresh action.

### 10.3 Catalog graph

The main graph combines all documents.

Graph views:

- hosts to workflows;
- workflows to activities/steps;
- workflow-to-workflow dependencies;
- Nexus services and operations;
- external systems;
- namespace/task queue topology.

### 10.4 Workflow detail

Per workflow:

- contract details;
- run input/output schema;
- signals;
- queries;
- updates;
- steps;
- dependencies;
- Temporal binding;
- runtime metrics over selected time range;
- links to Temporal UI if configured.

### 10.5 Runtime overlay controls

- connection selector;
- namespace selector;
- time range picker;
- filters based on search attributes where available;
- refresh button;
- overlay freshness timestamp.

---

## 11. Docker image

The catalog should be distributed as a standard Docker image.

Recommended image name:

```text
ghcr.io/workflowapi/workflowapi-catalog:latest
```

Recommended ports:

```text
8080 HTTP
```

Recommended health endpoints:

```http
GET /health/live
GET /health/ready
```

Recommended environment variables:

```text
WorkflowApiCatalog__Sources__0__Name=risk-worker
WorkflowApiCatalog__Sources__0__Uri=http://risk-worker/.well-known/workflow-api.json
WorkflowApiCatalog__Polling__IntervalSeconds=30
WorkflowApiCatalog__Temporal__Enabled=true
WorkflowApiCatalog__Temporal__Connections__0__Name=local
WorkflowApiCatalog__Temporal__Connections__0__TargetHost=temporal:7233
WorkflowApiCatalog__Temporal__Connections__0__Namespace=default
```

---

## 12. Kubernetes deployment

A minimal deployment should support:

- ConfigMap for source configuration;
- Secret for Temporal credentials/certificates;
- Service for UI/API;
- NetworkPolicy where appropriate;
- read-only Temporal credential permissions;
- optional persistent cache later, but memory cache by default.

Example ConfigMap fragment:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: workflowapi-catalog-config
data:
  appsettings.Production.json: |
    {
      "WorkflowApiCatalog": {
        "Sources": [
          {
            "Name": "risk-worker",
            "Uri": "http://risk-worker.risk.svc.cluster.local/.well-known/workflow-api.json"
          }
        ],
        "Temporal": {
          "Enabled": true,
          "Connections": [
            {
              "Name": "risk-prod",
              "TargetHost": "temporal-frontend.temporal.svc.cluster.local:7233",
              "Namespace": "B2B.RiskService"
            }
          ]
        }
      }
    }
```

---

## 13. Aspire hosting extension

The Aspire extension should make the standalone catalog easy to run locally.

### 13.1 Simple convention-based usage

```csharp
builder.AddWorkflowApiCatalog("workflow-catalog")
    .WithCatalogSource(riskWorker)
    .WithCatalogSource(offerWorker);
```

`WithCatalogSource(project)` resolves to:

```text
{project base endpoint}/.well-known/workflow-api.json
```

### 13.2 Explicit endpoint override

```csharp
builder.AddWorkflowApiCatalog("workflow-catalog")
    .WithCatalogSource(riskWorker, "/internal/workflow-api.json")
    .WithCatalogSource(offerWorker, "/.well-known/workflow-api.json");
```

### 13.3 File/directory sources

```csharp
builder.AddWorkflowApiCatalog("workflow-catalog")
    .WithCatalogSourceFile("../Risk.Worker/artifacts/workflow-api.json")
    .WithCatalogSourceDirectory("../artifacts/workflowapi");
```

### 13.4 Temporal overlay

```csharp
var temporal = builder.AddTemporalServer("temporal");

builder.AddWorkflowApiCatalog("workflow-catalog")
    .WithTemporal(temporal, namespaceName: "default")
    .WithCatalogSource(riskWorker)
    .WithCatalogSource(offerWorker)
    .WithRuntimeOverlay();
```

### 13.5 Generated container configuration

The Aspire extension should translate sources into environment variables or a generated mounted configuration file.

Example generated config:

```json
{
  "WorkflowApiCatalog": {
    "Sources": [
      {
        "Name": "risk-worker",
        "Uri": "http://risk-worker/.well-known/workflow-api.json"
      },
      {
        "Name": "offer-worker",
        "Uri": "http://offer-worker/.well-known/workflow-api.json"
      }
    ],
    "Temporal": {
      "Enabled": true,
      "Connections": [
        {
          "Name": "aspire-local",
          "TargetHost": "temporal:7233",
          "Namespace": "default",
          "WebUrl": "http://temporal-ui:8233"
        }
      ]
    }
  }
}
```

---

## 14. Security model

### 14.1 Spec source access

Sources may be internal-only. The catalog should support:

- static headers;
- bearer token from configuration;
- mTLS to source services later;
- service mesh identity later.

### 14.2 Temporal access

Runtime overlays require Temporal credentials. They should be scoped read-only wherever the deployment platform allows.

### 14.3 Data sensitivity

WorkflowAPI documents must not include secrets.

Runtime overlays must avoid leaking PII. Search attributes may contain business identifiers, so UI filters and exports should be treated as internal data.

### 14.4 UI protection

The standalone catalog may reveal internal topology. Production deployments should require authentication and authorization.

---

## 15. Implementation roadmap

### Phase 1 — static standalone catalog

- ASP.NET Core app.
- Configurable `WorkflowApiCatalog:Sources` list.
- HTTP/file fetcher.
- In-memory cache.
- Parse/validate WorkflowAPI documents.
- Merge documents.
- Source health page.
- Static graph UI.

### Phase 2 — Docker and Aspire

- Publish Docker image.
- Add health endpoints.
- Add `Aspire.Hosting.WorkflowApiCatalog`.
- Support convention-based `.WithCatalogSource(project)`.
- Support file/directory sources.

### Phase 3 — Temporal overlay

- Add Temporal connection configuration.
- Add workflow-level visibility metrics.
- Add user-selectable time range.
- Add Temporal UI deep links.
- Cache overlay responses by time range.

### Phase 4 — deeper runtime indexing

- Optional history sampling/indexing.
- Activity-level metrics.
- Retry/failure details.
- Drift detection between declared graph and observed histories.

### Phase 5 — enterprise mode

- Persistent store.
- Authentication/authorization.
- CI-published catalog sources.
- Git/OCI/package source adapters.
- Version diffing and governance workflows.

---

## 16. Agent implementation instructions

Specialist agents should implement in this order:

1. **Configuration agent**  
   Implement options model, validation, source resolution, and appsettings/env-var binding.

2. **Source fetcher agent**  
   Implement HTTP/file/directory fetchers, conditional requests, timeouts, and source health model.

3. **Parser/validator agent**  
   Parse WorkflowAPI documents and surface validation errors without crashing the catalog.

4. **Merge/graph agent**  
   Build merged catalog model and graph edges from many documents.

5. **UI agent**  
   Implement source health, catalog overview, and graph views. Use the same graph adapter as the per-service reference UI where possible.

6. **Temporal overlay agent**  
   Implement read-only Temporal metrics, cache keys, and time-range API.

7. **Aspire agent**  
   Implement `Aspire.Hosting.WorkflowApiCatalog` using convention-first source wiring.

8. **Container/Kubernetes agent**  
   Publish Dockerfile, health endpoints, Helm/example manifests, ConfigMap/Secret documentation.

---

## 17. Acceptance criteria

The standalone catalog is acceptable when:

- it runs with no workflow implementations in-process;
- it can load two or more WorkflowAPI documents from configured URLs;
- it keeps serving the last successful graph if one source fails;
- it displays source health clearly;
- it builds a combined graph from multiple specs;
- it works from a Docker image with environment-variable configuration;
- it can be added to Aspire via a hosting extension;
- Temporal runtime overlays are optional and disabled by default;
- enabling Temporal overlay displays workflow counts by selected time range;
- all runtime overlay operations are read-only.

---

## 18. Relationship to other documents

This document complements:

- `01-workflowapi-core-spec.md` — document model;
- `02-dotnet-implementation-design.md` — per-service .NET generation;
- `03-temporal-dotnet-binding.md` — Temporal binding and runtime overlay;
- `04-reference-ui-and-catalog.md` — UI concepts and catalog overview;
- `05-agent-implementation-instructions.md` — implementation sequencing;
- `06-examples.md` — concrete examples.

This document should be treated as the primary source for the standalone catalog server/container/Aspire-extension design.

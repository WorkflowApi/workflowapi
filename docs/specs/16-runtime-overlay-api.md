> **v1 tightened scope note**  
> This file is retained as a post-v1 design note unless a section explicitly says otherwise. WorkflowAPI v1 is limited to the compact durable execution workflow API specification, .NET generation from workflow attributes or fluent definitions, Temporal static binding metadata, and static workflow display. Runtime overlays, catalogue collation, source polling, workflow control-plane actions, BPMN-style modelling and runtime observability are outside v1 scope.

# 16. Runtime Overlay API

## Purpose

The generic WorkflowAPI UI renders declared workflow contracts. Runtime overlay providers decorate those contracts with operational facts such as counts, failures, durations, and deep links.

This document defines the proposed backend API and DTOs for runtime overlays.

## Design principles

- Runtime overlay data is optional.
- Runtime plugins decorate the spec graph; they do not own the declared topology.
- Backend talks to runtime systems; browser does not connect directly to Temporal.
- Overlay APIs must support selected time ranges.
- Runtime data should be cacheable and source-attributed.
- Runtime metrics must be read-only in v1.

## Core concepts

```text
WorkflowAPI document
  -> declared graph
Runtime overlay provider
  -> metrics by workflow/step/edge/bridge id
UI
  -> graph + badges + detail panels
```

## API endpoints

### Capabilities

```http
GET /api/runtime/capabilities
```

Response:

```json
{
  "providers": [
    {
      "id": "temporal",
      "displayName": "Temporal",
      "enabled": true,
      "supportsTimeRanges": true,
      "supportsDeepLinks": true,
      "supportsStepMetrics": true,
      "supportsBridgeMetrics": true
    }
  ]
}
```

### Workflow metrics

```http
GET /api/runtime/workflows/{workflowId}/metrics?from=2026-06-01T00:00:00Z&to=2026-06-10T00:00:00Z
```

Response:

```json
{
  "workflowId": "order-fulfilment",
  "provider": "temporal",
  "timeRange": {
    "from": "2026-06-01T00:00:00Z",
    "to": "2026-06-10T00:00:00Z"
  },
  "metrics": {
    "started": 2431,
    "running": 37,
    "completed": 2353,
    "failed": 12,
    "timedOut": 3,
    "terminated": 0,
    "canceled": 1,
    "avgDurationMs": 84120,
    "p50DurationMs": 51200,
    "p95DurationMs": 134000,
    "p99DurationMs": 281000,
    "slaBreached": 9
  },
  "freshness": {
    "calculatedAt": "2026-06-10T05:00:00Z",
    "ttlSeconds": 60,
    "source": "temporal-visibility"
  }
}
```

### Step metrics

```http
GET /api/runtime/workflows/{workflowId}/steps/{stepId}/metrics?from=...&to=...
```

Response:

```json
{
  "workflowId": "order-fulfilment",
  "stepId": "take-payment",
  "provider": "temporal",
  "metrics": {
    "scheduled": 2398,
    "started": 2398,
    "completed": 2353,
    "failed": 12,
    "timedOut": 0,
    "retried": 73,
    "avgDurationMs": 1840,
    "p95DurationMs": 8400,
    "topErrorTypes": [
      { "type": "DnbRateLimitedException", "count": 7 },
      { "type": "TimeoutException", "count": 2 }
    ]
  }
}
```

### Edge metrics

```http
GET /api/runtime/workflows/{workflowId}/edges/{edgeId}/metrics?from=...&to=...
```

Response:

```json
{
  "workflowId": "order-fulfilment",
  "edgeId": "check-and-block-inventory->take-payment",
  "provider": "temporal",
  "metrics": {
    "observedCount": 2103,
    "avgTransitionMs": 260,
    "dropOffCount": 12
  }
}
```

### Bridge metrics

```http
GET /api/runtime/bridges/{bridgeId}/metrics?from=...&to=...
```

Response:

```json
{
  "bridgeId": "riskservice-calculaterisk",
  "provider": "temporal",
  "metrics": {
    "calls": 18442,
    "completed": 18290,
    "failed": 92,
    "avgDurationMs": 1700,
    "p95DurationMs": 4200
  }
}
```

### Failed execution samples

```http
GET /api/runtime/workflows/{workflowId}/executions?status=failed&from=...&to=...&limit=20
```

Response:

```json
{
  "items": [
    {
      "workflowId": "risk-123",
      "runId": "...",
      "status": "failed",
      "startedAt": "2026-06-09T10:12:00Z",
      "closedAt": "2026-06-09T10:13:22Z",
      "errorType": "DnbRateLimitedException",
      "deepLinks": [
        {
          "label": "Open in Temporal UI",
          "url": "https://temporal.example/namespaces/Commerce.OrderService/workflows/risk-123/..."
        }
      ]
    }
  ]
}
```

## Time range model

The UI should support:

```text
Last 15 minutes
Last 1 hour
Last 24 hours
Last 7 days
Last 30 days
Custom UTC range
```

The backend should enforce maximum lookback limits per provider.

## Temporal overlay implementation notes

Temporal runtime overlay can use:

- Visibility APIs for workflow counts/status/time filtering;
- CountWorkflowExecutions where appropriate;
- workflow histories for step-level extraction;
- a local cache/indexer for expensive step metrics;
- optional metrics backend if the deployment exports Temporal metrics to Prometheus/DataDog/etc.

The plugin must be honest about metric provenance:

```text
visibility-count
history-sampled
history-indexed
metrics-backend
cache
```

## Caching

Runtime overlay responses should include freshness metadata.

Caching strategies:

- in-memory cache for standalone/local catalog;
- persistent cache for central catalog;
- stale-on-error behaviour for temporary Temporal outages;
- per-time-range cache keys.

Cache key shape:

```text
providerId + sourceId + workflowId/stepId/bridgeId + timeRange + filtersHash
```

## Error model

Runtime overlay errors should not break static spec rendering.

Example:

```json
{
  "error": {
    "code": "WFAPI_RUNTIME_TEMPORAL_UNAVAILABLE",
    "message": "Temporal runtime overlay is unavailable. Static WorkflowAPI graph is still available.",
    "retryAfterSeconds": 60
  }
}
```

## Agent acceptance criteria

An implementation agent must deliver:

- runtime overlay DTOs;
- provider abstraction;
- no-op/static provider;
- Temporal provider skeleton;
- API endpoints with mocked data;
- UI consumes mocked overlay data;
- cache/freshness model;
- tests for disabled/unavailable overlays.

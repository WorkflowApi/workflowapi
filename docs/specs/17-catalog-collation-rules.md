# 17. Catalog Collation and Graph Merge Rules

## Purpose

The standalone catalog loads many WorkflowAPI documents and builds a single graph. This requires deterministic identity, merge, conflict, and overlay rules.

## Source model

A catalog source is configured explicitly or discovered by convention.

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
    ]
  }
}
```

If only a base URL is provided, the catalog should try:

```text
/.well-known/workflow-api.json
/workflow-api/v1.json
```

## Source cache model

On startup, the catalog should:

1. load all configured sources;
2. validate syntax/schema;
3. run semantic validation;
4. store source documents in memory cache;
5. build the merged graph;
6. expose partial results if some sources fail.

For later versions, add persistent storage.

## Source states

| State | Meaning |
|---|---|
| `healthy` | fetched, validated, included |
| `warning` | included but has warnings |
| `invalid` | fetched but not included due to errors |
| `unreachable` | could not fetch |
| `stale` | last known good document retained after refresh failure |
| `disabled` | source configured but disabled |

## Canonical identities

Stable identities are required.

Recommended identity format:

```text
host:<environment>/<hostName>
workflow:<environment>/<hostName>/<workflowId>
step:<environment>/<hostName>/<workflowId>/<stepId>
bridge:<environment>/<bridgeId>
schema:<sourceName>/<schemaId>
```

If `environment` is omitted, use `default`.

## Merge rules

### Hosts

Same `host.name` and environment merge into one host if document source id matches or source declares same canonical host id.

Different source documents claiming the same host id should produce a warning or error depending on strictness.

### Workflows

Workflow identity is based on:

```text
environment + host identity + workflow id
```

A workflow with the same id in different hosts is not automatically a conflict. It may represent:

- same logical workflow deployed by different host versions;
- duplicate/conflicting definitions;
- environment-specific copies.

The catalog should group by logical id but retain host-specific instances.

### Steps

Steps are scoped to workflows.

Duplicate step id within one workflow is an error.

### Bridges

Bridges are top-level cross-boundary constructs. A bridge should have:

- stable bridge id;
- source workflow/step reference;
- target service/operation/workflow reference;
- optional binding-specific metadata.

Bridge conflicts occur when two documents define the same bridge id with incompatible targets.

### Schemas

Schemas should be content-addressed or source-scoped.

If two schemas have the same logical name but different hash:

- same source and version: error;
- different source: warning unless explicitly linked as same canonical schema.

## Overlay algorithm

The merged graph is built in this order:

1. hosts;
2. workflows;
3. operations;
4. steps;
5. internal edges;
6. nested workflows/subflows;
7. bridges;
8. dependencies/external systems;
9. runtime overlay placeholders;
10. runtime metrics.

## Bridge resolution

A generic bridge may resolve to:

- another WorkflowAPI bridge operation;
- a workflow run operation;
- a Nexus service operation;
- an external system placeholder;
- unresolved dependency.

Unresolved bridges should remain visible, not disappear. This is valuable because it exposes integration gaps.

UI state:

```text
resolved bridge   -> solid edge
unresolved bridge -> dashed warning edge
ambiguous bridge  -> dashed warning edge with multiple candidates
```

## Environment partitioning

The catalog must avoid merging local/test/prod graphs accidentally.

Sources should specify environment:

```json
{
  "Name": "risk-worker-prod",
  "Environment": "prod",
  "Uri": "https://risk-worker/.well-known/workflow-api.json"
}
```

The UI should allow environment filtering.

## Conflict diagnostics

Examples:

```text
WFAPI_CATALOG_DUPLICATE_HOST_ID
WFAPI_CATALOG_SCHEMA_HASH_MISMATCH
WFAPI_CATALOG_UNRESOLVED_BRIDGE_TARGET
WFAPI_CATALOG_AMBIGUOUS_BRIDGE_TARGET
WFAPI_CATALOG_SOURCE_STALE
WFAPI_CATALOG_SOURCE_INVALID
```

## Partial failure handling

The catalog should show:

- last successful fetch time;
- last error;
- whether stale data is being used;
- number of workflows contributed by each source;
- validation diagnostics by source.

Partial failure must not blank the entire catalog unless all sources are unavailable and no cache exists.

## Agent acceptance criteria

An implementation agent must deliver:

- source fetcher abstraction;
- memory source cache;
- merge graph builder;
- conflict diagnostics;
- stale source handling;
- environment filtering;
- tests for duplicate ids, unresolved bridges, stale sources, and schema mismatch.

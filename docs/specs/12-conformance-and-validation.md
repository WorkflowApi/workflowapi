> **v1 tightened scope note**  
> This file is retained as a post-v1 design note unless a section explicitly says otherwise. WorkflowAPI v1 is limited to the compact durable execution workflow API specification, .NET generation from workflow attributes or fluent definitions, Temporal static binding metadata, and static workflow display. Runtime overlays, catalogue collation, source polling, workflow control-plane actions, BPMN-style modelling and runtime observability are outside v1 scope.

# 12. WorkflowAPI Conformance and Validation

## Purpose

WorkflowAPI must be more than a documentation format. It needs a repeatable conformance and validation model so generators, UIs, catalogs, runtime overlay plugins, and CI pipelines can agree on what is valid and what should be rejected.

This document defines the proposed validation layers, diagnostic severities, conformance profiles, canonical examples, and test harness requirements for WorkflowAPI v0.1.

## Design goals

- Make invalid WorkflowAPI documents fail early.
- Keep the core DSL generic and runtime-neutral.
- Allow runtime bindings, such as Temporal, to define additional validation rules.
- Support warning-level adoption so teams can start with partial metadata.
- Provide deterministic validation results suitable for CI and agentic implementation loops.
- Provide machine-readable diagnostics for IDEs, PR bots, catalog import jobs, and CLI output.

## Validation layers

WorkflowAPI validation should run in layers. Each layer should be independently testable.

```text
JSON/YAML parse
  -> JSON Schema validation
  -> WorkflowAPI semantic validation
  -> binding-specific validation
  -> catalog collation validation
  -> runtime overlay validation, when runtime data is enabled
```

### 1. Syntax validation

Checks whether the document can be parsed as JSON or YAML.

Examples:

- invalid JSON syntax;
- invalid YAML indentation;
- unsupported document encoding;
- duplicate keys where parser supports duplicate-key detection.

Failure at this layer is always an error.

### 2. Structural schema validation

Checks against `schemas/workflowapi.schema.json`.

Examples:

- missing `workflowApi` version;
- invalid `info` object;
- invalid `workflows` shape;
- invalid enum value for `step.kind`;
- `edges` not being an array;
- `bindings` section using a non-object value.

This layer should use JSON Schema Draft 2020-12 or later unless a concrete implementation has a strong reason to use a different supported draft.

### 3. Core semantic validation

Checks rules that JSON Schema cannot easily express.

Examples:

- an edge references an unknown step id;
- a nested subflow references an unknown workflow id;
- two workflows in the same document use the same `id`;
- a bridge declares an operation but omits the bridge target;
- a workflow has no `run` operation and no explicit `abstract: true` marker;
- a step declares both `activityRef` and `workflowRef` where only one implementation reference is allowed;
- an example references an unknown schema;
- a deprecated workflow lacks deprecation guidance.

### 4. Binding-specific validation

Runtime bindings add their own rules.

For Temporal:

- `bindings.temporal.workflowType` should be present for concrete workflow implementations.
- `bindings.temporal.namespace` should be present for runtime-capable host documents.
- `bindings.temporal.taskQueue` or `bindings.temporal.taskQueues` should be present when the document describes a Temporal workflow host.
- Temporal search attributes must use supported Temporal-compatible types.
- Temporal Nexus bridge bindings must declare service name, operation name, and endpoint/target routing metadata when the catalog is expected to resolve cross-namespace bridges.
- Runtime credential material must not be present in the document.

### 5. Catalog collation validation

Applied when many WorkflowAPI documents are loaded into a catalog.

Examples:

- duplicate host identity;
- conflicting workflow ids across sources;
- identical workflow id but different schema hash;
- bridge target cannot be resolved;
- same Nexus operation exposed by multiple hosts without environment scoping;
- stale source document retained after fetch failure;
- source document has lower WorkflowAPI spec version than the catalog supports.

### 6. Runtime overlay validation

Applied when runtime data is enabled.

Examples:

- a runtime overlay references a workflow id not present in the active catalog graph;
- a Temporal metric provider has namespace credentials but no matching Temporal binding;
- a step metric maps to multiple declared steps without a disambiguation rule;
- runtime time range is invalid or exceeds configured maximum lookback;
- runtime source is stale beyond allowed TTL.

## Diagnostic severity model

Diagnostics should use four severities.

| Severity | Meaning | CI default |
|---|---|---|
| `error` | Document is invalid or cannot be safely processed. | fail |
| `warning` | Document is valid but incomplete, ambiguous, or likely wrong. | pass with warning |
| `info` | Useful advisory message. | pass |
| `hint` | Style or quality improvement. | pass |

Each diagnostic should include:

```json
{
  "code": "WFAPI_CORE_EDGE_UNKNOWN_STEP",
  "severity": "error",
  "message": "Edge 'register-shipping->publish-event' references unknown target step 'publish-event'.",
  "path": "$.workflows.order-fulfilment.edges[2].target",
  "source": "commerce-order-worker/.well-known/workflow-api.json"
}
```

## Diagnostic code namespaces

Use stable diagnostic code prefixes:

```text
WFAPI_PARSE_*      Syntax and parser errors
WFAPI_SCHEMA_*     JSON Schema validation errors
WFAPI_CORE_*       Runtime-neutral semantic validation
WFAPI_TEMPORAL_*   Temporal binding validation
WFAPI_CATALOG_*    Multi-source catalog collation validation
WFAPI_RUNTIME_*    Runtime overlay validation
WFAPI_STYLE_*      Naming/style/quality hints
```

Examples:

```text
WFAPI_CORE_DUPLICATE_WORKFLOW_ID
WFAPI_CORE_UNKNOWN_STEP_REFERENCE
WFAPI_TEMPORAL_MISSING_TASK_QUEUE
WFAPI_TEMPORAL_UNSUPPORTED_SEARCH_ATTRIBUTE_TYPE
WFAPI_CATALOG_UNRESOLVED_BRIDGE_TARGET
WFAPI_RUNTIME_UNMAPPED_ACTIVITY_METRIC
WFAPI_STYLE_MISSING_SUMMARY
```

## Conformance profiles

WorkflowAPI should define conformance profiles. This lets a simple generator pass a smaller profile while the central catalog demands more.

### `core-document`

A document conforms to the generic WorkflowAPI syntax and semantics.

Required:

- valid `workflowApi` version;
- valid `info`;
- at least one workflow, subflow, or bridge;
- valid schemas/components if referenced;
- all internal references resolve.

### `temporal-binding`

A document conforms to the Temporal binding profile.

Required:

- all Temporal binding fields are structurally valid;
- workflow type names are present where needed;
- task queue and namespace are present for host documents;
- Temporal Nexus bridge bindings are valid when present;
- unsupported Temporal-specific fields produce errors.

### `reference-ui`

A document can be rendered by the single-service WorkflowAPI reference UI.

Required:

- workflow ids and display titles;
- operation lists;
- topology can be generated from steps/edges or clear fallback rules;
- schemas can be displayed or linked.

### `catalog-source`

A document can be ingested by the standalone catalog.

Required:

- host identity;
- stable ids;
- environment/namespace scoping where runtime bindings are present;
- bridge identities are stable;
- document source metadata is present or supplied externally.

### `runtime-overlay`

A document can be decorated with runtime metrics.

Required:

- runtime binding sufficient for overlay provider;
- stable mapping from workflow/step ids to runtime names;
- overlay provider has non-secret configuration external to the spec.

## Validation modes

The validator should support modes.

```bash
workflowapi validate workflow-api.json --profile core-document
workflowapi validate workflow-api.json --profile temporal-binding
workflowapi validate workflow-api.json --profile catalog-source --strict
```

Modes:

| Mode | Behaviour |
|---|---|
| `default` | errors fail, warnings pass |
| `strict` | errors and warnings fail |
| `advisory` | never exits non-zero unless parsing fails |
| `catalog` | validates source documents plus collation rules |

## Style-quality validation

Quality hints should help teams improve catalog readability without blocking adoption.

Examples:

- workflow has no `summary`;
- activity step has a technical name only, such as `ExecuteActivity`;
- too many ungrouped steps;
- bridge lacks business description;
- schema names do not end in `Request`, `Result`, `Signal`, or similar convention;
- Temporal Search Attribute likely contains PII.

## Canonical example set

The repository should include canonical documents used as golden test fixtures.

```text
examples/specs/minimal-valid.workflowapi.json
examples/specs/temporal-basic.workflowapi.json
examples/specs/temporal-nexus-bridge.workflowapi.json
examples/specs/nested-workflow.workflowapi.json
examples/specs/catalog-collation.workflowapi.json
examples/specs/invalid/unknown-step-reference.workflowapi.json
examples/specs/invalid/duplicate-workflow-id.workflowapi.json
examples/specs/invalid/temporal-missing-task-queue.workflowapi.json
```

Each invalid example should include an expected diagnostics file:

```text
examples/specs/invalid/unknown-step-reference.diagnostics.json
```

## Test harness requirements

The conformance test project should include:

- JSON Schema validation tests;
- semantic validation tests;
- Temporal binding validation tests;
- catalog collation validation tests;
- snapshot tests for generated documents;
- CLI exit-code tests;
- source-generator/export tests;
- UI smoke tests against canonical documents.

## Agent acceptance criteria

An implementation agent completing the conformance slice must provide:

- a validator library;
- a CLI `validate` command;
- JSON diagnostics output;
- human-readable diagnostics output;
- at least ten positive fixtures;
- at least ten negative fixtures;
- CI workflow running validation on all examples;
- documentation of each diagnostic code.

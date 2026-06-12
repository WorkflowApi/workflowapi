> **v1 tightened scope note**  
> This file is retained as a post-v1 design note unless a section explicitly says otherwise. WorkflowAPI v1 is limited to the compact durable execution workflow API specification, .NET generation from workflow attributes or fluent definitions, Temporal static binding metadata, and static workflow display. Runtime overlays, catalogue collation, source polling, workflow control-plane actions, BPMN-style modelling and runtime observability are outside v1 scope.

# 13. WorkflowAPI Versioning and Compatibility

## Purpose

WorkflowAPI must support safe change management. Enterprises will use generated specs to drive catalogs, reviewers, runtime overlays, client code, and governance. The specification therefore needs explicit compatibility rules.

This document defines the initial compatibility model for WorkflowAPI v0.1.

## Version concepts

WorkflowAPI has several different versions. They must not be conflated.

| Version | Meaning |
|---|---|
| `workflowApi` | Version of the WorkflowAPI specification format. |
| `info.version` | Version of this document as published by a host/project. |
| `workflow.version` | Version of a logical workflow contract. |
| `binding.version` | Version of runtime binding metadata, if needed. |
| `implementation.version` | Application/package/container version. |
| `schema.version` | Version of a payload schema. |

A WorkflowAPI document may describe several workflows with different contract versions.

## Recommended versioning baseline

Use Semantic Versioning for document and workflow contract versions:

```text
MAJOR.MINOR.PATCH
```

- MAJOR: incompatible contract change.
- MINOR: backward-compatible feature addition.
- PATCH: documentation, metadata, or non-contract correction.

## Contract vs runtime binding changes

WorkflowAPI must distinguish contract changes from deployment/runtime changes.

Contract examples:

- run input schema;
- run output schema;
- signal names and payloads;
- query names and outputs;
- update names and payloads;
- bridge operation names and schemas;
- externally visible business semantics.

Runtime binding examples:

- Temporal namespace;
- Temporal task queue;
- Temporal Web URL;
- worker deployment name;
- runtime metric source;
- Kubernetes service address.

A runtime binding change can be operationally risky, but it is not always a WorkflowAPI contract-breaking change.

## Compatibility rules

### Workflow operations

| Change | Default compatibility |
|---|---|
| Add new workflow | non-breaking |
| Remove workflow | breaking |
| Rename workflow id | breaking |
| Change display title only | non-breaking |
| Change summary/description only | non-breaking |
| Mark workflow deprecated | non-breaking |
| Remove deprecated workflow | breaking unless past removal policy |

### Run operation

| Change | Default compatibility |
|---|---|
| Add optional input property | non-breaking |
| Add required input property | breaking |
| Remove input property | breaking unless previously deprecated and optional |
| Change input property type | breaking |
| Add output property | usually non-breaking |
| Remove output property | breaking |
| Change result type | breaking |

### Signals

| Change | Default compatibility |
|---|---|
| Add signal | non-breaking |
| Remove signal | breaking |
| Rename signal | breaking |
| Add optional signal payload property | non-breaking |
| Add required signal payload property | breaking |
| Change unfinished policy metadata | binding/runtime change; review required |

### Queries

| Change | Default compatibility |
|---|---|
| Add query | non-breaking |
| Remove query | breaking |
| Rename query | breaking |
| Add output property | usually non-breaking |
| Remove output property | breaking |
| Change query from read-only to mutating semantics | invalid design; queries must remain read-only |

### Updates

| Change | Default compatibility |
|---|---|
| Add update | non-breaking |
| Remove update | breaking |
| Rename update | breaking |
| Change update result schema | breaking |
| Change validation/acceptance semantics | potentially breaking; document explicitly |

### Steps and topology

Internal steps can be tricky. WorkflowAPI should distinguish public contract topology from internal implementation topology.

| Change | Compatibility |
|---|---|
| Add internal hidden step | non-breaking |
| Add visible business step | usually minor |
| Remove visible business step | potentially breaking for catalog consumers |
| Change edge labels | non-breaking unless used as contract semantics |
| Change bridge call target | potentially breaking |
| Convert inline activity to child workflow with same business semantics | usually non-breaking but should be highlighted |

### Bridges and Nexus-style namespace bridging

| Change | Compatibility |
|---|---|
| Add bridge operation | non-breaking |
| Remove bridge operation | breaking if externally referenced |
| Change bridge input/output schema | breaking according to schema rules |
| Change bridge target namespace/task queue only | runtime binding change; review required |
| Change bridge service or operation name | breaking |
| Add new bridge binding for another runtime | non-breaking |

## Diff categories

The CLI should classify diffs into categories:

```text
contract-breaking
contract-non-breaking
runtime-binding-change
documentation-change
metadata-change
quality-change
unknown-risk
```

Example CLI:

```bash
workflowapi diff old.workflowapi.json new.workflowapi.json --format markdown
workflowapi diff old.workflowapi.json new.workflowapi.json --fail-on contract-breaking
```

## Deprecation model

Any deprecable item should support:

```yaml
deprecated: true
deprecation:
  since: "1.4.0"
  removalAfter: "2027-01-01"
  reason: "Use RecalculateRiskV2 instead."
  replacement: "#/workflows/order-fulfilment/updates/RecalculateRiskV2"
```

Deprecation should not immediately make the document invalid. A strict catalog may warn when `removalAfter` has passed.

## Schema compatibility

WorkflowAPI should reuse JSON Schema compatibility practices where possible.

General guidance:

- adding optional object properties is usually safe;
- adding required object properties is breaking;
- narrowing enum values is breaking;
- widening enum values can be breaking for strict consumers;
- changing numeric/string/object shapes is breaking;
- removing schema definitions is breaking if referenced.

## Version negotiation and multi-version docs

A host may expose several documents:

```text
/workflow-api/v1.json
/workflow-api/internal.json
/.well-known/workflow-api.json
```

The default `.well-known` document should point to the current recommended document or contain an aggregate document with links.

## Agent acceptance criteria

An implementation agent completing this area must provide:

- diff engine library;
- CLI `diff` command;
- machine-readable and markdown diff output;
- breaking/non-breaking classification tests;
- examples for run, signal, query, update, bridge, and binding changes;
- catalog import policy for breaking changes.

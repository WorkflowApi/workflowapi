# 18. WorkflowAPI v1 Scope and Non-Goals

## Purpose

WorkflowAPI is broad. Without an explicit scope, an agentic implementation team may drift into building a BPMN designer, a Temporal UI replacement, or a workflow control plane.

This document defines the v1 scope and non-goals.

## v1 product thesis

WorkflowAPI v1 provides:

```text
A specification for durable workflow APIs
+ .NET generation/runtime endpoint tooling
+ Temporal .NET binding
+ single-service reference UI
+ standalone multi-service catalog
+ optional read-only Temporal runtime overlays
```

## In scope for v1

### Core spec

- WorkflowAPI document model.
- Workflows, run operations, signals, queries, updates.
- Activities/steps and declared topology.
- Nested workflows/subflows.
- Generic bridges.
- Temporal Nexus-style bridge binding.
- Components/schemas/examples/policies.
- Generic runtime binding extension model.

### .NET implementation

- `WorkflowApi.Abstractions`.
- `WorkflowApi.AspNetCore`.
- `WorkflowApi.Temporal`.
- Scrutor-backed discovery.
- XML documentation support.
- Transformers.
- `app.MapWorkflowApi()`.
- `app.MapWorkflowApiReference()`.

### Temporal implementation

- Inference from Temporal .NET attributes where possible.
- Temporal namespace/task queue binding from config.
- Workflow/activity name mapping.
- Signals/queries/updates mapping.
- Temporal Nexus bridge metadata.

### UI

- Generic WorkflowAPI reference UI.
- Graph rendering from declared WorkflowAPI topology.
- Optional runtime overlay plugin architecture.
- Temporal overlay plugin v1.
- EventCatalog visualiser adapter where useful.

### Standalone catalog

- HealthChecks.UI-style configured sources.
- Memory cache on startup.
- Periodic refresh.
- Partial failure handling.
- Graph collation.
- Docker image.
- Aspire hosting extension.

### Tooling

- validation;
- export;
- diff;
- initial MSBuild hook;
- JSON Schema artifacts;
- examples and conformance fixtures.

## Out of scope for v1

### Temporal control plane

Do not build UI actions for:

- start workflow;
- signal workflow;
- update workflow;
- cancel/terminate/reset workflow;
- modify schedules;
- modify Nexus endpoints;
- re-drive failed executions.

WorkflowAPI v1 is read-only.

### Workflow designer

Do not build:

- low-code workflow designer;
- drag-and-drop BPMN editor;
- code generation from diagrams;
- workflow execution engine.

WorkflowAPI documents describe workflow APIs; they do not author workflow code in v1.

### Full process mining

Do not promise complete discovery of intended topology from Temporal histories.

Runtime history indexing can provide observed overlays, but the declared graph comes from WorkflowAPI.

### Full multi-runtime parity

Temporal .NET is the first implementation. Other runtimes may influence generic design but are not v1 implementation targets.

### Enterprise identity platform

OIDC/RBAC should be designed, but do not build a full enterprise identity administration product.

### Full persistent analytics platform

In-memory cache is acceptable for the first standalone catalog. Persistent runtime analytics can come later.

## v1 milestone proposal

### Milestone 1: spec and validator

- core document model;
- JSON Schema;
- validator;
- examples.

### Milestone 2: .NET generation

- attributes;
- Scrutor discovery;
- XML comments;
- `MapWorkflowApi`.

### Milestone 3: Temporal binding

- Temporal attribute inference;
- namespace/task queue config;
- sample Temporal project.

### Milestone 4: reference UI

- single-service UI;
- graph rendering;
- schema/reference views.

### Milestone 5: standalone catalog

- configured sources;
- memory cache;
- collation;
- Docker image;
- Aspire hosting extension.

### Milestone 6: runtime overlay

- Temporal read-only metrics;
- time range selector;
- node/edge badges;
- Temporal Web deep links.

## Definition of done for v1 preview

- One sample Temporal .NET app can expose `.well-known/workflow-api.json`.
- The single-service UI renders its workflows.
- The standalone catalog loads at least three service specs.
- The catalog renders cross-workflow/bridge relationships.
- The Temporal overlay shows workflow-level counts for a selected time range.
- CLI validates and diffs specs.
- CI runs conformance examples.
- Docker image runs locally and in Aspire.

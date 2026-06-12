# 18. WorkflowAPI v1 Scope and Non-Goals

Status: v1 tightened draft

WorkflowAPI v1 defines a compact specification for durable execution workflow APIs and supports static workflow display generated from workflow-related attributes or fluent definitions.

## In scope for v1

- WorkflowAPI document model.
- Workflow host metadata.
- Workflow identity and metadata.
- Workflow durable entry point, modelled as `run` and equivalent to Temporal's `[WorkflowRun]` / `RunAsync` principle.
- Signals.
- Queries.
- Updates.
- Activities.
- Child workflow references.
- Optional bridge references.
- Optional declared display topology.
- Runtime bindings, with Temporal as the first binding.
- .NET attributes and fluent definitions.
- Static reference UI.

## Out of scope for v1

WorkflowAPI v1 does not model external callers that start workflows, including HTTP endpoints, message handlers, schedulers, cron triggers, external actions, CLI commands, any kind of events, broker routes or application services.

WorkflowAPI v1 does not model BPMN-style process semantics, runtime metrics, runtime overlays, execution history parsing, observed topology, catalogue collation, source polling, workflow control-plane actions, workflow editing or code generation from diagrams.

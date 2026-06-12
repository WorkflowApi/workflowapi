# WorkflowAPI Specification Pack

Status: v1 tightened draft

WorkflowAPI is a compact specification and tooling model for durable execution workflow APIs.

```text
OpenAPI      -> HTTP request/response APIs
AsyncAPI     -> event/message APIs
WorkflowAPI  -> durable execution workflow APIs
```

## Tightened v1 posture

WorkflowAPI v1 focuses on a generic durable workflow document model, workflow metadata, one durable workflow entry point modelled as `run`, signals, queries, updates, activities, child workflow references, optional bridge references, optional declared display topology, schema references, runtime bindings with Temporal as the first binding, .NET generation from workflow attributes or fluent definitions, and static workflow display/reference UI.

WorkflowAPI v1 does not model the external caller that starts a workflow, HTTP endpoints, message subscriptions, schedulers, BPMN-style process modelling, runtime histories, runtime metrics, catalogue collation, workflow control-plane actions, workflow editing or code generation from diagrams.

The examples use a generic e-commerce order fulfilment workflow: validate order, check and block inventory, take payment, register shipping and send confirmation email.

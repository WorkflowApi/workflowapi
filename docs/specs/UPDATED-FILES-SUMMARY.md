# Updated Files Summary

This package applies the tightened WorkflowAPI v1 direction and replaces the previous B2B risk example with a generic e-commerce order fulfilment example.

## Core changes

- Tightened v1 around a compact durable execution workflow API specification.
- Clarified that `run` means the workflow's durable entry point, comparable to Temporal `[WorkflowRun]` / `RunAsync`.
- Explicitly excluded modelling external workflow callers such as HTTP endpoints, message handlers, schedulers, CRM actions, storefront events and portal routes.
- Kept durable execution units named `activities`.
- Removed BPMN-light ambitions from v1.
- Reduced topology to static display-only steps and edges.
- Deferred catalogue, runtime overlay, source polling, runtime metrics and control-plane actions beyond v1.
- Replaced the example domain with e-commerce order fulfilment: validate order, check and block inventory, take payment, register shipping and send confirmation email.

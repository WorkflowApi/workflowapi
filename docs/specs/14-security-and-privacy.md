> **v1 tightened scope note**  
> This file is retained as a post-v1 design note unless a section explicitly says otherwise. WorkflowAPI v1 is limited to the compact durable execution workflow API specification, .NET generation from workflow attributes or fluent definitions, Temporal static binding metadata, and static workflow display. Runtime overlays, catalogue collation, source polling, workflow control-plane actions, BPMN-style modelling and runtime observability are outside v1 scope.

# 14. WorkflowAPI Security and Privacy

## Purpose

WorkflowAPI documents describe business processes, runtime bindings, dependencies, schemas, and optionally runtime metrics. This can leak sensitive information if not handled deliberately.

This document defines the security and privacy posture for WorkflowAPI v0.1.

## Security principles

- WorkflowAPI documents must never contain secrets.
- Runtime credentials must be external configuration, not spec content.
- Generated specs should support public/internal/debug views.
- Runtime overlays must be read-only by default.
- The reference UI and catalog must not become workflow control planes in v1.
- PII and sensitive business identifiers must be redacted or access-controlled.
- Catalog access should be authenticated in shared environments.

## Information classes

| Information | Sensitivity | Guidance |
|---|---|---|
| Workflow names | internal | okay internally; review before public release |
| Business step names | internal | may expose process details |
| DTO schema property names | internal/sensitive | review for PII and trade secrets |
| Temporal namespace | internal | hide from public docs |
| Temporal task queue | internal | hide from public docs unless needed |
| Nexus endpoint names | internal | hide or scope by environment |
| Search attributes | often sensitive | avoid PII; classify explicitly |
| Runtime counts | internal | can reveal volumes/business activity |
| Failed execution examples | sensitive | require RBAC/redaction |
| Workflow ids/run ids | sensitive | avoid in broad UI by default |
| Credentials/API keys/certificates | secret | forbidden in specs |

## Public, internal, and debug documents

WorkflowAPI generation should support document views.

### Public view

Safe for broad documentation.

Should omit:

- runtime namespace/task queue;
- internal activities;
- internal system names;
- search attributes that may expose business identifiers;
- runtime metric overlays;
- Temporal Web deep links.

### Internal view

For authenticated engineering/product users.

May include:

- business topology;
- owner/team metadata;
- runtime bindings without secrets;
- internal steps;
- non-sensitive runtime health.

### Debug view

For trusted operators.

May include:

- task queues;
- namespace mappings;
- recent failed execution links;
- detailed runtime diagnostics.

Should require stricter authorization.

## Secrets policy

The following must never appear in WorkflowAPI documents:

- Temporal API keys;
- mTLS private keys;
- certificates containing private key material;
- database connection strings;
- OAuth client secrets;
- service account tokens;
- Kubernetes secrets;
- raw Authorization headers.

The validator should emit an error if obvious secret-looking fields are detected.

## Runtime credential configuration

Runtime plugins should receive credentials through normal host configuration:

```json
{
  "WorkflowApi": {
    "RuntimeOverlays": {
      "Temporal": {
        "Enabled": true,
        "TargetHost": "temporal.company.internal:7233",
        "Namespace": "Commerce.OrderService"
      }
    }
  }
}
```

Secrets should be provided through environment variables, secret stores, managed identity, workload identity, or Kubernetes secrets.

## Search attributes and PII

Temporal Search Attributes are powerful for business filtering but should not casually contain personal data. WorkflowAPI should support classification metadata:

```yaml
searchAttributes:
  OrderId:
    type: Keyword
    classification: business-identifier
  CustomerEmail:
    type: Text
    classification: pii
    visibility: restricted
```

Catalog UI should hide or mask restricted attributes unless the user has permission.

## Runtime overlay authorization

The runtime overlay API must separate:

- ability to view static specs;
- ability to view aggregate metrics;
- ability to view failed execution samples;
- ability to deep-link into Temporal Web;
- ability to see search-attribute values.

Suggested scopes/roles:

```text
workflowapi:spec:read
workflowapi:catalog:read
workflowapi:runtime:metrics:read
workflowapi:runtime:executions:read
workflowapi:runtime:debug:read
workflowapi:admin
```

## Read-only guarantee for v1

WorkflowAPI v1 tooling must not perform write operations against Temporal.

Forbidden in v1 UI/catalog:

- start workflow;
- signal workflow;
- update workflow;
- terminate workflow;
- cancel workflow;
- reset workflow;
- modify schedules;
- modify Nexus endpoints.

This keeps the catalog/reference UI safe and adoption-friendly.

## Catalog deployment security

For Kubernetes or shared environments:

- run as non-root;
- set read-only root filesystem where practical;
- require HTTPS at ingress;
- support OIDC auth;
- use secret references for Temporal credentials;
- expose health endpoints separately from UI;
- disable runtime overlay by default unless configured.

## Supply-chain considerations

- Publish SBOMs for container images and NuGet packages.
- Sign container images where possible.
- Run dependency scanning and code scanning in CI.
- Treat agent skills and prompt files as code-reviewed assets.
- Never pre-approve shell/bashing capabilities in agent skills without review.

## Agent acceptance criteria

An implementation agent must provide:

- security checklist in PR template;
- no-secret validation rules;
- configurable public/internal/debug document modes;
- role-aware runtime overlay API design;
- Docker security baseline;
- documentation warning against using WorkflowAPI as a Temporal control plane in v1.

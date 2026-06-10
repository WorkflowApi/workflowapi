# WorkflowAPI Specification Pack

This pack defines the proposed **WorkflowAPI** standard and the first .NET/Temporal implementation design.

WorkflowAPI is intended to sit beside OpenAPI and AsyncAPI:

```text
OpenAPI      -> HTTP request/response APIs
AsyncAPI     -> event/message-driven APIs
WorkflowAPI  -> durable workflow APIs
```

The pack is deliberately split into focused markdown files so specialist implementation agents can work with smaller context windows.

## Files

1. [`01-workflowapi-core-spec.md`](01-workflowapi-core-spec.md)  
   The generic WorkflowAPI DSL/specification, vocabulary, document shape, extension model, examples, and compatibility rules.

2. [`02-dotnet-implementation-design.md`](02-dotnet-implementation-design.md)  
   The .NET package architecture, attributes, Scrutor-based discovery, XML comments, transformers, runtime/build-time generation, validation, and endpoint design.

3. [`03-temporal-dotnet-binding.md`](03-temporal-dotnet-binding.md)  
   The first implementation binding for Temporal .NET: what can be inferred, what must be configured, worker/task queue/namespace handling, Nexus, topology, runtime overlays, and edge cases.

4. [`04-reference-ui-and-catalog.md`](04-reference-ui-and-catalog.md)  
   The per-service WorkflowAPI reference UI and the central multi-service catalog design, including EventCatalog visualiser usage, Aspire integration, and runtime overlays.

5. [`05-agent-implementation-instructions.md`](05-agent-implementation-instructions.md)  
   Practical implementation instructions for an agentic AI engineering team: workstreams, milestones, acceptance criteria, tests, repository layout, and risk checks.

6. [`06-examples.md`](06-examples.md)  
   End-to-end examples of C# usage, generated WorkflowAPI JSON/YAML, Aspire AppHost usage, and catalog source configuration.

7. [`07-references.md`](07-references.md)  
   External references used to ground the design.

8. [`08-standalone-catalog-server.md`](08-standalone-catalog-server.md)  
   A HealthChecks.UI-style standalone catalog server design that ingests configured WorkflowAPI endpoints, caches source documents, builds a federated graph, and optionally overlays Temporal runtime metrics.

9. [`09-workflowapi-normative-document-model.md`](09-workflowapi-normative-document-model.md)  
   A stricter, fuller document-model supplement covering the normative WorkflowAPI DSL shape, nested workflows/subflows, generic bridges, Temporal Nexus binding, components, validation levels, and catalog collation rules.

10. [`10-github-organization-and-monorepo-setup.md`](10-github-organization-and-monorepo-setup.md)  
    Detailed GitHub organization and monorepo setup instructions for `https://github.com/WorkflowApi`, including repository layout, packages, CI, governance files, labels, milestones, ADRs, and agentic implementation workflow.

11. [`11-ui-core-and-runtime-plugin-architecture.md`](11-ui-core-and-runtime-plugin-architecture.md)  
    Defines the generic WorkflowAPI UI core, optional runtime overlay plugin architecture, Temporal overlay plugin responsibilities, graph ownership rules, UI/backend overlay APIs, caching, security, and implementation phases.

12. [`12-conformance-and-validation.md`](12-conformance-and-validation.md)  
    Defines conformance profiles, validation layers, diagnostic severities/codes, fixtures, and validator acceptance criteria.

13. [`13-versioning-and-compatibility.md`](13-versioning-and-compatibility.md)  
    Defines WorkflowAPI document/workflow/schema/binding versioning, compatibility rules, deprecation, and diff categories.

14. [`14-security-and-privacy.md`](14-security-and-privacy.md)  
    Defines secrets policy, public/internal/debug views, PII/search-attribute handling, runtime overlay authorization, and read-only v1 security posture.

15. [`15-cli-and-msbuild-tooling.md`](15-cli-and-msbuild-tooling.md)  
    Defines CLI commands, MSBuild export, validation, diff, bundle, publish, init, deterministic output, and CI usage.

16. [`16-runtime-overlay-api.md`](16-runtime-overlay-api.md)  
    Defines backend API endpoints and DTOs for workflow, step, edge, bridge, and execution runtime overlays.

17. [`17-catalog-collation-rules.md`](17-catalog-collation-rules.md)  
    Defines source fetching, memory cache, merge identity, conflict rules, bridge resolution, environment partitioning, and partial failure behaviour.

18. [`18-v1-scope-and-non-goals.md`](18-v1-scope-and-non-goals.md)  
    Defines the v1 implementation boundary, non-goals, milestones, and definition of done.

19. [`19-agentic-delivery-and-repo-ai-tooling.md`](19-agentic-delivery-and-repo-ai-tooling.md)  
    Defines the agentic delivery model, `.agents/skills`, `.github/copilot-instructions.md`, path-specific instructions, prompt files, and PR expectations.

Additional artifacts:

- [`schemas/workflowapi.schema.json`](schemas/workflowapi.schema.json)
- [`schemas/workflowapi-temporal-binding.schema.json`](schemas/workflowapi-temporal-binding.schema.json)
- [`schemas/workflowapi-runtime-overlay.schema.json`](schemas/workflowapi-runtime-overlay.schema.json)
- [`repo-template/`](repo-template/) with starter `AGENTS.md`, `.agents/skills`, and `.github` Copilot/CI assets.

## Design posture

This is a **v0.1 design specification**, not a final standard. It should be implemented experimentally, with deliberate feedback loops from:

- real Temporal .NET projects;
- Aspire local development scenarios;
- generated specs checked into CI;
- a single-service reference UI;
- a later multi-service central catalog.

## Key conclusion

The most important design decision is separation of concerns:

```text
WorkflowAPI document generation  !=  reference UI
WorkflowAPI core model           !=  Temporal binding
WorkflowAPI per-service endpoint !=  central catalog
WorkflowAPI contract metadata    !=  runtime observability data
WorkflowAPI generic UI core      !=  Temporal runtime overlay plugin
WorkflowAPI conformance tooling    !=  catalog runtime overlay collection
Agent skills and repo instructions !=  product documentation
```

This mirrors the direction of the modern .NET OpenAPI stack, where `Microsoft.AspNetCore.OpenApi` generates documents and UIs such as Scalar consume them separately.

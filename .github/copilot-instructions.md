# GitHub Copilot Instructions for WorkflowAPI

WorkflowAPI defines durable workflow API contracts, analogous to OpenAPI and AsyncAPI.

Repository standards:

- Target .NET 10 LTS for .NET projects.
- Keep nullable reference types enabled.
- Keep WorkflowAPI core generic and runtime-neutral.
- Temporal support belongs in Temporal-specific binding/runtime overlay packages.
- UI core must render WorkflowAPI documents without Temporal credentials.
- Runtime overlays are optional plugins and must be read-only in v1.
- Do not include secrets or credentials in specs, examples, tests, or docs.
- When changing the DSL, update: prose spec, JSON Schema, examples, validator tests, and docs.
- When changing generated output, update snapshot/golden tests.
- Prefer deterministic output and stable IDs.

References:
- See /docs/specs/ for the normative WorkflowAPI specification.
- See /docs/assets/ for UI mockups.

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan at
specs/002-visualizer-docker-compose/plan.md
<!-- SPECKIT END -->

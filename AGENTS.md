# WorkflowAPI Agent Guide

WorkflowAPI is a specification and tooling ecosystem for durable workflow APIs, analogous to OpenAPI for HTTP APIs and AsyncAPI for event/message APIs.

Before changing code:

1. Read the relevant document under `docs/specification/`.
2. Use the relevant skill under `.agents/skills/*/SKILL.md`.
3. Keep spec, schemas, examples, and tests synchronized.
4. Run the relevant validation commands before proposing changes.

Rules:

- Keep the WorkflowAPI core runtime-neutral.
- Put Temporal-specific logic in the Temporal binding/runtime overlay packages.
- Do not add write/control-plane actions against Temporal in v1.
- Do not put secrets in examples, specs, tests, or generated files.
- Prefer small PRs with clear acceptance criteria.

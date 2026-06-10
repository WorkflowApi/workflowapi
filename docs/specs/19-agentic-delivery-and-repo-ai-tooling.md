# 19. Agentic Delivery and Repository AI Tooling

## Purpose

WorkflowAPI will likely be implemented by a mixed human and AI agentic engineering team. The repository should therefore contain clear, scoped, machine-readable instructions that keep agents aligned without overloading every prompt with the full project context.

This document defines the recommended AI tooling structure for the monorepo.

## Current guidance basis

GitHub Copilot supports repository-wide instructions via `.github/copilot-instructions.md`, path-specific instructions via `.github/instructions/**/*.instructions.md`, and agent instructions such as `AGENTS.md` depending on surface. GitHub also documents agent skills, where project skills may live in `.github/skills`, `.claude/skills`, or `.agents/skills`; skills are intended for detailed task-specific instructions, while custom instructions are better for broad standards used on most tasks.

WorkflowAPI should use:

```text
.agents/skills/        generic agent skills, vendor-neutral where possible
.github/               GitHub/Copilot-specific instructions, prompts, workflows, templates
AGENTS.md              root agent orientation and task routing
```

## Repository AI structure

Recommended monorepo structure:

```text
workflowapi/
  AGENTS.md

  .agents/
    skills/
      workflowapi-spec/
        SKILL.md
      dotnet-workflowapi-implementation/
        SKILL.md
      temporal-binding/
        SKILL.md
      reference-ui/
        SKILL.md
      catalog-server/
        SKILL.md
      conformance-validation/
        SKILL.md
      security-review/
        SKILL.md

  .github/
    copilot-instructions.md
    instructions/
      dotnet.instructions.md
      workflowapi-spec.instructions.md
      ui.instructions.md
      tests.instructions.md
      security.instructions.md
      docs.instructions.md
    prompts/
      implement-workflowapi-feature.prompt.md
      review-workflowapi-change.prompt.md
      add-conformance-test.prompt.md
      update-spec-and-schemas.prompt.md
      create-adr.prompt.md
    workflows/
      ci.yml
      validate-specs.yml
      publish-packages.yml
    ISSUE_TEMPLATE/
      feature.yml
      bug.yml
      spec-change.yml
    pull_request_template.md
```

## Root `AGENTS.md`

The root `AGENTS.md` should be short and directive. It should tell any agent:

- what WorkflowAPI is;
- where the spec lives;
- how to build/test;
- how to choose a skill;
- not to make unapproved breaking spec changes;
- to update tests, schemas, examples, and docs together.

Example outline:

```markdown
# WorkflowAPI Agent Guide

WorkflowAPI is a specification and tooling ecosystem for durable workflow APIs, analogous to OpenAPI and AsyncAPI.

Before changing code:
1. Read `docs/specification/` relevant to your area.
2. Use the relevant `.agents/skills/*/SKILL.md`.
3. Keep spec, schema, examples, and tests synchronized.
4. Run `dotnet test` and `workflowapi validate examples/specs --recursive`.

Do not introduce Temporal-specific concepts into the core model unless represented as bindings.
```

## `.github/copilot-instructions.md`

This file should contain broad repository-wide rules:

- target .NET 10 LTS;
- use nullable reference types;
- treat warnings as build-quality issues;
- keep WorkflowAPI core generic;
- Temporal-specific features must live in the Temporal binding package;
- UI core must not directly call Temporal;
- runtime overlays are plugins;
- no secrets in specs;
- update docs/tests/examples in the same PR.

Keep it concise. Detailed task rules belong in skills or path-specific instructions.

## Path-specific Copilot instructions

Use `.github/instructions/*.instructions.md` for area-specific rules.

Examples:

```text
.github/instructions/dotnet.instructions.md
  applies to src/dotnet/** and tests/dotnet/**

.github/instructions/workflowapi-spec.instructions.md
  applies to docs/specification/** and schemas/**

.github/instructions/ui.instructions.md
  applies to src/web/**

.github/instructions/security.instructions.md
  applies to all files when security-sensitive changes are made
```

These files should be short and highly scoped.

## Prompt files

Use `.github/prompts/*.prompt.md` for repeatable workflows:

- implement a feature;
- add a conformance test;
- review a change;
- update schema and docs;
- write an ADR.

Prompt files should specify expected outputs and verification commands.

## Agent skills

Skills are deeper task packs. Store them in `.agents/skills` to keep them generic and portable while still usable by tools that understand agent skills.

Each skill should contain:

```text
SKILL.md
optional examples/
optional checklists/
optional scripts/ only when safe and reviewed
```

Do not pre-approve shell/bash execution by default. Agent skills that run scripts must be reviewed like code.

### Skill: `workflowapi-spec`

Use for changes to:

- normative DSL;
- JSON Schema;
- compatibility rules;
- bridges;
- nested workflows;
- validation rules.

Acceptance:

- update prose spec;
- update JSON Schema;
- update positive/negative examples;
- update validator tests;
- add ADR for major model change.

### Skill: `dotnet-workflowapi-implementation`

Use for:

- attributes;
- Scrutor scanning;
- XML docs;
- transformers;
- `MapWorkflowApi`;
- MSBuild/CLI export.

Acceptance:

- source compiles;
- unit tests pass;
- generated document snapshots updated;
- no Temporal concepts leak into core packages.

### Skill: `temporal-binding`

Use for:

- Temporal .NET attribute inference;
- namespace/task queue binding;
- worker registration metadata;
- Temporal Nexus bridge support;
- runtime overlay provider.

Acceptance:

- Temporal-specific code remains in Temporal package;
- no runtime credentials in generated docs;
- sample Temporal project validates.

### Skill: `reference-ui`

Use for:

- generic WorkflowAPI UI core;
- graph rendering;
- EventCatalog visualiser adapter;
- runtime overlay decoration;
- single-service reference UI.

Acceptance:

- UI renders static specs without runtime plugin;
- Temporal overlay is optional;
- accessibility smoke checks pass.

### Skill: `catalog-server`

Use for:

- standalone catalog server;
- configured source list;
- memory cache;
- source refresh;
- graph collation;
- Docker image;
- Aspire hosting extension.

Acceptance:

- partial source failure does not break catalog;
- stale source state is visible;
- Docker image runs with sample config.

### Skill: `conformance-validation`

Use for:

- validator;
- diagnostics;
- CLI validation;
- fixtures;
- diff engine.

Acceptance:

- JSON diagnostics are stable;
- invalid examples fail with expected codes;
- strict mode works.

### Skill: `security-review`

Use for:

- runtime credential handling;
- Docker hardening;
- redaction;
- RBAC;
- no-secrets validation;
- agent skill safety.

Acceptance:

- no secret material in specs;
- no write operations against Temporal in v1;
- threat checklist updated.

## Agent fleet workflow

Recommended agentic execution model:

```text
Planner agent
  creates issue breakdown and ADR proposals

Spec agent
  updates DSL, schemas, examples

.NET agent
  implements generator/runtime endpoint

Temporal agent
  implements Temporal binding and runtime overlay

UI agent
  implements reference/catalog UI

Conformance agent
  adds tests/fixtures/diagnostics

Security agent
  reviews risk, redaction, credentials, Docker

Integrator agent
  runs full build/test, resolves conflicts, prepares PR
```

No agent should merge its own PR without human review in the early project phase.

## Context-window discipline

Agents should not load the whole repository by default.

Instead:

- read `AGENTS.md`;
- read relevant skill;
- read relevant spec file;
- inspect targeted source/test files;
- update only files required by acceptance criteria.

This is important because WorkflowAPI will have many packages and long specifications.

## PR requirements

Every implementation PR should state:

```text
Spec files changed:
Schemas changed:
Examples changed:
Tests added/updated:
Validation commands run:
Compatibility impact:
Security/privacy impact:
```

## Agent acceptance criteria

An implementation agent must add the repo-template files described by this document and ensure they are referenced from README/setup documentation.

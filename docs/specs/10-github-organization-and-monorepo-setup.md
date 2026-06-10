# 10. GitHub Organization and Monorepo Setup Instructions

Status: v0.1 design guidance  
Target organization: `https://github.com/WorkflowApi`  
Recommended initial repository: `https://github.com/WorkflowApi/workflowapi`

## 1. Purpose

This document defines the recommended GitHub organization and repository setup for the WorkflowAPI project.

WorkflowAPI is intended to become an ecosystem, not merely a single sample application. It includes:

- the WorkflowAPI specification;
- JSON Schema definitions;
- .NET generation packages;
- the first Temporal .NET binding;
- a single-service reference UI;
- a central multi-service catalog server;
- an Aspire hosting integration;
- sample Temporal applications;
- CLI/build-time tooling;
- validation/diff/publish tooling.

For the first phase, the project should use a **single monorepo under the `WorkflowApi` GitHub organization**. This keeps the specification, implementation, UI, samples, and agent instructions in one place while the design is still evolving.

## 2. Repository strategy

### 2.1 Recommended starting point

Create one main repository:

```text
WorkflowApi/workflowapi
```

This repository should contain the specification, .NET implementation, UI, catalog server, Aspire integration, examples, and tests.

### 2.2 Why a monorepo first

A monorepo is recommended for v0.1 because the specification and implementation will change together. Early splitting would create unnecessary coordination overhead.

The monorepo should make it easy for both humans and agentic AI implementation teams to work across:

```text
specification changes
.NET generator changes
Temporal binding changes
UI changes
catalog server changes
sample application changes
tests and validation changes
```

A monorepo also keeps cross-cutting refactors simple while concepts such as `host`, `implements`, `dependsOn`, `bridges`, `bindings`, and runtime overlays are still stabilizing.

### 2.3 When to split later

The project may later split into multiple repositories if the boundaries become stable and independent.

Possible future repositories:

```text
WorkflowApi/spec
WorkflowApi/workflowapi-dotnet
WorkflowApi/workflowapi-ui
WorkflowApi/workflowapi-catalog
WorkflowApi/workflowapi-samples
WorkflowApi/workflowapi-aspire
```

Do not split until at least these are true:

- the core WorkflowAPI document model has reached a stable v0.2 or v1.0 candidate;
- the .NET package boundaries are stable;
- the catalog server has a stable ingestion API;
- the reference UI can consume documents without internal implementation coupling;
- CI/release automation is mature enough to handle cross-repository versioning.

## 3. Recommended repository layout

The initial monorepo should use a predictable layout that separates specification, implementation, UI, samples, and engineering assets.

```text
workflowapi/
  README.md
  LICENSE
  CONTRIBUTING.md
  CODE_OF_CONDUCT.md
  SECURITY.md
  GOVERNANCE.md
  ROADMAP.md

  .github/
    workflows/
      build-dotnet.yml
      test-dotnet.yml
      validate-spec.yml
      build-ui.yml
      build-container.yml
      publish-prerelease.yml
    ISSUE_TEMPLATE/
      bug_report.yml
      feature_request.yml
      spec_change.yml
      implementation_task.yml
    pull_request_template.md
    dependabot.yml

  docs/
    specification/
      workflowapi.md
      workflowapi-normative-model.md
      workflowapi-json-schema.md
      bridges.md
      nested-workflows.md
      catalog-collation.md
      runtime-overlays.md
    bindings/
      temporal-dotnet.md
      temporal-nexus.md
    implementation/
      dotnet-generator.md
      aspnetcore-integration.md
      reference-ui.md
      catalog-server.md
      aspire-hosting.md
    adr/
      0001-use-workflowapi-name.md
      0002-monorepo-first.md
      0003-temporal-first-binding.md
      0004-separate-generation-and-ui.md
      0005-use-scrutor-for-runtime-discovery.md

  schemas/
    workflowapi.schema.json
    workflowapi-temporal-binding.schema.json
    workflowapi-catalog-source.schema.json

  src/
    dotnet/
      WorkflowApi.Abstractions/
      WorkflowApi.AspNetCore/
      WorkflowApi.Temporal/
      WorkflowApi.Reference/
      WorkflowApi.Catalog.Server/
      WorkflowApi.Cli/
      WorkflowApi.MSBuild/
      Aspire.Hosting.WorkflowApiCatalog/
    web/
      workflowapi-reference/
      workflowapi-catalog-ui/

  samples/
    temporal-dotnet-single-service/
      Risk.Workflows/
      Risk.ApiHost/
      Risk.Worker/
      AppHost/
    temporal-dotnet-multi-service/
      Offer.Workflows/
      Offer.Host/
      Risk.Workflows/
      Risk.Host/
      Document.Workflows/
      Document.Host/
      AppHost/
    standalone-catalog-server/
      appsettings.example.json
      docker-compose.yml

  tests/
    dotnet/
      WorkflowApi.Abstractions.Tests/
      WorkflowApi.AspNetCore.Tests/
      WorkflowApi.Temporal.Tests/
      WorkflowApi.Catalog.Server.Tests/
    schema/
      valid-documents/
      invalid-documents/
    integration/
      TemporalSingleService.Tests/
      TemporalMultiServiceCatalog.Tests/
      AspireHosting.Tests/

  eng/
    build/
    scripts/
    packaging/
    release/

  artifacts/
    .gitkeep
```

## 4. Repository bootstrap commands

If using the GitHub CLI, the initial setup could be:

```bash
gh repo create WorkflowApi/workflowapi --public --clone
cd workflowapi

mkdir -p docs/specification docs/bindings docs/implementation docs/adr
mkdir -p schemas
mkdir -p src/dotnet src/web
mkdir -p samples/temporal-dotnet-single-service samples/temporal-dotnet-multi-service samples/standalone-catalog-server
mkdir -p tests/dotnet tests/schema/valid-documents tests/schema/invalid-documents tests/integration
mkdir -p eng/build eng/scripts eng/packaging eng/release
mkdir -p .github/workflows .github/ISSUE_TEMPLATE

touch LICENSE CONTRIBUTING.md CODE_OF_CONDUCT.md SECURITY.md GOVERNANCE.md ROADMAP.md
```

Add a root `.gitignore` for .NET, Node, build artifacts, local secrets, IDE folders, and generated package outputs.

Recommended initial branch:

```text
main
```

Recommended early milestone branches:

```text
feature/spec-v0.1
feature/dotnet-generator-v0.1
feature/reference-ui-v0.1
feature/catalog-server-v0.1
```

## 5. GitHub organization settings

The `WorkflowApi` organization should be configured as a real open-source project space from the beginning.

Recommended org-level settings:

- enable two-factor authentication requirement for members;
- restrict package publishing to maintainers initially;
- enable GitHub Discussions once the first public README is ready;
- use organization secrets only for publish credentials;
- avoid storing Temporal credentials, cloud credentials, or test secrets in repository-level files;
- create teams early, even if they only contain one maintainer initially.

Suggested teams:

```text
maintainers
spec-editors
dotnet-maintainers
ui-maintainers
catalog-maintainers
security-reviewers
```

## 6. Repository governance files

### 6.1 `README.md`

The root README should be short and direct. It should explain:

- what WorkflowAPI is;
- how it relates to OpenAPI and AsyncAPI;
- that Temporal .NET is the first binding;
- how to run the first sample;
- where the specification lives;
- project status and stability warning.

Suggested tagline:

```text
WorkflowAPI is a specification and tooling ecosystem for durable workflow APIs,
analogous to OpenAPI for HTTP APIs and AsyncAPI for event/message APIs.
```

### 6.2 `CONTRIBUTING.md`

Should define:

- how to propose spec changes;
- how to propose binding-specific changes;
- how to add tests;
- how to run the build locally;
- expectations for examples and documentation.

### 6.3 `GOVERNANCE.md`

Should define:

- maintainer responsibilities;
- spec-change process;
- release approval process;
- compatibility policy;
- how experimental features are marked.

### 6.4 `SECURITY.md`

Should explicitly cover:

- how to report vulnerabilities;
- handling of Temporal credentials in examples;
- that runtime overlays must be read-only by default;
- no secrets in WorkflowAPI documents;
- warnings around PII in search attributes and examples.

## 7. Initial package naming

### 7.1 NuGet packages

Recommended NuGet package IDs:

```text
WorkflowApi.Abstractions
WorkflowApi.AspNetCore
WorkflowApi.Temporal
WorkflowApi.Reference
WorkflowApi.Catalog.Server
WorkflowApi.Cli
WorkflowApi.MSBuild
Aspire.Hosting.WorkflowApiCatalog
```

Package responsibilities:

| Package | Responsibility |
|---|---|
| `WorkflowApi.Abstractions` | Attributes, core document model, validation contracts. |
| `WorkflowApi.AspNetCore` | `AddWorkflowApi`, `MapWorkflowApi`, document generation, XML comments, transformers, Scrutor scanning. |
| `WorkflowApi.Temporal` | Temporal .NET attribute inference, Temporal binding model, worker/task-queue/namespace metadata helpers. |
| `WorkflowApi.Reference` | Single-service reference UI, similar in spirit to Scalar for OpenAPI. |
| `WorkflowApi.Catalog.Server` | Standalone multi-source catalog server. |
| `WorkflowApi.Cli` | Validate, export, diff, publish, and inspect WorkflowAPI documents. |
| `WorkflowApi.MSBuild` | Build-time document generation. |
| `Aspire.Hosting.WorkflowApiCatalog` | Aspire AppHost integration for the catalog container. |

### 7.2 Container image names

Recommended GHCR image names:

```text
ghcr.io/workflowapi/workflowapi-catalog
ghcr.io/workflowapi/workflowapi-samples-risk-service
ghcr.io/workflowapi/workflowapi-samples-offer-service
```

The catalog image should run as a standalone .NET application that can ingest configured WorkflowAPI source endpoints.

### 7.3 NPM package names, if needed later

Potential package names:

```text
@workflowapi/spec
@workflowapi/reference-ui
@workflowapi/catalog-ui
@workflowapi/eventcatalog-adapter
```

Avoid publishing NPM packages until the UI component boundaries are clear.

## 8. Versioning and releases

### 8.1 Specification version

The WorkflowAPI document should include a spec version field:

```yaml
workflowApi: 0.1.0
```

The spec version is independent of NuGet package versions.

### 8.2 Package versions

Initial packages should remain pre-1.0:

```text
0.1.0-alpha.1
0.1.0-alpha.2
0.1.0-beta.1
```

### 8.3 Release tags

Use repo tags such as:

```text
v0.1.0-alpha.1
v0.1.0-alpha.2
v0.1.0-beta.1
```

The release notes should explicitly list:

- spec version;
- package versions;
- schema changes;
- breaking changes;
- sample compatibility;
- known gaps.

## 9. GitHub Actions design

### 9.1 Minimum initial workflows

Create separate workflows rather than one enormous workflow:

```text
build-dotnet.yml
build-ui.yml
validate-spec.yml
test-dotnet.yml
build-container.yml
```

### 9.2 `validate-spec.yml`

Should validate:

- `schemas/workflowapi.schema.json` is valid JSON Schema;
- example WorkflowAPI documents validate;
- invalid examples fail validation;
- documentation links are not obviously broken;
- generated example output is deterministic.

### 9.3 `build-dotnet.yml`

Should run:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
```

Later add:

```bash
dotnet format --verify-no-changes
```

### 9.4 `build-ui.yml`

Should use `pnpm` if the UI is TypeScript/React-based.

Recommended checks:

```bash
pnpm install --frozen-lockfile
pnpm lint
pnpm typecheck
pnpm test
pnpm build
```

### 9.5 `build-container.yml`

Should build the standalone catalog server image but only publish from tagged releases or trusted branches.

## 10. Issue labels and milestones

Recommended labels:

```text
area/spec
area/dotnet
area/temporal
area/ui
area/catalog
area/aspire
area/samples
area/docs
area/testing
area/security
kind/bug
kind/feature
kind/design
kind/refactor
kind/question
status/needs-design
status/ready-for-agent
status/blocked
status/good-first-issue
```

Recommended milestones:

```text
v0.1 Spec Draft
v0.1 .NET Generator
v0.1 Temporal Binding
v0.1 Single-Service Reference UI
v0.1 Standalone Catalog Server
v0.1 Aspire Integration
v0.1 Samples
```

## 11. Agentic AI implementation workflow

The monorepo should be structured to support specialist sub-agents with tight context windows.

Recommended agent workstreams:

| Agent | Scope |
|---|---|
| Spec agent | `docs/specification`, `schemas`, schema tests. |
| .NET generator agent | `src/dotnet/WorkflowApi.*`, unit tests. |
| Temporal binding agent | `src/dotnet/WorkflowApi.Temporal`, Temporal samples. |
| UI agent | `src/web`, `WorkflowApi.Reference`, visualisation tests. |
| Catalog agent | `WorkflowApi.Catalog.Server`, source ingestion, memory cache, graph collation. |
| Aspire agent | `Aspire.Hosting.WorkflowApiCatalog`, AppHost samples. |
| Docs agent | README, examples, tutorials, ADRs. |

Each agent should receive:

- the relevant markdown design file;
- the normative document model;
- one or more acceptance criteria issues;
- tests to make pass;
- explicit instruction not to broaden scope without an issue/ADR.

## 12. ADRs to create immediately

Create these architectural decision records at project start:

```text
0001-use-workflowapi-name.md
0002-start-as-monorepo.md
0003-temporal-dotnet-first-binding.md
0004-separate-spec-generation-from-ui.md
0005-use-scrutor-for-runtime-discovery.md
0006-support-runtime-and-build-time-document-generation.md
0007-use-workflow-host-terminology.md
0008-standalone-catalog-server-ingests-configured-sources.md
```

Each ADR should include:

- status;
- context;
- decision;
- consequences;
- alternatives considered.

## 13. Standalone catalog server repository role

Even though the monorepo includes per-service `MapWorkflowApiReference`, the catalog server is still an important deployable.

It should support:

- running as a standalone .NET application;
- running as a Docker container;
- running in Kubernetes;
- running in Aspire via `Aspire.Hosting.WorkflowApiCatalog`;
- configured source endpoints, similar in spirit to HealthChecks UI;
- convention-based `/.well-known/workflow-api.json` discovery;
- memory cache at startup;
- periodic refresh;
- partial failure handling;
- optional Temporal credentials for runtime overlays.

This avoids forcing users to add WorkflowAPI UI into every service while still enabling a central view.

## 14. Recommended first implementation order

Do not start with the UI. Start with the document model and generator.

Recommended order:

1. Create monorepo skeleton.
2. Add core specification docs and JSON Schema.
3. Add `WorkflowApi.Abstractions`.
4. Add `WorkflowApi.AspNetCore` with `AddWorkflowApi` and `MapWorkflowApi`.
5. Add Scrutor-based discovery for WorkflowAPI attributes and Temporal SDK attributes.
6. Add XML comment ingestion.
7. Add document/workflow/operation/step/schema transformers.
8. Add `WorkflowApi.Temporal` with binding metadata.
9. Add a minimal Temporal .NET sample.
10. Add `MapWorkflowApiReference` single-service UI.
11. Add standalone catalog server with configured source list and memory cache.
12. Add Aspire hosting extension.
13. Add runtime overlay from Temporal Visibility/History APIs.
14. Add CI/export/publish tooling.

## 15. Definition of done for repository setup

The initial setup is complete when:

- `WorkflowApi/workflowapi` exists;
- root governance files exist;
- repository layout exists;
- first ADRs exist;
- `README.md` explains the project clearly;
- `docs/specification` contains the normative spec draft;
- `schemas/workflowapi.schema.json` exists, even if incomplete;
- GitHub Actions can run basic validation;
- sample issues/milestones/labels exist;
- an agent can pick up a scoped issue and implement without reading the entire repository.

## 16. Non-goals for initial setup

Do not do these in the first setup phase:

- split into multiple repositories;
- publish stable NuGet packages;
- promise v1 compatibility;
- implement every possible workflow engine binding;
- create a complex governance model;
- require a central catalog server for single-service local development;
- depend on live Temporal access to render the basic spec UI;
- require users to hand-write DSL files for basic usage.

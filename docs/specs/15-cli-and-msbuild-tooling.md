> **v1 tightened scope note**  
> This file is retained as a post-v1 design note unless a section explicitly says otherwise. WorkflowAPI v1 is limited to the compact durable execution workflow API specification, .NET generation from workflow attributes or fluent definitions, Temporal static binding metadata, and static workflow display. Runtime overlays, catalogue collation, source polling, workflow control-plane actions, BPMN-style modelling and runtime observability are outside v1 scope.

# 15. WorkflowAPI CLI and MSBuild Tooling

## Purpose

WorkflowAPI should work in local development, CI, and enterprise catalog publishing. That requires CLI and build-time tooling in addition to ASP.NET Core runtime endpoints.

## Tooling principles

- Runtime endpoint generation and build-time export must produce equivalent documents.
- CLI tools must be deterministic and CI-friendly.
- Tools must support JSON diagnostics.
- CLI must avoid requiring a live Temporal connection for static document generation.
- Publishing to a catalog must be optional and decoupled from document generation.

## Proposed packages

```text
WorkflowApi.Cli
WorkflowApi.MSBuild
WorkflowApi.Generator
WorkflowApi.Validation
WorkflowApi.Diff
```

The CLI can be packaged as a .NET tool:

```bash
dotnet tool install --global WorkflowApi.Cli
```

## Commands

### `workflowapi validate`

Validate one or more WorkflowAPI documents.

```bash
workflowapi validate ./artifacts/workflow-api.json
workflowapi validate ./specs --recursive --profile catalog-source
workflowapi validate ./specs --format json --output diagnostics.json
```

Options:

```text
--profile core-document|temporal-binding|reference-ui|catalog-source|runtime-overlay
--strict
--format text|json|sarif
--output <path>
--fail-on error|warning
```

### `workflowapi export`

Generate a document from a built .NET assembly or project.

```bash
workflowapi export \
  --assembly ./src/Risk.Workflows/bin/Release/net10.0/Risk.Workflows.dll \
  --output ./artifacts/workflow-api.json
```

Options:

```text
--project <csproj>
--assembly <dll>
--document-name v1
--include-internal
--view public|internal|debug
--temporal-namespace <namespace>
--temporal-task-queue <taskQueue>
--format json|yaml
```

### `workflowapi diff`

Compare two documents.

```bash
workflowapi diff old.json new.json --format markdown
workflowapi diff old.json new.json --fail-on contract-breaking
```

Options:

```text
--format text|json|markdown
--fail-on contract-breaking|warning|any-change
--include-runtime-bindings
```

### `workflowapi bundle`

Bundle many specs for a catalog.

```bash
workflowapi bundle ./specs --output catalog-bundle.json
```

The bundle should preserve source identity and document hashes.

### `workflowapi publish`

Publish a document or bundle to a catalog server.

```bash
workflowapi publish ./artifacts/workflow-api.json \
  --catalog https://workflowapi.company.internal \
  --source commerce-order-worker \
  --environment prod
```

### `workflowapi schema`

Emit the JSON Schema for the current CLI version.

```bash
workflowapi schema --output workflowapi.schema.json
```

### `workflowapi init`

Create starter repo assets.

```bash
workflowapi init --monorepo --github-copilot --agent-skills
```

Should be able to scaffold:

- `.github/copilot-instructions.md`;
- `.github/instructions/*.instructions.md`;
- `.github/prompts/*.prompt.md`;
- `.agents/skills/*/SKILL.md`;
- sample workflow;
- basic CI.

## MSBuild integration

Build-time export should be optional and controlled by MSBuild properties.

Example:

```xml
<PropertyGroup>
  <GenerateWorkflowApiDocument>true</GenerateWorkflowApiDocument>
  <WorkflowApiDocumentName>v1</WorkflowApiDocumentName>
  <WorkflowApiOutputPath>$(MSBuildProjectDirectory)/artifacts/workflow-api.json</WorkflowApiOutputPath>
  <WorkflowApiView>internal</WorkflowApiView>
</PropertyGroup>
```

Optional items:

```xml
<ItemGroup>
  <WorkflowApiAssembly Include="$(TargetPath)" />
  <WorkflowApiXmlDocs Include="$(DocumentationFile)" />
</ItemGroup>
```

## CI examples

### Validate generated spec

```yaml
name: workflowapi
on:
  pull_request:
  push:
    branches: [ main ]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet build --configuration Release
      - run: dotnet tool restore
      - run: dotnet workflowapi export --project src/Risk.Workflows/Risk.Workflows.csproj --output artifacts/workflow-api.json
      - run: dotnet workflowapi validate artifacts/workflow-api.json --strict
```

### Diff spec in pull request

```bash
workflowapi diff ./baseline/workflow-api.json ./artifacts/workflow-api.json --fail-on contract-breaking
```

## Output determinism

Exported documents should be deterministic:

- stable ordering of workflows, operations, steps, edges, schemas;
- stable generated ids;
- no timestamps unless explicitly requested;
- no machine-local paths unless debug mode is enabled;
- stable schema references.

## Agent acceptance criteria

An implementation agent must deliver:

- CLI project skeleton;
- `validate`, `export`, and `diff` commands first;
- JSON diagnostics output;
- deterministic output tests;
- GitHub Actions examples;
- MSBuild property design and initial target file;
- documentation for local and CI usage.

# Temporal .NET Scanner Implementation Plan

**Goal:** A .NET tool that scans a Temporal .NET project for workflow/activity/signal/query/update/Nexus attributes, cross-references them with `TemporalWorkerOptions` registered in DI, and emits one WorkflowAPI YAML document per worker — consumable by the existing React Flow visualiser.

**Design source of truth:** `docs/hackweek/2026-06-17-temporal-dotnet-scanner-design.md`. Anything implementation-level (types, attribute names, file shape) is defined there. This document just sequences the work.

**Hackweek rule:** Tooling never runs git. Every "✅ Done" below is a pause for Rebecca to review and commit when ready.

**Naming rule:** All emitted YAML and code-facing types use the locked vocabulary in `.squad/decisions/inbox/coordinator-naming-conventions-locked.md`. Any rename requires team discussion.

---

## File layout (new, under `src/dotnet/`)

```
src/dotnet/
  WorkflowApi.sln
  Directory.Build.props
  WorkflowApi.Abstractions/        POCOs only; zero external refs
  WorkflowApi.AspNetCore/          DI, endpoints, YAML/JSON writers; no Temporal refs
  WorkflowApi.Temporal/            Scrutor scan + DI introspection + Nexus + CLI
  tests/
    WorkflowApi.Abstractions.Tests/
    WorkflowApi.AspNetCore.Tests/
    WorkflowApi.Temporal.Tests/
    WorkflowApi.Architecture.Tests/
    fixtures/
      SimpleWorker/              one workflow, one activity, one task queue
      MultiWorker/               two task queues, distinct workflows
      NexusService/              [NexusService]/[NexusOperation] declarations
      ClassLibraryNoTemporal/    negative case
```

---

## Phase 1A — Foundation

### Task 1: Solution + `Directory.Build.props`
- .NET 10, nullable on, warnings-as-errors, implicit usings.
- Empty solution builds clean.
- ✅ Pause.

### Task 2: `WorkflowApi.Abstractions` POCOs

**Shape source of truth:** `docs/specs/09-workflowapi-normative-document-model.md` and the existing UI fixture `src/web/workflow-react-flow-visualizer-mvp/src/data/risk-enrichment.workflowapi.yaml`. POCOs must round-trip to a **valid subset** of that shape.

Records to create (one C# record per type, all in namespace `WorkflowApi.Abstractions`):

- `WorkflowApiDocument` — top-level. Fields: `WorkflowApi` (string), `Info` (`InfoDef?`), `Host` (`HostDef`), `Bindings` (`BindingsDef`), `Workflows` (`IReadOnlyDictionary<string, WorkflowDef>` keyed by workflow name), `Activities` (`IReadOnlyDictionary<string, ActivityDef>?` keyed by activity id), `Bridges` (`IReadOnlyDictionary<string, BridgeDef>?` keyed by bridge id), `Diagnostics` (`IReadOnlyList<Diagnostic>`).
- `InfoDef` — `Title`, `Version`, `Summary?`, `Description?`.
- `HostDef` — `Id`, `Name?`. (Other fields like `owner`, `environment`, `baseUrl` are spec-permitted but not derivable in Phase 1; omit.)
- `BindingsDef` — `Temporal` (`TemporalBindingDef?`).
- `TemporalBindingDef` — `Namespace`, `TaskQueue`, `WorkerHost?`, `Sdk?` (default `"temporal-dotnet"`).
- `WorkflowDef` — `Name`, `DisplayName?`, `Run` (`OperationDef`), `Signals` (`IReadOnlyDictionary<string, OperationDef>`), `Queries` (`IReadOnlyDictionary<string, OperationDef>`), `Updates` (`IReadOnlyDictionary<string, OperationDef>`), `Bindings` (`BindingsDef?` for overlay).
- `OperationDef` — `OperationId`, `Summary?`, `Input` (`SchemaRefDef?`), `Output` (`SchemaRefDef?`), `Bindings` (`BindingsDef?` for overlay per spec §12.1). Phase 1 leaves `Input`/`Output`/`Bindings` null (no schema generation or operation-level overlay yet).
- `SchemaRefDef` — `Ref` (string; serializes as `$ref`). Defined now for forward compatibility even though Phase 1 doesn't populate it.
- `BridgeDef` — `Id`, `DisplayName?`, `Kind` (string, e.g. `"nexus"`), `Summary?`, `Operations` (`IReadOnlyList<string>` of operation names).
- `ActivityDef` — `Id`, `Title?`, `Summary?`, `Group?`, `Visibility?` (default `"internal"`).
- `Diagnostic` — `Severity` (`DiagnosticSeverity` enum), `Code`, `Message`, `Target?`.
- `EmittedDocument` — wrapper used by serializers/endpoints: `TaskQueue` (string), `Document` (`WorkflowApiDocument`).

**Critical naming rules** (per `.squad/decisions/inbox/coordinator-naming-conventions-locked.md`):
- The method-identifier field on operations is `operationId`, **not** `method`.
- `signals`, `queries`, `updates`, `workflows`, `bridges` serialize as **maps keyed by name**, not lists.
- `bindings.temporal` is the binding key for the Temporal binding kind. `bridge.kind = "nexus"` for Nexus bridges.
- `$ref` (with the dollar sign) is the JSON-pointer escape for schema references — serializers must emit the literal `$ref` key.

**Constraints:**
- Zero package references.
- All collections are `IReadOnlyDictionary<,>` or `IReadOnlyList<>`.
- Use C# records.

✅ Pause.

### Task 3: Abstractions sanity tests
- Record equality + enum round-trip. Cheap smoke test that the model compiles and behaves.
- ✅ Pause.

---

## Phase 1B — Output (built first so scanning has a target)

### Task 4: YAML writer (`WorkflowApi.AspNetCore.Serialization.WorkflowApiYamlWriter`)
- `YamlDotNet`, camelCase, omit-null.
- Deterministic key order: `workflowApi → host → workflows → activities → bridges → diagnostics`.
- Verify snapshot test on a hand-built document.
- ✅ Pause.

### Task 5: JSON writer
- Same input model, `System.Text.Json`, camelCase, indented.
- Verify snapshot test.
- ✅ Pause.

---

## Phase 1C — Scanning

### Task 6: `TemporalAttributeScanner`
- Reflection over a set of assemblies. Finds `[Workflow]`, `[WorkflowRun]`, `[WorkflowSignal]`, `[WorkflowQuery]`, `[WorkflowUpdate]`, `[Activity]`.
- Returns a typed scan result (no YAML knowledge here).
- Unit test against in-test fixture types.
- ✅ Pause.

### Task 7: `TemporalWorkerOptionsReader`
- Given an `IServiceCollection`, builds a provider, enumerates registered `TemporalWorkerOptions` instances (one per `AddHostedTemporalWorker(...)`), returns `{ namespace, taskQueue, workflowTypes, activityTypes }` per worker.
- Unit test that registers two workers and asserts both come back.
- **Implementer note:** Temporalio SDK property names may have moved; adapt while keeping shape.
- ✅ Pause.

### Task 8: `WorkerMappingBuilder`
- Joins scan result × worker options. Workflow/activity types in scan but not registered to any worker → `Diagnostic` (`WF001` / `ACT001`).
- Unit tests: happy path + orphan path.
- ✅ Pause.

### Task 9: `DocumentFactory`
- One `WorkflowApiDocument` per worker. Per-workflow `bindings.temporal: { namespace, taskQueue, workflowType }`.
- Unit test: SimpleWorker fixture → one document with correct binding values.
- ✅ Pause.

---

## Phase 1D — Delivery

> CLI first so a demo is possible even if endpoints don't land.

### Task 10: `--dump <directory>` CLI mode (`WorkflowApi.Temporal.Cli.DumpCommand`)
- Writes `{hostId}-{taskQueue}.workflowapi.yaml` per worker + `index.json` listing them with orphan diagnostics.
- Integration test against `SimpleWorker` fixture.
- ✅ Pause.

### Task 11: ASP.NET endpoints (`MapWorkflowApi(...)`)
- `GET /workflowapi/index.json` → list of `{ taskQueue, url }`.
- `GET /workflowapi/{taskQueue}.yaml` → that worker's document.
- Endpoints depend on `IReadOnlyList<EmittedDocument>` injected by the host — keeps AspNetCore Temporal-free.
- `TestHost` integration test.
- ✅ Pause.

---

## Phase 1E — Nexus + Polish

### Task 12: `NexusExtractor` (declarations only)
- `[NexusService]` types → `BridgeDef { kind: "nexus", operations: [...] }`.
- Caller-side call edges (which workflow uses which bridge) are explicitly Phase 2.
- Bridges attach to every per-worker document for now (global declarations).
- **Implementer note:** if the SDK version on hand doesn't ship Nexus attributes, file a Deviation Record and skip.
- ✅ Pause.

### Task 13: Architecture tests (`NetArchTest`)
- `WorkflowApi.Abstractions` must not reference `Temporalio`, `Scrutor`, or `Microsoft.AspNetCore`.
- `WorkflowApi.AspNetCore` must not reference `Temporalio` or `Scrutor`.
- Failing tests block boundary drift.
- ✅ Pause.

### Task 14: Fixture projects
- `SimpleWorker`, `MultiWorker`, `NexusService`, `ClassLibraryNoTemporal`. Each is the minimum that exercises one branch of the scanner. End-to-end test goes from fixture assembly → YAML.
- ✅ Pause.

### Task 15: Manual demo runbook
- `docs/hackweek/2026-06-17-temporal-dotnet-scanner-demo.md`: run `--dump` against `SimpleWorker`, drop the YAML into the visualiser, confirm it renders. Repeat for `MultiWorker`. Confirm `ClassLibraryNoTemporal` exits cleanly with empty output.
- Walk the runbook on a clean checkout before declaring it done.
- ✅ Pause.

---

## Closing checklist

- [ ] All 15 tasks green
- [ ] `dotnet test src/dotnet/WorkflowApi.sln` clean
- [ ] Architecture tests enforce boundaries
- [ ] Existing visualiser still renders bundled fixture (no regression)
- [ ] Demo runbook reproduces end-to-end
- [ ] Every spec deviation has a Deviation Record in `.squad/decisions/inbox/`
- [ ] Tooling ran zero git operations

## Explicitly out of scope (Phase 2 candidates)

- Caller-side call-edge detection (workflow → bridge / childWorkflow / activity references)
- Multi-document loading in the visualiser
- JSON Schema generation for workflow input/output types
- Catalog server / `publish` CLI
- Emission Option (c) (workflow-level binding shape) — long-term target per deviation record
- Runtime overlay decorations (live run status)
- Auth on `/workflowapi/*` endpoints

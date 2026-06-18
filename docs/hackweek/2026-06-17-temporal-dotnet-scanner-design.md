# Temporal .NET Scanner — Hackweek Phase 1 Design

**Date:** 2026-06-17
**Author:** Squad Coordinator (on behalf of Rebecca Powell)
**Status:** Draft for review
**Track:** Hackweek (NOT the formal v1 spec track — see deviation records)

## Goal

Build a generic .NET tool/package that ingests a real .NET solution implementing Temporal workflows and emits one or more WorkflowAPI YAML documents that the existing React Flow visualiser (`src/web/workflow-react-flow-visualizer-mvp/`) can render. The same solution should work for any Temporal .NET project that adopts the package, with no per-project customisation beyond a single registration line.

## Out of scope (explicitly)

- Source-only / un-built code analysis (Roslyn) — deferred
- Build-time MSBuild target — deferred
- Catalogue collation across services — deferred (per spec 18)
- Runtime overlays (metrics, p95, etc.) — deferred (per spec 16, post-v1)
- Caller-side call-edge detection (which workflow calls which activity / child workflow / Nexus operation) — Phase 2
- JSON Schema generation for workflow input/output types — Phase 2 (Phase 1 emits a `$ref`-style placeholder with the CLR type name only)
- UI changes — Phase 1 emits files only; visualiser stays single-document

## Approach selected

**Option B2 — in-process scanner using Scrutor**, running inside the target Temporal worker host. Selected over:
- B1 (external CLI with `MetadataLoadContext`): can't see DI / `IConfiguration`-derived bindings, can't reliably map workflows to workers
- A (Roslyn): heavier setup, still can't see runtime config
- C (MSBuild target): more intrusive than B2 for no extra value during hackweek

## Architecture

### Packages

```
WorkflowApi.Abstractions
   • POCOs only (WorkflowApiDocument, WorkflowDef, ActivityDef,
     SignalDef, QueryDef, UpdateDef, BridgeDef, BindingDef, Diagnostic)
   • Zero references to Temporal or Scrutor
   • Zero references to ASP.NET Core
        ▲                                          ▲
        │                                          │
WorkflowApi.AspNetCore                  WorkflowApi.Temporal
   • AddWorkflowApi()                      • ScanTemporalAssemblies()
   • /workflowapi/index.json                 — Scrutor scan for [Workflow] /
   • /workflowapi/{taskQueue}.yaml             [Activity] / [NexusService]
   • --dump <dir> mode                     • Reads TemporalWorkerOptions from DI
   • YAML/JSON serializers                   to determine per-worker mapping
   • Zero references to Temporal           • References Temporalio.* and Scrutor
```

**Boundary invariants (must be enforced by architecture tests):**
1. `WorkflowApi.Abstractions` has zero references to `Temporalio.*`, `Scrutor`, or ASP.NET Core.
2. `WorkflowApi.AspNetCore` has zero references to `Temporalio.*`.
3. `WorkflowApi.Temporal` is the only package that references `Temporalio.*`.

### Runtime flow

```
[Target Worker Program.cs]
    builder.Services
        .AddHostedTemporalWorker(...)
        .AddWorkflow<T>()
        .AddScopedActivities<T>();
    builder.Services
        .AddWorkflowApi()                ← new
        .ScanTemporalAssemblies();       ← new — Scrutor scan

[Startup, after builder.Build()]
    For each TemporalWorkerOptions instance in DI:
        worker.namespace = options.ClientOptions.Namespace
        worker.taskQueue = options.TaskQueue
        worker.workflows = options.Workflows ∩ scan results
        worker.activities = options.Activities ∩ scan results
    Orphans (in scan but not registered with any worker)
        → diagnostic WFAPI-ORPHAN-WORKFLOW / WFAPI-ORPHAN-ACTIVITY

[Document assembly]
    One WorkflowApiDocument PER worker (Option b — see deviation record).
    Document shape unchanged from current spec:
        workflowApi: 0.1.0   (see open question below)
        info:    { title, version, summary, ... }
        host:    { id, name, owner, environment, ... }
        bindings.temporal: { namespace, taskQueue, sdk: temporal-dotnet, ... }
        workflows: { ... only those hosted by this worker ... }
        activities: { ... only those hosted by this worker ... }
        bridges: { ... Nexus services exposed by workflows in this worker ... }
        diagnostics: [ ... ]

[Output]
    GET /workflowapi/index.json
        → { documents: [
            { taskQueue: "risk-enrichment", url: "/workflowapi/risk-enrichment.yaml" },
            { taskQueue: "audit-pipeline",  url: "/workflowapi/audit-pipeline.yaml"  }
          ] }
    GET /workflowapi/{taskQueue}.yaml
        → YAML for that single worker
    GET /workflowapi/{taskQueue}.json
        → same document as JSON
    dotnet run -- --dump <directory>
        → writes one .workflowapi.yaml per worker into the directory and exits 0
```

### Attribute mapping (Phase 1)

| Temporal .NET attribute / API          | WorkflowAPI element                                |
|----------------------------------------|----------------------------------------------------|
| `[Workflow]` on class                  | `workflows.<Name>`                                 |
| `[WorkflowRun]` on method              | `workflows.<Name>.run` (operationId, input, output) |
| `[WorkflowSignal]` on method           | `workflows.<Name>.signals.<name>`                  |
| `[WorkflowQuery]` on method            | `workflows.<Name>.queries.<name>`                  |
| `[WorkflowUpdate]` on method           | `workflows.<Name>.updates.<name>`                  |
| `[Activity]` on class or method        | `activities.<Name>` grouped under the host         |
| `[NexusService]` on interface          | `bridges.<Name>` with `kind: nexus`                |
| `[NexusOperation]` on method           | `bridges.<Name>.operations.<name>`                 |
| `TemporalWorkerOptions.Namespace`      | `bindings.temporal.namespace`                      |
| `TemporalWorkerOptions.TaskQueue`      | `bindings.temporal.taskQueue`                      |

Naming follows the existing spec — `bridge` for the generic cross-boundary concept, `nexus` as the binding kind. See `coordinator-naming-conventions-locked.md`.

### Phase 2 (not in this design)

- Caller-side call-edge detection (workflow → activity, workflow → child workflow, workflow → bridge) via IL inspection or a small Roslyn pass
- Full JSON Schema generation for workflow input/output types
- Migration toward Option (c) — workflow-level bindings — when the wider team aligns on the shape change

## Error handling

| Situation                                                | Behaviour                                                                  |
|----------------------------------------------------------|----------------------------------------------------------------------------|
| Assembly has no `[Workflow]` types                       | Log info `"No Temporal types found in <project>"`, no document entry      |
| `[Workflow]` with no `[WorkflowRun]` method              | Emit workflow with diagnostic `WFAPI-NO-RUN` (severity: error)            |
| Two `[WorkflowRun]` methods on one type                  | First wins, emit diagnostic `WFAPI-DUPLICATE-RUN` (severity: error)       |
| `[Workflow]` type found by scan but not registered       | Diagnostic `WFAPI-ORPHAN-WORKFLOW` (severity: warning) on each affected   |
|   with any worker via `.AddWorkflow<T>()`                | document; orphan workflow appears in no document                          |
| `[Activity]` found but not registered with any worker    | Diagnostic `WFAPI-ORPHAN-ACTIVITY` (severity: warning), same rule         |
| `TemporalWorkerOptions.TaskQueue` not configured         | Skip the worker, diagnostic `WFAPI-NO-TASKQUEUE` (severity: error)        |
| Workflow registered with multiple workers (shouldn't     | Diagnostic `WFAPI-WORKFLOW-MULTI-WORKER` (severity: warning), emit into   |
|   normally happen)                                       | each worker's document                                                    |
| File write failure on `--dump`                           | Exit code 2, error to stderr                                              |
| Empty result on `--dump`                                 | Write nothing, exit 0, log info                                           |

Diagnostic JSON envelope must match what Miller is freezing for spec 12 conformance (shared across scanner and validator).

## Testing

| Layer                  | Tool                    | Coverage                                                                  |
|------------------------|-------------------------|---------------------------------------------------------------------------|
| Unit — attribute mapping | xUnit                 | Fake types with each attribute combination → expected element             |
| Unit — DI inspection   | xUnit                   | Hand-built `ServiceCollection` with mock `TemporalWorkerOptions`          |
| Unit — YAML serializer | Verify.Xunit (snapshot) | Known `WorkflowApiDocument` → byte-equivalent YAML (deterministic)        |
| Architecture tests     | NetArchTest / similar   | Boundary invariants (no Temporal in Abstractions, no Temporal in AspNetCore) |
| Integration — single   | xUnit + WebApplicationFactory | Tiny host with 1 worker → GET `/workflowapi/{tq}.yaml` matches snapshot |
| Integration — multi    | xUnit + WebApplicationFactory | Tiny host with 2 workers → 2 files; workflow→worker mapping correct |
| Round-trip             | xUnit                   | Emitted YAML re-parses to same `WorkflowApiDocument`                      |

**Fixture projects** (in `test/fixtures/`):
- `SimpleWorker` — one worker, one workflow with signal + query + update, one activity class
- `MultiWorker` — two workers as in the architecture example
- `NexusService` — workflow exposing `[NexusService]` / `[NexusOperation]`
- `ClassLibraryNoTemporal` — no Temporal types (verifies the info log + no-output behaviour)

**Deterministic output rules** (per `docs/specs/12-conformance-and-validation.md`):
- Alphabetical key ordering in emitted YAML
- No timestamps in output
- Invariant culture for numeric formatting
- Stable type-name → ref resolution

**Manual demo path:**
1. `cd test/fixtures/SimpleWorker`
2. `dotnet run -- --dump ../out`
3. Copy `../out/<taskQueue>.workflowapi.yaml` into `src/web/workflow-react-flow-visualizer-mvp/src/data/`
4. Update the import in `App.tsx`
5. `npm run dev` → graph renders

## Open questions

### Q1 — `workflowApi` document version string

The existing UI YAML uses `workflowApi: 0.1.0`. Holden's final v1 plan pinned `workflowApi: 1.0.0`. The scanner has to pick one.

- If we emit `1.0.0`: matches the v1 plan; the existing UI YAML / TS fixture will need updating to match if loaded.
- If we emit `0.1.0`: matches what the visualiser TS model currently asserts; deviates from the v1 plan.

**Proposed:** emit `0.1.0` for hackweek to avoid forcing a UI fixture update, with a deviation record. Migrate to `1.0.0` when Naomi formalises the spec freeze.

### Q2 — Where do diagnostics surface?

The scanner produces diagnostics. They can live in the YAML (`diagnostics: []` at document root) and / or be logged to the host's `ILogger`. The existing visualiser already renders document-root diagnostics. Proposed: do both — diagnostics in the document AND logged at startup.

## Deviations from the formal v1 plan

This work is a hackweek track that starts ahead of Holden's M1 spec-freeze gate. Multiple decisions in this design diverge from the current v1 plan / spec set. They are captured as separate Deviation Records under `.squad/decisions/inbox/` and will be merged into `.squad/decisions.md` by Scribe:

- `coordinator-deviation-record-protocol.md` — the protocol itself (how we record divergence)
- `coordinator-naming-conventions-locked.md` — spec naming is authoritative
- `coordinator-deviation-multi-worker-emission.md` — one file per worker (Option b), long-term Option (c)
- `coordinator-deviation-hackweek-scanner-ahead-of-m1.md` — building scanner before M1 spec freeze
- `coordinator-deviation-document-version-0-1-0.md` — version string choice (pending Q1 resolution)

## Acceptance criteria for Phase 1

1. A reference fixture (`SimpleWorker`) with one worker, one workflow with signal + query + update, one activity class produces a single deterministic `<taskQueue>.workflowapi.yaml` that loads in the existing visualiser without modification (beyond pointing the import at the new file).
2. A reference fixture (`MultiWorker`) with two workers produces two YAML files, each containing only its own worker's workflows and activities, plus a valid `/workflowapi/index.json` listing both.
3. A reference fixture (`NexusService`) with a `[NexusService]` interface produces a `bridges.<Name>` entry with `kind: nexus` that renders as a `BridgeNode` in the visualiser.
4. A reference fixture (`ClassLibraryNoTemporal`) referenced by the host produces no document entry and a single info log line.
5. Architecture tests pass: no Temporal references in `Abstractions` or `AspNetCore`.
6. Snapshot tests pass: emitted YAML is byte-equivalent across runs.
7. No git commits performed by tooling — all repository writes are user-driven.

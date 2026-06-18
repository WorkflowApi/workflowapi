# Resume notes — 2026-06-17 → 2026-06-18

You stopped after Task 6 of the Temporal .NET scanner. Pick up at Task 7.

## TL;DR

```bash
# 1. Sanity check — build is currently clean
cd /Users/Robert.Harris/Documents/Code/HackWeek8/workflowapi
dotnet build src/dotnet/WorkflowApi.sln           # expect 0 warn / 0 err
dotnet test  src/dotnet/WorkflowApi.sln           # expect 34 passed (14 + 8 + 12)

# 2. Resume the squad with:
#    "Continue Task 7 from where we left off"
```

The squad's coordinator will pick up the plan from `~/.copilot/session-state/0b916d28-63d7-4987-9b3a-e65de25b5358/plan.md`.

---

## What's done (6 of 11 tasks)

Phase 1A foundation + Phase 1B output + Task 6 scanner.

```
src/dotnet/
├── Directory.Build.props              .NET 10, nullable on, warnings-as-errors
├── NuGet.config                       public-feed isolation
├── WorkflowApi.sln                    contains 6 projects
├── WorkflowApi.Abstractions/          POCO data model (13 records, zero deps)
├── WorkflowApi.Abstractions.Tests/    14 tests
├── WorkflowApi.AspNetCore/            YAML + JSON writers
├── WorkflowApi.AspNetCore.Tests/      8 tests (minimal/full/deterministic/omit-null)
├── WorkflowApi.Temporal/              TemporalAttributeScanner (Temporalio 1.15.0)
└── WorkflowApi.Temporal.Tests/        12 tests, incl. real-RiskWorker integration
```

The scanner has been validated against `src/temporal-local/worker/` (the real Temporal worker in this repo). It correctly discovers all 3 workflows and 12 activities including signal/query names, with strict naming-validation throwing on YAML-unsafe characters.

## What's next (5 tasks)

| # | Task | What it produces |
|---|------|------------------|
| **7** | `TemporalWorkerOptionsReader` | Reads a configured `IServiceCollection` → returns `RegisteredWorker` per `AddHostedTemporalWorker(...)` with task queue + namespace + workflow/activity types |
| 8 | `WorkerMappingBuilder` | Joins scanner output × DI options; emits orphan diagnostics (`WF001` / `ACT001`) |
| 9 | `DocumentFactory` | One `WorkflowApiDocument` per worker, with `bindings.temporal: { namespace, taskQueue }` |
| 10 | `--dump <dir>` CLI | Writes `{hostId}-{taskQueue}.workflowapi.yaml` per worker + `index.json` |
| 15 | Demo runbook | Markdown — drop YAML into visualizer, confirm rendering |

End-state demo: `dotnet run --dump ./out` against the RiskWorker → YAML files → drop into the React Flow visualiser at `src/web/workflow-react-flow-visualizer-mvp/` → it renders.

Tasks 11 (endpoints), 12 (Nexus), 13 (NetArchTest), 14 (synthetic fixtures) were dropped from hackweek scope. RiskWorker subsumed Task 14. See `.squad/decisions/inbox/coordinator-deviation-hackweek-scope-reduction.md`.

## Open decisions on Task 7

- **DI introspection over JSON config** (you confirmed yesterday). Amos will investigate `Temporalio.Extensions.Hosting` 1.15.0 to discover how `AddHostedTemporalWorker(taskQueue).AddWorkflow<T>().AddScopedActivities<T>()` registers into `IServiceCollection` — probably as named options keyed by task queue.
- **Fallback** if SDK doesn't expose enough: user-supplied JSON file mapping workflows → task queues. Don't use unless DI introspection genuinely can't work.

## Pending deviation records (in `.squad/decisions/inbox/`)

11 records waiting for Scribe merge into `decisions.md`. None blocking. Run "Scribe, please merge the inbox" when you want them consolidated.

Most important ones to read if you want context:
- `coordinator-deviation-record-protocol.md` — the format you mandated
- `coordinator-naming-conventions-locked.md` — naming rules the scanner now enforces
- `coordinator-deviation-hackweek-scope-reduction.md` — yesterday's scope cut, with rationale
- `coordinator-deviation-scanner-throws-on-unsafe-names.md` — the "no silent renames" rule applied at scan time
- `coordinator-tech-debt-task-6.md` — 7 conscious deferrals from reviews

## Files NOT to commit (decide intent first)

These were added during hackweek and you may or may not want them in git:
- `src/dotnet/NuGet.config` — necessary to isolate from machine-level private feeds
- `src/dotnet/Directory.Build.props`, `WorkflowApi.sln`, all 6 .NET projects under `src/dotnet/`
- `docs/hackweek/*.md` — design, plan, this resume note
- `.squad/decisions/inbox/*.md` — pending deviation records

I have not run any git commands. Your call.

## How to "wake the squad up"

Open a fresh Copilot session in this directory and say something like:

> "Continue the WorkflowAPI hackweek work — pick up at Task 7 (TemporalWorkerOptionsReader). Plan is in `docs/hackweek/2026-06-17-temporal-dotnet-scanner-plan.md` and session checkpoint is in `~/.copilot/session-state/`."

The squad's Coordinator (Holden) will route to Amos. If anything looks off, the most useful spelunking files are:

- `docs/hackweek/2026-06-17-temporal-dotnet-scanner-design.md` — locked design
- `docs/hackweek/2026-06-17-temporal-dotnet-scanner-plan.md` — task plan
- `src/temporal-local/worker/Program.cs` — the DI pattern Task 7 reads
- `src/dotnet/WorkflowApi.Temporal/Scanning/TemporalAttributeScanner.cs` — the upstream scanner Task 8 will join with Task 7's output

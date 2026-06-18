# Tasks: Workflow Node Execution Metrics

**Feature**: `004-workflow-node-metrics` | **Date**: 2026-06-18
**Input**: Design documents from `specs/004-workflow-node-metrics/`

**Prerequisites**: Feature 003 complete (Activity counters, Prometheus, Proxy, Visualizer all operational)

**Tests**: Not explicitly requested — test tasks omitted per spec. Manual validation via `quickstart.md`.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete-task dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in every task description

---

## Phase 1: Setup

**Purpose**: Confirm Feature 003 baseline is intact before adding incremental changes

- [X] T001 Verify `docker compose up --build -d` starts all Feature 003 services healthy (temporal, prometheus, metrics-proxy, visualizer) from `src/`

**Checkpoint**: Feature 003 baseline confirmed — ChildWorkflow/Subflow nodes show `–` (no data), Activity nodes show counts. Ready to implement.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No new shared infrastructure is required — Feature 003 already provides Prometheus scraping, OpenTelemetry, Docker Compose, and the metrics endpoint.
The foundational prerequisite is **User Story 3 (Worker counter)** which must be implemented first so that Phase 3 and Phase 4 can be tested end-to-end.

> ⚠️ Proceed to Phase 3 (US3) immediately — it is the critical path for all downstream stories.

---

## Phase 3: User Story 3 – Worker exportiert Workflow-Execution-Metriken (Priority: P1) 🔑 Foundation

**Goal**: The Temporal worker emits a new Prometheus counter `temporal_workflow_task_completed_total{workflow_type="..."}` incremented on every terminal workflow outcome (Completed, Failed, Cancelled) via `try/finally`. All three workflow types are covered.

**Independent Test**: `curl -s http://localhost:9090/metrics | grep temporal_workflow_task_completed` shows one counter entry per workflow type with value ≥ 1 after the simulator has run for ~30 s. (See `quickstart.md` Step 1.)

### Implementation for User Story 3

- [X] T002 [US3] Add `WorkflowExecutions` counter (`temporal_workflow_task_completed`) and `RecordWorkflowExecution(string workflowType)` method to `src/temporal-local/worker/Metrics/WorkerMetrics.cs` — mirror the existing `ActivityExecutions` counter pattern with label `workflow_type`
- [X] T003 [P] [US3] Wrap `RiskEnrichmentWorkflow.RunAsync` body in `try/finally` and call `WorkerMetrics.RecordWorkflowExecution(nameof(RiskEnrichmentWorkflow))` in the `finally` block in `src/temporal-local/worker/Workflows/RiskEnrichmentWorkflow.cs`
- [X] T004 [P] [US3] Wrap `CalculateRiskWorkflow.RunAsync` body in `try/finally` and call `WorkerMetrics.RecordWorkflowExecution(nameof(CalculateRiskWorkflow))` in the `finally` block in `src/temporal-local/worker/Workflows/CalculateRiskWorkflow.cs`
- [X] T005 [P] [US3] Wrap `ExternalChecksWorkflow.RunAsync` body in `try/finally` and call `WorkerMetrics.RecordWorkflowExecution(nameof(ExternalChecksWorkflow))` in the `finally` block in `src/temporal-local/worker/Workflows/ExternalChecksWorkflow.cs`

**Checkpoint**: `temporal_workflow_task_completed_total` appears in `/metrics` for all three workflow types. US3 acceptance criteria met. Unblocks US1 and US2 for end-to-end validation.

---

## Phase 4: User Story 1 – ChildWorkflow Nodes zeigen Ausführungszähler (Priority: P1) 🎯 MVP

**Goal**: ChildWorkflow trigger-nodes (`CalculateRiskWorkflow`, `ExternalChecksWorkflow`) show a numeric execution-count badge sourced from a new `workflowCounts` field in the metrics response. The entire data path — Proxy → MetricsClient → Hook → Context → Adapter → Node — is wired end-to-end.

**Independent Test**: Open `http://localhost:5173` with all services running. ChildWorkflow nodes (`Calculate risk`, `External checks`) display a green badge with a number ≥ 0. Changing the time-range dropdown updates the badge value. Activity nodes continue to show their existing counts unchanged. (See `quickstart.md` Steps 2–3.)

### Implementation for User Story 1

#### Metrics Proxy (two tasks, T007 depends on T006)

- [X] T006 [P] [US1] Add `buildWorkflowCountsQuery(range: TimeRange): string` and `queryWorkflowCounts(prometheusUrl: string, range: TimeRange): Promise<Record<string, number>>` to `src/web/metrics-proxy/src/services/prometheus-client.ts` — mirror the existing `buildActivityCountsQuery`/`queryActivityCounts` pattern, using `workflow_type` label and metric `temporal_workflow_task_completed_total`
- [X] T007 [US1] Extend the route handler in `src/web/metrics-proxy/src/routes/activity-counts.ts` to run both Prometheus queries in parallel via `Promise.all([queryActivityCounts(...), queryWorkflowCounts(...)])` and include `workflowCounts` in the JSON response body (always present; empty object `{}` on Prometheus error) — per contract `contracts/metrics-proxy-api.md`

#### Visualizer — Types & State (all parallel, no inter-dependencies)

- [X] T008 [P] [US1] Add `workflowCounts: Record<string, number>` field to the `MetricsResponse` interface in `src/web/workflow-react-flow-visualizer-mvp/src/services/metrics-client.ts` (default to `{}` when absent in the API response)
- [X] T009 [P] [US1] Add `workflowCounts: Record<string, number>` to the `ActivityMetricsState` interface and set the default value to `workflowCounts: {}` in `src/web/workflow-react-flow-visualizer-mvp/src/hooks/useActivityMetrics.ts` — propagate the `workflowCounts` field from the fetched `MetricsResponse` into state
- [X] T010 [P] [US1] Update the default context value to include `workflowCounts: {}` in `src/web/workflow-react-flow-visualizer-mvp/src/contexts/MetricsContext.ts` so the context type always carries `workflowCounts`

#### Visualizer — Adapter (parallel with T006–T010)

- [X] T011 [P] [US1] Add `workflowRef?: string` to the `WorkflowNodeData` interface and, in the `childWorkflow`-node code path of `src/web/workflow-react-flow-visualizer-mvp/src/adapters/graph-to-reactflow.ts`, extract `node.raw.workflowRef` (string guard) and pass it as `workflowRef` in the node data object — `activityType` remains unchanged for Activity nodes only

#### Visualizer — ChildWorkflowNode Renderer (depends on T009, T010, T011)

- [X] T012 [US1] Update `src/web/workflow-react-flow-visualizer-mvp/src/components/nodes/ChildWorkflowNode.tsx` to read the execution count from `workflowCounts[nodeData.workflowRef]` (via `useMetrics()`) instead of the Activity `counts` map — when `workflowRef` is undefined or the key is absent in `workflowCounts`, pass `undefined` to `ExecutionCountBadge` so it renders `–`

**Checkpoint**: ChildWorkflow nodes show numeric badges end-to-end. Activity nodes unchanged. US1 acceptance criteria met. MVP deliverable complete — can stop and demo here.

---

## Phase 5: User Story 2 – Subflow Nodes zeigen Ausführungszähler (Priority: P2)

**Goal**: Subflow-Group-Nodes use the same `workflowCounts` lookup path. Since Subflow nodes have no `workflowRef` (no direct Temporal counter pendant), they correctly show `–` (not `0`) — which satisfies US2 acceptance scenario 2. Should a future Subflow gain a `workflowRef`, the badge will display its count automatically.

**Independent Test**: Open `http://localhost:5173`. Subflow nodes show `–` badge (unavailable) — not a blank, not `0`, and not a broken badge. This is the expected and correct behaviour per spec. (See `quickstart.md` Step 3.)

**Dependency**: Requires Phase 4 (US1) to be complete, because `workflowCounts` must already be available in `MetricsContext`.

### Implementation for User Story 2

- [X] T013 [US2] Update `src/web/workflow-react-flow-visualizer-mvp/src/components/nodes/SubflowNode.tsx` to read from `workflowCounts` via `useMetrics()` using the same `workflowRef`-keyed lookup as `ChildWorkflowNode.tsx`; since Subflow nodes carry no `workflowRef`, the lookup resolves to `undefined` and `ExecutionCountBadge` renders `–`

**Checkpoint**: Subflow nodes show `–` (not broken). US2 acceptance criteria met.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, final validation, and regression check across all stories.

- [X] T014 [P] Add XML documentation comments to the new `WorkflowExecutions` static field and `RecordWorkflowExecution()` method in `src/temporal-local/worker/Metrics/WorkerMetrics.cs` per Constitution Principle I (public APIs must have XML docs)
- [X] T015 [P] Run full end-to-end validation from `specs/004-workflow-node-metrics/quickstart.md` (Steps 1–5): worker counter, proxy response `workflowCounts`, visualizer ChildWorkflow badges, graceful degradation on Prometheus stop, counter resilience on worker restart
- [X] T016 [P] Verify Activity node regression: confirm `counts` field is still populated correctly in proxy response, and `ActivityNode.tsx` badges are unchanged after all Phase 3–5 edits

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
  └─→ Phase 3 (US3 – Worker) ← critical path
        └─→ Phase 4 (US1 – ChildWorkflow) ← E2E test requires US3
              └─→ Phase 5 (US2 – Subflow) ← requires workflowCounts in context (US1)
                    └─→ Phase 6 (Polish)
```

> **Note**: US1 and US2 frontend code changes (T006–T013) can be written in parallel with US3 (T002–T005). However, end-to-end testing of US1/US2 requires US3 to be deployed and emitting counters.

### User Story Dependencies

| Story | Depends On | Can Start |
|-------|-----------|-----------|
| US3 (P1 – Worker) | Phase 1 complete | Immediately after T001 |
| US1 (P1 – ChildWorkflow) | Phase 1 complete (code); US3 deployed (E2E test) | Code: parallel with US3; Full test: after US3 |
| US2 (P2 – Subflow) | US1 complete (workflowCounts in context) | After T012 |

### Within Phase 4 (US1) — Parallel Groups

**Group A** (launch together, all different files):
```
T006  prometheus-client.ts  — buildWorkflowCountsQuery + queryWorkflowCounts
T008  metrics-client.ts     — workflowCounts in MetricsResponse
T009  useActivityMetrics.ts — workflowCounts in ActivityMetricsState
T010  MetricsContext.ts     — workflowCounts in default state
T011  graph-to-reactflow.ts — workflowRef in WorkflowNodeData + adapter
```

**Sequential after Group A**:
```
T007 (after T006) — activity-counts.ts route, uses queryWorkflowCounts
T012 (after T009, T010, T011) — ChildWorkflowNode.tsx renderer
```

### Within Phase 3 (US3) — Parallel Groups

**Sequential base**:
```
T002 — WorkerMetrics.cs (defines RecordWorkflowExecution)
```

**Parallel after T002** (three different workflow files):
```
T003  RiskEnrichmentWorkflow.cs
T004  CalculateRiskWorkflow.cs
T005  ExternalChecksWorkflow.cs
```

---

## Parallel Execution Examples

### Phase 3 (US3) — After T002

```
Task: "Add try/finally + RecordWorkflowExecution in RiskEnrichmentWorkflow.cs"   → T003
Task: "Add try/finally + RecordWorkflowExecution in CalculateRiskWorkflow.cs"    → T004
Task: "Add try/finally + RecordWorkflowExecution in ExternalChecksWorkflow.cs"   → T005
```

### Phase 4 (US1) — Group A (all parallel)

```
Task: "Add buildWorkflowCountsQuery + queryWorkflowCounts in prometheus-client.ts"  → T006
Task: "Add workflowCounts to MetricsResponse in metrics-client.ts"                  → T008
Task: "Add workflowCounts to ActivityMetricsState in useActivityMetrics.ts"         → T009
Task: "Update MetricsContext default state with workflowCounts: {} "                → T010
Task: "Add workflowRef to WorkflowNodeData + adapter in graph-to-reactflow.ts"      → T011
```

---

## Implementation Strategy

### MVP First (User Story 3 + User Story 1 Only)

1. Complete **Phase 1**: Verify baseline
2. Complete **Phase 3** (US3 – Worker): `RecordWorkflowExecution` in all 3 workflows → counters appear in Prometheus
3. Complete **Phase 4** (US1 – ChildWorkflow): Proxy extension + Visualizer wiring → badges on ChildWorkflow nodes
4. **STOP and VALIDATE**: Run `quickstart.md` Steps 1–3, confirm ChildWorkflow badges show counts
5. **Demo / Deploy MVP** — full P1 story delivered

### Incremental Delivery

1. **Setup + US3** → Worker emits counters → testable at `/metrics` endpoint
2. **+ US1** → ChildWorkflow badges live in Visualizer → MVP deliverable
3. **+ US2** → Subflow nodes correctly show `–` → P2 story complete
4. **Polish** → XML docs, regression check, E2E quickstart validation

### Parallel Team Strategy

With two developers after Phase 1:

- **Developer A**: US3 (T002–T005) — .NET Worker changes
- **Developer B**: US1 Group A (T006, T008–T011) — TypeScript Proxy + Visualizer types/adapter

After US3 and Group A complete:
- **Developer A**: T014 (XML docs), T015 (quickstart validation)
- **Developer B**: T007, T012 → T013 → T016

---

## Notes

- `[P]` tasks modify different files with no dependency on incomplete tasks — safe to run in parallel
- `[Story]` labels map each task to its user story for traceability
- `workflowRef` key must match exactly the C# class name (e.g. `"CalculateRiskWorkflow"`) — no namespace prefix
- SubflowNode intentionally shows `–` (no `workflowRef`) — this is correct behaviour per spec
- Activity nodes (`ActivityNode.tsx`) must NOT be modified — `counts` remains their exclusive source
- Commit after each logical task group; stop at any checkpoint to validate independently

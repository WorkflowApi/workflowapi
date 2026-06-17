# Tasks: Activity Execution Counts

**Input**: Design documents from `specs/003-activity-execution-counts/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Not explicitly requested in spec — test tasks omitted.

**Organization**: Tasks grouped by user story for independent implementation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize new projects, add dependencies, create directory structure

- [X] T001 Create metrics-proxy project scaffold with package.json and tsconfig.json in src/web/metrics-proxy/
- [X] T002 [P] Create Prometheus configuration directory and prometheus.yml in src/prometheus/prometheus.yml
- [X] T003 [P] Add OpenTelemetry NuGet packages to Worker in src/temporal-local/worker/RiskWorker.csproj (OpenTelemetry.Extensions.Hosting, OpenTelemetry.Exporter.Prometheus.HttpListener)
- [X] T004 [P] Create Dockerfile for metrics-proxy service in src/web/metrics-proxy/Dockerfile

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Complete data pipeline (Worker → Prometheus → Proxy) that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete (no metrics = no badges)

- [X] T005 Implement WorkerMetrics static class with Meter and Counter in src/temporal-local/worker/Metrics/WorkerMetrics.cs
- [X] T006 Configure OpenTelemetry MeterProvider with PrometheusHttpListener in src/temporal-local/worker/Program.cs
- [X] T007 Instrument all Activity classes to increment counter with activity_type tag in src/temporal-local/worker/Activities/*.cs
- [X] T008 [P] Implement Prometheus client service (PromQL query builder) in src/web/metrics-proxy/src/services/prometheus-client.ts
- [X] T009 Implement activity-counts route handler (GET /api/activity-counts) in src/web/metrics-proxy/src/routes/activity-counts.ts
- [X] T010 Implement Express server entry point with CORS and health endpoint in src/web/metrics-proxy/src/index.ts
- [X] T011 Add prometheus, metrics-proxy services and worker metrics port to src/docker-compose.yml
- [X] T012 Update src/.env.example with METRICS_PROXY_PORT, PROMETHEUS_PORT, METRICS_PORT variables

**Checkpoint**: `docker compose up --build` starts full pipeline. `curl http://localhost:4000/api/activity-counts?range=1h` returns JSON with activity counts.

---

## Phase 3: User Story 1 — Ausführungszähler auf Activity Nodes (Priority: P1) 🎯 MVP

**Goal**: Jede Activity Node zeigt ein kompaktes Badge mit der Ausführungszahl im aktuellen Zeitraum (default: 1d)

**Independent Test**: Visualizer im Browser öffnen → jede Activity Node hat ein numerisches Badge ≥ 0

### Implementation for User Story 1

- [X] T013 [P] [US1] Create compact number formatter utility in src/web/workflow-react-flow-visualizer-mvp/src/utils/format-number.ts
- [X] T014 [P] [US1] Create metrics HTTP client service in src/web/workflow-react-flow-visualizer-mvp/src/services/metrics-client.ts
- [X] T015 [US1] Create useActivityMetrics polling hook (2s interval, AbortController) in src/web/workflow-react-flow-visualizer-mvp/src/hooks/useActivityMetrics.ts
- [X] T016 [US1] Create ExecutionCountBadge component with loading/value/unavailable states in src/web/workflow-react-flow-visualizer-mvp/src/components/nodes/ExecutionCountBadge.tsx
- [X] T017 [US1] Integrate ExecutionCountBadge into ActivityNode component in src/web/workflow-react-flow-visualizer-mvp/src/components/nodes/ActivityNode.tsx
- [X] T018 [US1] Wire useActivityMetrics hook into App.tsx or WorkflowVisualizer.tsx and pass counts to ActivityNode via node data

**Checkpoint**: Activity Nodes show execution count badges with values from Prometheus. Auto-refresh every 2 seconds visible.

---

## Phase 4: User Story 2 — Zeitraumauswahl (Priority: P2)

**Goal**: Benutzer kann zwischen Letzte Stunde / Letzter Tag / Letzte Woche / Letzter Monat wählen; Zähler aktualisieren sich

**Independent Test**: Zeitraum von "Letzter Tag" auf "Letzte Stunde" wechseln → Badge-Werte ändern sich

### Implementation for User Story 2

- [X] T019 [US2] Create TimeRangeSelector component (segmented control/dropdown) in src/web/workflow-react-flow-visualizer-mvp/src/components/TimeRangeSelector.tsx
- [X] T020 [US2] Add time range state management to App.tsx and pass selected range to useActivityMetrics hook
- [X] T021 [US2] Update useActivityMetrics hook to accept range parameter and re-fetch on range change in src/web/workflow-react-flow-visualizer-mvp/src/hooks/useActivityMetrics.ts
- [X] T022 [US2] Show loading state in badges during time-range transition in src/web/workflow-react-flow-visualizer-mvp/src/components/nodes/ExecutionCountBadge.tsx

**Checkpoint**: Time range selector visible, switching range updates all badges within 2 seconds. Loading state visible during transition.

---

## Phase 5: User Story 3 — Graceful Degradation (Priority: P3)

**Goal**: Bei nicht erreichbarem Prometheus/Proxy bleibt der Graph navigierbar; Badges zeigen "–" statt Fehler

**Independent Test**: `docker compose stop prometheus` → Badges zeigen "–", Graph bleibt funktional

### Implementation for User Story 3

- [X] T023 [US3] Implement error handling and timeout (5s) in useActivityMetrics hook with unavailable state fallback in src/web/workflow-react-flow-visualizer-mvp/src/hooks/useActivityMetrics.ts
- [X] T024 [US3] Implement "unavailable" badge state rendering ("–" indicator) in src/web/workflow-react-flow-visualizer-mvp/src/components/nodes/ExecutionCountBadge.tsx
- [X] T025 [P] [US3] Add 503 response handling in metrics-proxy when Prometheus is unreachable in src/web/metrics-proxy/src/routes/activity-counts.ts
- [X] T026 [US3] Implement recovery behavior: resume polling and restore values when service becomes available again

**Checkpoint**: Stopping prometheus/metrics-proxy shows "–" badges without breaking the graph. Restarting services restores values within 2 poll cycles.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Integration, documentation, final validation

- [X] T027 [P] Add tooltip with full number on badge hover in src/web/workflow-react-flow-visualizer-mvp/src/components/nodes/ExecutionCountBadge.tsx
- [X] T028 [P] Expose worker metrics port (9090) in worker Dockerfile in src/temporal-local/worker/Dockerfile
- [X] T029 [P] Configure Prometheus retention (45d) for 30-day query support in src/docker-compose.yml prometheus service command
- [X] T030 Update src/temporal-local/README.md with new services documentation (Prometheus, Metrics Proxy)
- [X] T031 Run full quickstart.md validation (all 6 scenarios) from specs/003-activity-execution-counts/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) — delivers MVP
- **User Story 2 (Phase 4)**: Depends on US1 (Phase 3) — extends badge with time range
- **User Story 3 (Phase 5)**: Depends on US1 (Phase 3) — adds error handling to existing badges
- **Polish (Phase 6)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational — can start as soon as pipeline works
- **US2 (P2)**: Depends on US1 (needs existing badge + hook to extend with range parameter)
- **US3 (P3)**: Depends on US1 (needs existing badge to add error states). Can run in parallel with US2.

### Within Phases — Parallel Opportunities

**Phase 1** (all [P] can run in parallel):
```
T001 ─┐
T002 ─┤ all parallel
T003 ─┤
T004 ─┘
```

**Phase 2** (Worker → Proxy → Docker):
```
T005 → T006 → T007  (Worker metrics: sequential)
T008 → T009 → T010  (Proxy: sequential)
T011 + T012          (Docker Compose: parallel with above once services ready)
```

**Phase 3** (US1):
```
T013 ─┐ parallel (utilities)
T014 ─┘
T015 → T016 → T017 → T018  (hook → badge → integrate)
```

**Phase 4 + 5** (US2 + US3 can run in parallel after US1):
```
Phase 4: T019 → T020 → T021 → T022
Phase 5: T023 → T024, T025 [P], T026
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (scaffold projects)
2. Complete Phase 2: Foundational (full data pipeline working)
3. Complete Phase 3: User Story 1 (badges visible with counts)
4. **STOP and VALIDATE**: `docker compose up --build` → badges show counts on all Activity Nodes
5. Deploy/demo if ready — delivers immediate value

### Incremental Delivery

1. Setup + Foundational → Pipeline working (curl returns counts) ✓
2. Add US1 → Badges visible on all Activity Nodes (MVP!) ✓
3. Add US2 → Time range selector allows comparison ✓
4. Add US3 → Works gracefully without Temporal/Prometheus ✓
5. Polish → Tooltips, documentation, full validation ✓

### Suggested MVP Scope

**MVP = Phase 1 + Phase 2 + Phase 3** (Tasks T001–T018)
- 18 tasks for a fully functional badge display with auto-refresh
- Delivers immediate visual value to developers

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- Activity names in WorkflowAPI YAML `activityRef` match Prometheus `activity_type` labels exactly
- Worker metrics port (9090) must not conflict with Prometheus host port — use different host mapping
- The existing `WorkloadSimulator` generates workflow executions every 5s — ideal for testing badge updates
- Commit after each task or logical group

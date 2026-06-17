# Tasks: Local Temporal Environment

**Input**: Design documents from `/specs/001-local-temporal-environment/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Not explicitly requested. Manual validation via Temporal UI + CLI per quickstart.md.

**Organization**: Tasks grouped by user story. US1 and US2 are co-dependent (US1 needs compilable worker code from US2); implement sequentially Phase 3 → Phase 4.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Project Scaffolding)

**Purpose**: Create the project structure, .NET project file, and static configuration files.

- [X] T001 Create directory structure for src/temporal-local/ with subdirectories: worker/Workflows/, worker/Activities/, worker/Models/, worker/Simulation/, dynamicconfig/
- [X] T002 Create .NET 10 project file with Temporalio 1.15.0, Temporalio.Extensions.Hosting 1.15.0, and Microsoft.Extensions.Hosting 10.0.0 packages in src/temporal-local/worker/RiskWorker.csproj (enable nullable, TreatWarningsAsErrors, GenerateDocumentationFile, RootNamespace: WorkflowApi.Temporal.RiskWorker)

---

## Phase 2: Foundational (Shared Data Models)

**Purpose**: All C# record types shared across workflows. MUST complete before workflow/activity implementation.

**⚠️ CRITICAL**: Activities and workflows depend on these model types to compile.

- [X] T003 [P] Create RiskEnrichmentRequest record with CompanyId, Country, CompanyName?, Duns?, Reason? properties and XML docs in src/temporal-local/worker/Models/RiskEnrichmentRequest.cs
- [X] T004 [P] Create RiskEnrichmentResult record with CompanyId, RiskClass, Status, DocumentId? properties and XML docs in src/temporal-local/worker/Models/RiskEnrichmentResult.cs
- [X] T005 [P] Create RiskEnrichmentStatus record with State?, CurrentStep?, UpdatedAt? properties and XML docs in src/temporal-local/worker/Models/RiskEnrichmentStatus.cs
- [X] T006 [P] Create CalculateRiskRequest record with CompanyId?, Duns? properties and XML docs in src/temporal-local/worker/Models/CalculateRiskRequest.cs
- [X] T007 [P] Create RiskScoreResult record with Score (double), RiskClass? properties and XML docs in src/temporal-local/worker/Models/RiskScoreResult.cs
- [X] T008 [P] Create ExternalChecksRequest record with CompanyId?, Country? properties and XML docs in src/temporal-local/worker/Models/ExternalChecksRequest.cs
- [X] T009 [P] Create ExternalChecksResult record with SanctionsHit, PepHit, AdverseMediaHit bool properties and XML docs in src/temporal-local/worker/Models/ExternalChecksResult.cs

**Checkpoint**: All shared model types compile. Activity and workflow implementation can begin.

---

## Phase 3: User Story 1 - Start Temporal environment with one command (Priority: P1) 🎯 MVP

**Goal**: A developer runs `docker compose up` in `src/temporal-local/` and gets a fully functional Temporal cluster (server, UI, PostgreSQL) with the `B2B.RiskService` namespace auto-created and the `risk-enrichment` task queue visible.

**Independent Test**: Run `docker compose up`, verify all 4 services reach healthy state within 60 seconds, open `http://localhost:8233`, confirm `B2B.RiskService` namespace and `risk-enrichment` task queue with registered workers.

**Note**: US1 can only be fully validated after US2 (Phase 4) is complete, since the worker container must compile the .NET project.

### Implementation for User Story 1

- [X] T010 [US1] Create docker-compose.yml with postgresql (postgres:16), temporal (temporalio/auto-setup:1.31.1), temporal-ui (temporalio/ui:2.51.0), and risk-worker services per contracts/docker-compose.md — include health checks, depends_on conditions, temporal-local bridge network, and all environment variables in src/temporal-local/docker-compose.yml
- [X] T011 [P] [US1] Create .env file with default values for TEMPORAL_VERSION, TEMPORAL_UI_VERSION, POSTGRES_VERSION, TEMPORAL_NAMESPACE, TEMPORAL_TASK_QUEUE, SIMULATOR_ENABLED, SIMULATOR_INTERVAL_SECONDS, ACTIVITY_FAILURE_RATE, ACTIVITY_MIN_DELAY_MS, ACTIVITY_MAX_DELAY_MS in src/temporal-local/.env
- [X] T012 [P] [US1] Create Temporal dynamic config with limit.maxIDLength=255 and system.forceSearchAttributesCacheRefreshOnRead=true in src/temporal-local/dynamicconfig/development-sql.yaml
- [X] T013 [P] [US1] Create multi-stage Dockerfile using mcr.microsoft.com/dotnet/sdk:10.0 (build) and mcr.microsoft.com/dotnet/runtime:10.0 (runtime), restore → publish → copy pattern, USER $APP_UID, ENTRYPOINT dotnet RiskWorker.dll in src/temporal-local/worker/Dockerfile

**Checkpoint**: Docker infrastructure files ready. Worker compilation requires Phase 4 completion.

---

## Phase 4: User Story 2 - Worker executes Risk Enrichment workflow end-to-end (Priority: P1)

**Goal**: When a `RiskEnrichmentWorkflow` is started with valid input, the worker executes all activities through the full topology (identify → enrich → calculate-risk → document-pdf → publish), supports `GetStatus` query and `Cancel` signal, and completes with a valid `RiskEnrichmentResult`.

**Independent Test**: `docker compose exec temporal temporal workflow start --namespace B2B.RiskService --task-queue risk-enrichment --type RiskEnrichmentWorkflow --input '{"companyId":"test-001","country":"DE"}'` → workflow completes within 30 seconds with valid result containing companyId, riskClass, and status.

### Activities for User Story 2

> All activities use `[Activity]` decorator, implement configurable Task.Delay (read ACTIVITY_MIN/MAX_DELAY_MS from env), and return mock data. Activities marked with failure simulation throw `ApplicationFailureException` with ~10% probability (read ACTIVITY_FAILURE_RATE from env) before executing. Include retry policy: MaximumAttempts=5, InitialInterval=500ms, BackoffCoefficient=2.0, MaximumInterval=10s.

- [X] T014 [P] [US2] Create IdentifyCompanyActivity with [Activity] decorator — resolves company identifiers, returns enriched RiskEnrichmentRequest with generated DUNS if missing, delay 500-1000ms, no failure simulation in src/temporal-local/worker/Activities/IdentifyCompanyActivity.cs
- [X] T015 [P] [US2] Create EnrichDnbActivity with [Activity] decorator — simulates D&B bridge, returns DnbEnrichmentResult (define record in-file: LegalName, CreditLimit decimal), delay 1000-2000ms, ~10% failure simulation in src/temporal-local/worker/Activities/EnrichDnbActivity.cs
- [X] T016 [P] [US2] Create ScoreCompanyRiskActivity with [Activity] decorator — runs scoring engine on normalised signals, returns double score 0-100, delay 800-1500ms, no failure simulation in src/temporal-local/worker/Activities/ScoreCompanyRiskActivity.cs
- [X] T017 [P] [US2] Create AggregateRiskScoreActivity with [Activity] decorator — maps score to RiskScoreResult (low<30, medium 30-70, high>70), delay 300-600ms, no failure simulation in src/temporal-local/worker/Activities/AggregateRiskScoreActivity.cs
- [X] T018 [P] [US2] Create PublishRiskResultActivity with [Activity] decorator — publishes final result, returns bool true, delay 300-800ms, no failure simulation in src/temporal-local/worker/Activities/PublishRiskResultActivity.cs
- [X] T019 [P] [US2] Create GeneratePdfActivity with [Activity] decorator — simulates PDF generation bridge, returns random GUID documentId string, delay 1500-2000ms, ~10% failure simulation in src/temporal-local/worker/Activities/GeneratePdfActivity.cs
- [X] T020 [P] [US2] Create FetchPaymentHistoryActivity with [Activity] decorator — returns PaymentHistory record (define in-file: PaymentCount int, TotalAmount decimal), delay 500-1000ms, no failure simulation in src/temporal-local/worker/Activities/FetchPaymentHistoryActivity.cs
- [X] T021 [P] [US2] Create FetchCreditLimitActivity with [Activity] decorator — returns random decimal 10000-1000000, delay 500-1000ms, no failure simulation in src/temporal-local/worker/Activities/FetchCreditLimitActivity.cs
- [X] T022 [P] [US2] Create NormaliseRiskSignalsActivity with [Activity] decorator — returns NormalisedSignals record (define in-file: normalised 0-1 values), delay 200-500ms, no failure simulation in src/temporal-local/worker/Activities/NormaliseRiskSignalsActivity.cs
- [X] T023 [P] [US2] Create SanctionsCheckActivity with [Activity] decorator — returns bool (~5% true hit rate), delay 1000-1500ms, ~10% failure simulation in src/temporal-local/worker/Activities/SanctionsCheckActivity.cs
- [X] T024 [P] [US2] Create PoliticallyExposedPersonCheckActivity with [Activity] decorator — returns bool (~3% true hit rate), delay 1000-1500ms, ~10% failure simulation in src/temporal-local/worker/Activities/PoliticallyExposedPersonCheckActivity.cs
- [X] T025 [P] [US2] Create AdverseMediaCheckActivity with [Activity] decorator — returns bool (~8% true hit rate), delay 800-1200ms, ~10% failure simulation in src/temporal-local/worker/Activities/AdverseMediaCheckActivity.cs

### Workflows for User Story 2

> All workflows use `[Workflow]` decorator with `[WorkflowRun]` entry point. Use `Workflow.ExecuteActivityAsync` for activities and `Workflow.ExecuteChildWorkflowAsync` for child workflows. Random in workflows must use `Workflow.Random` for deterministic replay.

- [X] T026 [US2] Create ExternalChecksWorkflow with [Workflow] decorator — topology: start → SanctionsCheck → PEPCheck → AdverseMedia → end, accepts ExternalChecksRequest, returns ExternalChecksResult aggregating all three check booleans in src/temporal-local/worker/Workflows/ExternalChecksWorkflow.cs
- [X] T027 [US2] Create CalculateRiskWorkflow with [Workflow] decorator — topology: start → FetchPaymentHistory + FetchCreditLimit (parallel) → NormaliseRiskSignals → ScoreCompanyRisk → ExecuteChildWorkflow<ExternalChecksWorkflow> → AggregateRiskScore → end, accepts CalculateRiskRequest, returns RiskScoreResult in src/temporal-local/worker/Workflows/CalculateRiskWorkflow.cs
- [X] T028 [US2] Create RiskEnrichmentWorkflow with [Workflow] decorator — topology: start → IdentifyCompany → EnrichDnb → ExecuteChildWorkflow<CalculateRiskWorkflow> → GeneratePdf → PublishRiskResult → end, accepts RiskEnrichmentRequest, returns RiskEnrichmentResult. Include [WorkflowQuery] GetStatus returning RiskEnrichmentStatus (currentStep + updatedAt), [WorkflowSignal] Cancel for graceful termination with CancelSignal record (define in-file), state tracking per topology node in src/temporal-local/worker/Workflows/RiskEnrichmentWorkflow.cs

### Host Entry Point

- [X] T029 [US2] Create Program.cs using Generic Host pattern — configure TemporalClient (read TEMPORAL_ADDRESS, TEMPORAL_NAMESPACE from env), register hosted Temporal worker on TEMPORAL_TASK_QUEUE with all 3 workflow types and all 12 activity classes, configure logging, add placeholder for WorkloadSimulator BackgroundService (disabled until Phase 5) in src/temporal-local/worker/Program.cs

**Checkpoint**: `docker compose up --build` starts all services. Workflows execute end-to-end with simulated delays and ~10% failure retries visible in Temporal UI at localhost:8233.

---

## Phase 5: User Story 3 - Continuous workload simulation (Priority: P2)

**Goal**: A `BackgroundService` automatically starts new Risk Enrichment workflows every 5 seconds (configurable via `SIMULATOR_INTERVAL_SECONDS`) with randomised but realistic input data, producing continuous activity in the Temporal cluster.

**Independent Test**: After 2 minutes of `docker compose up`, Temporal UI shows ≥20 workflow executions with varied company IDs, countries, and risk class outcomes (low, medium, high).

### Implementation for User Story 3

- [X] T030 [P] [US3] Create MockDataGenerator static class — generates random company IDs (GUIDs), random countries from pool ["DE","US","GB","FR","NL","CH"], random company names from pool of 20 realistic names, returns valid RiskEnrichmentRequest instances in src/temporal-local/worker/Simulation/MockDataGenerator.cs
- [X] T031 [US3] Create WorkloadSimulator as BackgroundService — reads SIMULATOR_ENABLED and SIMULATOR_INTERVAL_SECONDS from configuration, starts RiskEnrichmentWorkflow via TemporalClient every interval using MockDataGenerator for input, includes structured logging for each workflow start, handles graceful shutdown via CancellationToken in src/temporal-local/worker/Simulation/WorkloadSimulator.cs
- [X] T032 [US3] Register WorkloadSimulator as hosted BackgroundService in Program.cs — add `builder.Services.AddHostedService<WorkloadSimulator>()`, ensure it starts after worker is connected, reads SIMULATOR_ENABLED to conditionally activate in src/temporal-local/worker/Program.cs

**Checkpoint**: Environment sustains continuous load. Temporal UI shows ongoing workflow executions with varied inputs and outcomes.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and final validation across all user stories.

- [X] T033 [P] Create README.md with quick start instructions (prerequisites, docker compose up, accessing UI at localhost:8233, manual workflow start command, teardown) and environment variable reference in src/temporal-local/README.md
- [X] T034 Run quickstart.md validation scenarios — verify all 8 scenarios pass: environment health, Temporal UI, workflow execution, GetStatus query, workload simulator, retry behaviour, cancel signal, worker recovery

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (.csproj must exist for model namespace)
- **US1 (Phase 3)**: Depends on Phase 1 (directory structure for docker-compose.yml placement)
- **US2 (Phase 4)**: Depends on Phase 2 (models must compile) — **ALSO**: US1's `docker compose up` test requires US2 complete (worker must compile)
- **US3 (Phase 5)**: Depends on Phase 4 (Program.cs and working workflows)
- **Polish (Phase 6)**: Depends on all previous phases complete

### User Story Dependencies

- **US1 (P1)**: Infrastructure files are independent, but full validation requires US2 (worker must compile for `docker compose build`)
- **US2 (P1)**: Depends on Foundational (Phase 2) models. Once complete, BOTH US1 and US2 are testable.
- **US3 (P2)**: Depends on US2 (needs working workflows to simulate). Can start after Phase 4.

### Within User Story 2 (Activity → Workflow → Host chain)

- All 12 activities (T014-T025): Independent of each other → **all parallel**
- ExternalChecksWorkflow (T026): Depends on T023, T024, T025 (references activity classes)
- CalculateRiskWorkflow (T027): Depends on T020-T022, T026 (uses ExternalChecks as child)
- RiskEnrichmentWorkflow (T028): Depends on T014-T019, T027 (uses CalculateRisk as child)
- Program.cs (T029): Depends on T026-T028 (registers all workflow types)

### Parallel Opportunities

- **Phase 2**: All 7 model records (T003-T009) — different files, no dependencies
- **Phase 3**: T011, T012, T013 can parallel with each other (T010 is the anchor)
- **Phase 4**: All 12 activities (T014-T025) — different files, no cross-dependencies
- **Phase 5**: T030 (MockDataGenerator) can parallel with any remaining Phase 4 work
- **Cross-phase**: Phase 3 (Docker files) can be written in parallel with Phase 2 (models) since Docker files don't reference .NET code directly

---

## Parallel Example: User Story 2

```bash
# Launch all 12 activities in parallel (different files, no dependencies):
Task: "Create IdentifyCompanyActivity in worker/Activities/IdentifyCompanyActivity.cs"
Task: "Create EnrichDnbActivity in worker/Activities/EnrichDnbActivity.cs"
Task: "Create ScoreCompanyRiskActivity in worker/Activities/ScoreCompanyRiskActivity.cs"
Task: "Create AggregateRiskScoreActivity in worker/Activities/AggregateRiskScoreActivity.cs"
Task: "Create PublishRiskResultActivity in worker/Activities/PublishRiskResultActivity.cs"
Task: "Create GeneratePdfActivity in worker/Activities/GeneratePdfActivity.cs"
Task: "Create FetchPaymentHistoryActivity in worker/Activities/FetchPaymentHistoryActivity.cs"
Task: "Create FetchCreditLimitActivity in worker/Activities/FetchCreditLimitActivity.cs"
Task: "Create NormaliseRiskSignalsActivity in worker/Activities/NormaliseRiskSignalsActivity.cs"
Task: "Create SanctionsCheckActivity in worker/Activities/SanctionsCheckActivity.cs"
Task: "Create PoliticallyExposedPersonCheckActivity in worker/Activities/PoliticallyExposedPersonCheckActivity.cs"
Task: "Create AdverseMediaCheckActivity in worker/Activities/AdverseMediaCheckActivity.cs"

# Then sequentially (dependency chain):
Task: "Create ExternalChecksWorkflow" (needs check activities)
Task: "Create CalculateRiskWorkflow" (needs ExternalChecksWorkflow)
Task: "Create RiskEnrichmentWorkflow" (needs CalculateRiskWorkflow)
Task: "Create Program.cs" (needs all workflows)
```

---

## Parallel Example: Foundational Models

```bash
# Launch all 7 model records in parallel:
Task: "Create RiskEnrichmentRequest.cs"
Task: "Create RiskEnrichmentResult.cs"
Task: "Create RiskEnrichmentStatus.cs"
Task: "Create CalculateRiskRequest.cs"
Task: "Create RiskScoreResult.cs"
Task: "Create ExternalChecksRequest.cs"
Task: "Create ExternalChecksResult.cs"
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Complete Phase 1: Setup (2 tasks)
2. Complete Phase 2: Foundational models (7 tasks, parallel)
3. Complete Phase 3: US1 Docker infrastructure (4 tasks, mostly parallel)
4. Complete Phase 4: US2 Worker implementation (16 tasks, 12 parallel + 4 sequential)
5. **STOP and VALIDATE**: `docker compose up --build` → all healthy → start workflow → completes
6. Both US1 and US2 pass at this point

### Incremental Delivery

1. Phase 1 + 2 + 3 + 4 → US1 + US2 validated (MVP! Full working Temporal environment)
2. Add Phase 5 (US3) → Continuous simulation validated
3. Add Phase 6 → Documentation and full quickstart validation

### Single Developer Strategy (Recommended)

Execute phases sequentially in order. Within each phase, parallelise where marked [P]:
1. Setup → Foundational (models in parallel) → US1 (Docker files in parallel) → US2 (activities in parallel, then workflows sequentially) → US3 → Polish

---

## Notes

- [P] tasks = different files, no dependencies on each other
- [US1/US2/US3] labels map tasks to user stories for traceability
- Constitution compliance: nullable enabled, XML docs on public APIs, TreatWarningsAsErrors, Temporal code isolated in src/temporal-local/
- Activities are intentionally stubs (Task.Delay + mock data) — this IS the full implementation per spec
- All randomness in activities uses `Random.Shared`; workflow randomness uses `Workflow.Random`
- Commit after each phase completion for clean rollback points

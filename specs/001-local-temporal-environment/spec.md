# Feature Specification: Local Temporal Environment

**Feature Branch**: `001-local-temporal-environment`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "Den Workflow-Visualizer um Live-Daten erweitern – erste Iteration: lokales Temporal-Environment aufsetzen. Docker-Compose mit Temporal-Cluster, Worker für den Beispiel-Workflow risk-enrichment.workflowapi.yaml und simulierter Workload."

## Clarifications

### Session 2026-06-17

- Q: Welche Implementierungssprache soll für den Worker verwendet werden? → A: .NET (C# mit Temporal .NET SDK)
- Q: Sollen Activities zufällige Fehler simulieren, um Retry-Verhalten sichtbar zu machen? → A: Ja, ~10% zufällige Fehlerrate bei ausgewählten Activities
- Q: Wo soll das docker-compose Setup im Repository leben? → A: `src/temporal-local/`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Start Temporal environment with one command (Priority: P1)

A developer clones the repository, runs a single `docker compose up` command, and gets a fully functional local Temporal cluster (server, UI, database) with pre-registered workers executing the Risk Enrichment workflow. No manual configuration or external dependencies beyond Docker are required.

**Why this priority**: This is the foundational capability — without a running Temporal environment, no live-data features can be developed or tested.

**Independent Test**: Can be fully tested by running `docker compose up` and verifying all services reach healthy state within 60 seconds, then opening Temporal UI at `http://localhost:8233` and confirming the `B2B.RiskService` namespace exists.

**Acceptance Scenarios**:

1. **Given** the repository is freshly cloned and Docker is installed, **When** the developer runs `docker compose up` in the temporal-dev directory, **Then** Temporal server, Temporal UI, and the worker container all report healthy status within 60 seconds.
2. **Given** all containers are running, **When** the developer opens the Temporal Web UI, **Then** the `B2B.RiskService` namespace is visible and the `risk-enrichment` task queue shows registered workers.
3. **Given** all containers are running, **When** the developer runs `docker compose down`, **Then** all containers and networks are cleanly removed.

---

### User Story 2 - Worker executes Risk Enrichment workflow end-to-end (Priority: P1)

The worker container implements all activities and child workflows defined in `examples/risk-enrichment.workflowapi.yaml` as simulated stubs (with realistic delays and mock data). When a workflow is started, it progresses through all topology nodes (identify-company → enrich-dnb → calculate-risk → document-pdf → publish-result) and completes successfully.

**Why this priority**: The worker is the core runtime component that produces the live data the visualizer will eventually consume. Without working activities, there is nothing to visualize.

**Independent Test**: Start a workflow via Temporal CLI (`temporal workflow start`) and verify it completes with a valid `RiskEnrichmentResult` containing all required fields.

**Acceptance Scenarios**:

1. **Given** the Temporal environment is running, **When** a `RiskEnrichmentWorkflow` is started with valid input (`companyId`, `country`), **Then** the workflow completes within 30 seconds with status `completed` and returns a valid `RiskEnrichmentResult`.
2. **Given** a workflow is running, **When** the `GetStatus` query is issued, **Then** it returns the current step name and a valid timestamp.
3. **Given** a workflow is running, **When** the `CalculateRiskWorkflow` child workflow is reached, **Then** it executes its own topology (fetch-signals → score-engine → external-checks → aggregate) as a nested child workflow before returning.
4. **Given** a workflow is running, **When** the `ExternalChecksWorkflow` child is reached, **Then** sanctions-check, PEP-check, and adverse-media activities all execute with simulated results.

---

### User Story 3 - Continuous workload simulation (Priority: P2)

A workload simulator automatically starts new Risk Enrichment workflows at a configurable rate, producing continuous activity in the Temporal cluster. This provides a steady stream of live data for future visualizer development.

**Why this priority**: While individual workflow execution (P1) is sufficient for basic testing, continuous workload is needed to realistically test live-data streaming scenarios and observe concurrent workflow behaviour.

**Independent Test**: Observe the Temporal UI after 2 minutes of uptime: there should be multiple workflow executions in various states (running, completed), demonstrating ongoing load.

**Acceptance Scenarios**:

1. **Given** the environment is running with the simulator enabled, **When** 2 minutes have elapsed, **Then** at least 10 workflow executions are visible in the Temporal UI.
2. **Given** the simulator is running, **When** workflows are started, **Then** each workflow uses randomised but valid input data (different company IDs, countries).
3. **Given** the simulator is running, **When** a workflow completes, **Then** it produces different risk classes (low, medium, high) based on simulated scoring logic.
4. **Given** the environment starts with default configuration, **When** no environment variables are overridden, **Then** the simulator starts 1 workflow every 5 seconds by default.

---

### Edge Cases

- What happens when a simulated activity fails randomly (e.g., network timeout)? The workflow should retry per Temporal's retry policy.
- What happens when the worker container restarts mid-workflow? Temporal's durability guarantees should resume the workflow from its last checkpoint.
- What happens when the `Cancel` signal is sent? The workflow should terminate gracefully and set status to `cancelled`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a `docker-compose.yml` in `src/temporal-local/` that starts Temporal Server, Temporal UI, a PostgreSQL database, and worker containers with a single `docker compose up` command.
- **FR-002**: System MUST include a worker that registers all workflow types defined in `risk-enrichment.workflowapi.yaml`: `RiskEnrichmentWorkflow`, `CalculateRiskWorkflow`, and `ExternalChecksWorkflow`.
- **FR-003**: System MUST implement all activities referenced in the workflow spec as simulated stubs with configurable delays (default: 500ms–2000ms per activity).
- **FR-004**: System MUST implement all bridges (`Dnb.EnrichCompany`, `DocumentService.GeneratePdf`) as simulated activities returning mock data.
- **FR-004a**: Selected activities (bridges and external checks) MUST randomly fail with ~10% probability to trigger Temporal retry behaviour, making retries visible in the Temporal UI.
- **FR-005**: System MUST support the `GetStatus` query returning the current workflow step and timestamp.
- **FR-006**: System MUST support the `Cancel` signal for graceful workflow termination.
- **FR-007**: System MUST include a workload simulator that starts workflows at a configurable interval (default: every 5 seconds).
- **FR-008**: System MUST use the Temporal namespace `B2B.RiskService` and task queue `risk-enrichment` as defined in the workflow spec bindings.
- **FR-009**: System MUST generate randomised but realistic mock data for each workflow execution (varying company IDs, countries, and risk outcomes).
- **FR-010**: System MUST expose the Temporal Web UI on `localhost:8233` for inspection and debugging.
- **FR-011**: All containers MUST include health checks and restart policies for resilience.
- **FR-012**: System MUST NOT require any external credentials, API keys, or internet access beyond pulling Docker images.

### Key Entities

- **Temporal Server**: The core workflow orchestration engine managing workflow state, timers, and dispatch.
- **Worker**: A container hosting workflow and activity implementations that polls the `risk-enrichment` task queue.
- **Workload Simulator**: A process that periodically starts new workflow executions with randomised inputs.
- **Workflow Execution**: A running instance of `RiskEnrichmentWorkflow` progressing through its topology nodes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can go from `git clone` to a running Temporal environment in under 5 minutes (excluding Docker image pull time).
- **SC-002**: The system sustains 1 workflow start per 5 seconds without errors or resource exhaustion for at least 1 hour.
- **SC-003**: 100% of started workflows complete successfully under normal conditions (no simulated failures).
- **SC-004**: The Temporal Web UI shows real-time workflow state including pending activities, child workflow hierarchy, and completed executions.
- **SC-005**: Worker recovery after container restart completes within 10 seconds with no workflow data loss.

## Assumptions

- Developers have Docker and Docker Compose v2 installed locally.
- The worker implementation uses .NET (C#) with the Temporal .NET SDK, consistent with the project's .NET 10 LTS target.
- Activity simulations use `Task.Delay`-based delays rather than real external service calls.
- The PostgreSQL database is ephemeral (data does not persist across `docker compose down`).
- This environment is for local development only; no production hardening is required.
- The visualizer will consume Temporal data in a future iteration — this spec only sets up the data source.

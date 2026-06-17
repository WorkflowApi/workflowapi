# Implementation Plan: Local Temporal Environment

**Branch**: `001-local-temporal-environment` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-local-temporal-environment/spec.md`

## Summary

Set up a local Temporal development environment via Docker Compose that runs the Temporal server cluster, a .NET worker implementing the Risk Enrichment workflow (with simulated activities and ~10% random failure rate), and a workload simulator starting 1 workflow every 5 seconds. The environment runs with a single `docker compose up` command and requires no external credentials.

## Technical Context

**Language/Version**: C# / .NET 10 LTS

**Primary Dependencies**: Temporalio 1.15.0 (NuGet), Temporalio.Extensions.Hosting 1.15.0, Docker Compose v2

**Storage**: PostgreSQL 16 (ephemeral, Temporal server persistence only)

**Testing**: Manual validation via Temporal UI + `temporal workflow start` CLI; unit tests via `dotnet test`

**Target Platform**: Linux containers (Docker), local development on macOS/Linux/Windows

**Project Type**: Docker Compose service stack + .NET worker application

**Performance Goals**: Sustain 1 workflow start per 5 seconds for 1 hour without errors; all services healthy within 60 seconds

**Constraints**: No external credentials, no internet access beyond Docker image pulls, ephemeral data (no persistence across `docker compose down`)

**Scale/Scope**: Single developer local environment; 3 workflow types, ~12 activity stubs, 1 workload simulator

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Nullable reference types enabled | ✅ PASS | Worker project will enable `<Nullable>enable</Nullable>` |
| I. XML docs on public APIs | ✅ PASS | All public workflow/activity classes will have XML docs |
| I. No compiler warnings | ✅ PASS | `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in .csproj |
| I. Separated packages | ✅ PASS | Temporal-specific code lives in `src/temporal-local/`, not in core abstractions |
| I. Temporal not in core | ✅ PASS | This is a standalone Temporal binding/runtime package |
| I. Deterministic output | ⚠️ N/A | Simulated activities use randomness intentionally (only in activities, not workflow logic) |
| II. Tests cover public API | ✅ PASS | Unit tests for activities, integration tests via docker compose |
| II. Deterministic tests | ✅ PASS | Seeded random for test scenarios; no external service deps |
| III. No credentials in code | ✅ PASS | FR-012 explicitly requires no external credentials |
| IV. Performance budget | ✅ PASS | Sustain 1 wf/5s for 1 hour is within Temporal's capacity |
| Development Workflow: YAGNI | ✅ PASS | Minimal stubs with realistic delays; no over-engineering |

**Gate Result**: ✅ ALL GATES PASS — No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/001-local-temporal-environment/
├── plan.md              # This file
├── research.md          # Phase 0: Technology research findings
├── data-model.md        # Phase 1: Worker project structure and data model
├── quickstart.md        # Phase 1: Validation guide
├── contracts/           # Phase 1: Docker Compose and environment contracts
│   ├── docker-compose.md
│   └── environment-variables.md
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/temporal-local/
├── docker-compose.yml           # Full Temporal stack + worker + simulator
├── .env                         # Default environment variable values
├── scripts/
│   ├── setup-postgres.sh        # Schema setup for Temporal DB
│   └── create-namespace.sh      # Creates B2B.RiskService namespace
├── dynamicconfig/
│   └── development-sql.yaml     # Temporal dynamic config
└── worker/
    ├── RiskWorker.csproj        # .NET 10 worker project
    ├── Dockerfile               # Multi-stage build
    ├── Program.cs               # Host entry point (worker + simulator)
    ├── Workflows/
    │   ├── RiskEnrichmentWorkflow.cs
    │   ├── CalculateRiskWorkflow.cs
    │   └── ExternalChecksWorkflow.cs
    ├── Activities/
    │   ├── IdentifyCompanyActivity.cs
    │   ├── EnrichDnbActivity.cs
    │   ├── ScoreCompanyRiskActivity.cs
    │   ├── AggregateRiskScoreActivity.cs
    │   ├── PublishRiskResultActivity.cs
    │   ├── GeneratePdfActivity.cs
    │   ├── FetchPaymentHistoryActivity.cs
    │   ├── FetchCreditLimitActivity.cs
    │   ├── NormaliseRiskSignalsActivity.cs
    │   ├── SanctionsCheckActivity.cs
    │   ├── PoliticallyExposedPersonCheckActivity.cs
    │   └── AdverseMediaCheckActivity.cs
    ├── Models/
    │   ├── RiskEnrichmentRequest.cs
    │   ├── RiskEnrichmentResult.cs
    │   ├── RiskEnrichmentStatus.cs
    │   ├── CalculateRiskRequest.cs
    │   ├── RiskScoreResult.cs
    │   ├── ExternalChecksRequest.cs
    │   └── ExternalChecksResult.cs
    └── Simulation/
        ├── WorkloadSimulator.cs
        └── MockDataGenerator.cs
```

**Structure Decision**: Single .NET project (`RiskWorker`) in `src/temporal-local/worker/` containing all workflows, activities, and the simulator. Docker Compose orchestrates the full stack. This follows the constitution's separation principle — Temporal-specific runtime code is isolated from core WorkflowAPI abstractions.

## Complexity Tracking

> No violations detected. All gates pass without justification needed.

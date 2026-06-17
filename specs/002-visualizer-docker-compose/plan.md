# Implementation Plan: Visualizer Docker Compose Integration

**Branch**: `002-visualizer-docker-compose` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-visualizer-docker-compose/spec.md`

## Summary

Move `src/temporal-local/docker-compose.yml` to `src/docker-compose.yml` and add a new `workflow-visualizer` service for the Vite/React app at `src/web/workflow-react-flow-visualizer-mvp`. The visualizer is packaged as a Docker multi-stage build (`node:22-alpine` build → `nginx:1.27-alpine` serve) that delivers a fully static production bundle. A single `docker compose up --build` from `src/` starts all five services: PostgreSQL, Temporal Server, Temporal UI, Risk Worker, and the Workflow Visualizer. The visualizer port defaults to `3000` and is configurable via a `.env` file.

## Technical Context

**Language/Version**: YAML (Docker Compose v2), TypeScript 5.5 / React 18 (Vite 6 frontend — already written, not modified), Dockerfile multi-stage syntax

**Primary Dependencies**: Docker Engine 24+, Docker Compose v2; `node:22-alpine` (build stage), `nginx:1.27-alpine` (serve stage); existing services: `temporalio/auto-setup:1.29.2`, `temporalio/ui:2.51.0`, `postgres:16`, .NET 10 RiskWorker

**Storage**: N/A — local development only; no persistent state added for the visualizer service

**Testing**: Manual acceptance testing via browser and `docker compose ps`/`curl`; no automated unit/integration tests for infrastructure config files

**Target Platform**: Developer workstation (macOS, Linux); Docker Engine 24+ required; no CI pipeline target for this feature

**Project Type**: Infrastructure / DevOps configuration — Docker Compose orchestration + Dockerfile authoring

**Performance Goals**: All five services healthy within 60 seconds of `docker compose up --build`; Visualizer HTTP response <500ms for static asset delivery (nginx default is well within this)

**Constraints**: Visualizer port must not collide with Temporal UI (8233) or Temporal gRPC (7233); default port 3000; configurable via `VISUALIZER_PORT` in `.env`; no Vite Dev Server in container (production build only); nginx serves `dist/` only — no `proxy_pass`; `.env` must NOT be committed (`.env.example` pattern)

**Scale/Scope**: Single developer local environment; five Docker services; one new Dockerfile; one updated Compose file; one new `.env.example`; one updated README

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Code Quality** — nullable refs, XML docs, no warnings, project layering | ✅ N/A | No .NET source files are added or modified. Existing worker code is unchanged. |
| **II. Testing Standards** — unit, integration, conformance, snapshot tests | ✅ PASS | Feature is pure infrastructure config; no public API surface. Acceptance is via manual smoke test documented in `quickstart.md`. No automated test regression possible for Compose files. |
| **III. UX Consistency** — accessible UI, error messages, graceful fallbacks | ✅ PASS | The React app itself is unchanged. nginx serves static files; no new user-facing surfaces are introduced. |
| **IV. Performance** — UI load <2s, Catalog p95 <500ms | ✅ PASS | nginx serves pre-built static assets; well within any latency budget. |
| **Quality Gates** — no secrets in code/config | ✅ PASS | `.env` is gitignored (existing pattern); only `.env.example` is committed. No credentials in Dockerfile or Compose file beyond already-present Temporal/DB credentials (unchanged). |
| **Development Workflow** — YAGNI, prefer simplest solution | ✅ PASS | Multi-stage Dockerfile is the standard minimal pattern for Vite + nginx. No unnecessary abstractions introduced. |

**Post-design re-check**: ✅ All gates pass. No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/002-visualizer-docker-compose/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── service-interfaces.md
│   └── env-variables.md
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── docker-compose.yml                          ← MOVED from src/temporal-local/docker-compose.yml
├── .env.example                                ← NEW: canonical list of all configurable variables
├── temporal-local/
│   ├── README.md                               ← UPDATED: new start path (src/)
│   ├── dynamicconfig/
│   │   └── development-sql.yaml               (unchanged)
│   └── worker/
│       ├── Dockerfile                          (unchanged)
│       ├── .dockerignore                       (unchanged)
│       └── *.cs / *.csproj                     (unchanged)
└── web/
    └── workflow-react-flow-visualizer-mvp/
        ├── Dockerfile                          ← NEW: multi-stage (node:22-alpine → nginx:1.27-alpine)
        ├── package.json                        (unchanged)
        └── src/                               (unchanged)
```

**Structure Decision**: Infrastructure-only change. No new application packages or layers. All file moves/additions are co-located with their relevant component. The Compose file lives at `src/` level to be the single orchestration root for all services under `src/`.

## Complexity Tracking

> No constitution violations. No entry required.

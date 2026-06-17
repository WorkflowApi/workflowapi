# Implementation Plan: Activity Execution Counts

**Branch**: `003-activity-execution-counts` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-activity-execution-counts/spec.md`

## Summary

Erweitert den Workflow-Visualizer um Echtzeit-Ausführungszähler auf Activity Nodes. Der .NET Worker exportiert Prometheus-Metriken (OpenTelemetry), ein Prometheus-Container aggregiert diese, ein Node.js/Express-Proxy stellt PromQL-Ergebnisse als JSON-API bereit, und das React-Frontend zeigt kompakte Badges mit 2-Sekunden-Auto-Refresh und wählbarem Zeitraum (Stunde/Tag/Woche/Monat) an.

## Technical Context

**Language/Version**: TypeScript 5.x (Frontend + Proxy), .NET 10 (Worker-Erweiterung)

**Primary Dependencies**:
- Frontend: React 18, @xyflow/react 12, Vite 6
- Proxy: Node.js 22, Express
- Worker: Temporalio 1.15.0, OpenTelemetry.Exporter.Prometheus
- Infrastructure: Prometheus (prom/prometheus), Docker Compose

**Storage**: Prometheus (time-series metrics store)

**Testing**: Vite/Vitest (Frontend), Jest/Vitest (Proxy), dotnet test (Worker)

**Target Platform**: Local Docker Compose development stack (Linux containers)

**Project Type**: Multi-service web application (Frontend + Proxy + Worker + Infrastructure)

**Performance Goals**: Zähler-Update innerhalb 2s, Initial-Load <3s, Proxy-Latenz <500ms p95

**Constraints**: Auto-Refresh alle 2 Sekunden; graceful degradation bei Prometheus/Worker-Ausfall; Browser greift nie direkt auf Prometheus/Temporal zu

**Scale/Scope**: Lokales Dev-Setup, ~12 Activity Nodes, 1 Worker-Instanz, 1 Prometheus-Container

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Code Quality | ✅ PASS | Nullable enabled (.NET), TypeScript strict (Frontend/Proxy), clear package separation |
| II. Testing Standards | ✅ PASS | Unit tests for Proxy, Frontend components, Worker metrics export. Snapshot tests for badge rendering. |
| III. UX Consistency | ✅ PASS | Badge gracefully degrades when Prometheus unavailable; Tooltip for full number; no direct Temporal access from browser |
| IV. Performance | ✅ PASS | 2s polling interval, <3s initial load, <500ms proxy latency |
| Quality Gates | ✅ PASS | No secrets in specs/code; Temporal-specific logic stays in Worker, not in core |
| Development Workflow | ✅ PASS | Runtime-specific logic (OTEL export) in Worker package only; Frontend remains runtime-neutral |

**Gate Result: PASS** — No violations. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/003-activity-execution-counts/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── metrics-proxy-api.md
│   └── prometheus-config.md
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── docker-compose.yml                          # Add prometheus + metrics-proxy services
├── .env.example                                # Add METRICS_PROXY_PORT, PROMETHEUS_PORT
├── prometheus/
│   └── prometheus.yml                          # NEW: Prometheus scrape config
├── web/
│   ├── workflow-react-flow-visualizer-mvp/     # MODIFY: Add badge, polling, time range
│   │   └── src/
│   │       ├── components/
│   │       │   ├── nodes/ActivityNode.tsx       # MODIFY: Add execution count badge
│   │       │   └── TimeRangeSelector.tsx        # NEW: Time range dropdown
│   │       ├── hooks/
│   │       │   └── useActivityMetrics.ts        # NEW: Polling hook for metrics
│   │       ├── services/
│   │       │   └── metrics-client.ts            # NEW: HTTP client for proxy API
│   │       └── utils/
│   │           └── format-number.ts             # NEW: Compact number formatting
│   └── metrics-proxy/                           # NEW: Node.js/Express proxy service
│       ├── package.json
│       ├── tsconfig.json
│       ├── Dockerfile
│       └── src/
│           ├── index.ts                         # Express server entry point
│           ├── routes/
│           │   └── activity-counts.ts           # GET /api/activity-counts?range=1h
│           └── services/
│               └── prometheus-client.ts         # PromQL query builder
└── temporal-local/
    └── worker/
        ├── Program.cs                           # MODIFY: Add OTEL/Prometheus exporter
        └── RiskWorker.csproj                    # MODIFY: Add OTEL packages
```

**Structure Decision**: Extends the existing multi-service Docker Compose structure. New services (metrics-proxy, prometheus) co-locate with existing services under `src/`. The metrics-proxy lives under `src/web/` since it's a web-tier service serving the frontend.

## Complexity Tracking

> No constitution violations. No complexity justification needed.

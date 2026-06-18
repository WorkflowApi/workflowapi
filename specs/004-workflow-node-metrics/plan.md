# Implementation Plan: Workflow Node Execution Metrics

**Branch**: `004-workflow-node-metrics` | **Date**: 2026-06-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-workflow-node-metrics/spec.md`

## Summary

Erweitert Feature 003 um Ausführungszähler auf ChildWorkflow-Nodes. Der .NET Worker bekommt einen neuen OpenTelemetry-Counter `temporal_workflow_task_completed{workflow_type}`, der bei jedem terminalen Workflow-Ausgang (Completed/Failed/Cancelled) via `try/finally` inkrementiert wird. Der Metrics Proxy liefert zusätzlich `workflowCounts` im API-Response (rückwärtskompatibel, Parallel-Queries). Der Visualizer-Adapter überträgt `workflowRef` in die Node-Daten; `ChildWorkflowNode` liest seinen Zähler aus `workflowCounts[workflowRef]`.

## Technical Context

**Language/Version**: TypeScript 5.x (Frontend + Proxy), .NET 10 (Worker)

**Primary Dependencies**:
- Frontend: React 18, @xyflow/react 12, Vite 6
- Proxy: Node.js 22, Express, fetch (built-in)
- Worker: Temporalio, OpenTelemetry (`System.Diagnostics.Metrics`)
- Infrastructure: Prometheus (bestehend), Docker Compose (bestehend)

**Storage**: Prometheus (bestehend, kein neuer Container)

**Testing**: Vitest (Frontend/Proxy), dotnet test (Worker)

**Target Platform**: Local Docker Compose development stack

**Project Type**: Inkrementelles Feature auf bestehender Multi-Service-Architektur (Feature 003)

**Performance Goals**: Gleich wie Feature 003 — Zähler-Update innerhalb 2 s, Proxy-Latenz <500 ms p95

**Constraints**: Kein neuer Port, kein neuer Container; rückwärtskompatible API-Erweiterung; kein direkter Temporal/Prometheus-Zugriff vom Browser

**Scale/Scope**: Lokales Dev-Setup, 3 Workflow-Typen, ~12 Activity Nodes, 1 Worker-Instanz

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Code Quality | ✅ PASS | Nullable enabled (.NET), TypeScript strict; klare Erweiterung bestehender Klassen; kein Cross-Layer-Leak |
| II. Testing Standards | ✅ PASS | Unit-Tests für neuen Counter (Worker), neue Proxy-Query (Proxy), Context-Erweiterung (Frontend) |
| III. UX Consistency | ✅ PASS | Badge-Verhalten (Loading/Unavailable) geerbt von Feature 003; kein neuer Port/keine neuen Credentials |
| IV. Performance | ✅ PASS | Beide Prometheus-Queries parallel (Promise.all); gleiche Polling-Frequenz wie Feature 003 |
| Quality Gates | ✅ PASS | Keine Secrets, rückwärtskompatibel, Temporal-Logik bleibt im Worker |
| Development Workflow | ✅ PASS | Kleine, fokussierte Änderungen über 3 Layer; klare Acceptance Criteria |

**Gate Result: PASS** — Keine Verletzungen. Weiter mit Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/004-workflow-node-metrics/
├── plan.md              # Dieses Dokument
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── metrics-proxy-api.md   # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (betroffene Pfade)

```text
src/temporal-local/worker/
├── Metrics/
│   └── WorkerMetrics.cs                    # +WorkflowExecutions counter, +RecordWorkflowExecution()
└── Workflows/
    ├── RiskEnrichmentWorkflow.cs            # +RecordWorkflowExecution in alle terminalen Pfade (try/finally)
    ├── CalculateRiskWorkflow.cs             # +RecordWorkflowExecution (try/finally)
    └── ExternalChecksWorkflow.cs            # +RecordWorkflowExecution (try/finally)

src/web/metrics-proxy/src/
├── services/
│   └── prometheus-client.ts                # +buildWorkflowCountsQuery(), +queryWorkflowCounts()
└── routes/
    └── activity-counts.ts                  # +workflowCounts im Response-Body (Promise.all)

src/web/workflow-react-flow-visualizer-mvp/src/
├── services/
│   └── metrics-client.ts                   # +workflowCounts in MetricsResponse
├── hooks/
│   └── useActivityMetrics.ts               # +workflowCounts in ActivityMetricsState + default
├── contexts/
│   └── MetricsContext.ts                   # +workflowCounts im Default-State
├── adapters/
│   └── graph-to-reactflow.ts               # +workflowRef in WorkflowNodeData; childWorkflow-Node-Pfad
└── components/nodes/
    └── ChildWorkflowNode.tsx               # Liest workflowCounts[workflowRef] statt counts[...]
```

**Structure Decision**: Inkrementelle Erweiterung bestehender Dateien. Keine neuen Packages oder Projekte.

## Phase 0: Research

Siehe [research.md](./research.md) für vollständige Findings.

**Kritische Entscheidungen:**

| Frage | Entscheidung | Begründung |
|-------|-------------|------------|
| Workflow-Counter-Mechanismus | `try/finally` in `RunAsync` | Garantiert Inkrementierung bei allen terminalen Ausgängen |
| Proxy-API-Erweiterung | Rückwärtskompatibles `workflowCounts`-Feld | Bestehende Clients unverändert |
| Parallel-Queries im Proxy | `Promise.all([activityQuery, workflowQuery])` | Minimiert Latenzzunahme |
| Metric-Key im Visualizer | `workflowRef` aus `node.raw` | 1:1 mit Worker-Klassennamen, kein Mapping nötig |
| SubflowNode | Zeigt `–` (kein `workflowRef`) | Kein Temporal-Counter-Pendant für Subflows |

## Phase 1: Design & Contracts

Siehe [data-model.md](./data-model.md) und [contracts/metrics-proxy-api.md](./contracts/metrics-proxy-api.md).

### Datenfluss

```
Worker (RunAsync try/finally)
  → RecordWorkflowExecution("RiskEnrichmentWorkflow")
  → Counter<long> temporal_workflow_task_completed{workflow_type="..."}
  → Prometheus Scrape (:9090/metrics)
  → PromQL: sum by(workflow_type)(increase(temporal_workflow_task_completed_total[1d]))
  → Metrics Proxy /api/activity-counts → { counts: {...}, workflowCounts: {...} }
  → useActivityMetrics hook → MetricsContext
  → ChildWorkflowNode: workflowCounts[nodeData.workflowRef] → ExecutionCountBadge
```

### Quickstart

Siehe [quickstart.md](./quickstart.md) für den vollständigen Validierungsleitfaden.

## Constitution Check (Post-Design)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Code Quality | ✅ PASS | XML-Docs für neue public API; keine Compiler-Warnungen |
| II. Testing Standards | ✅ PASS | Unit-Tests für Counter, Proxy-Query, Context-Hook |
| III. UX Consistency | ✅ PASS | `–` für unbekannte Nodes; Loading/Unavailable-States geerbt |
| IV. Performance | ✅ PASS | Parallel-Queries; kein zusätzlicher Poll-Intervall |

**Gate Result: PASS**

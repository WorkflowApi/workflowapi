# Implementation Plan: Node Detail View

**Branch**: `005-node-detail-view` | **Date**: 2026-06-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-node-detail-view/spec.md`

## Summary

Fügt dem Workflow-Visualizer ein Detail-Panel rechts vom Graphen hinzu. Ein Klick auf einen Node öffnet das Panel mit allen verfügbaren statischen Daten aus `WorkflowGraphNode` / `WorkflowNodeData` (Label, Typ, Beschreibung, `raw`-Felder). Wenn ein Runtime-Overlay (MetricsContext) aktiv ist, werden Ausführungszähler als optionaler Abschnitt ergänzt. Das Panel schließt sich beim erneuten Klick auf denselben Node, beim Klick außerhalb des Graphen oder via Escape. Das Layout in `App.tsx` wechselt beim Öffnen zu einem `flex-row`-Split (70 % Graph / 30 % Panel).

## Technical Context

**Language/Version**: TypeScript 5.x

**Primary Dependencies**:
- React 18, @xyflow/react 12, Vite 6
- Tailwind CSS (bestehend)
- MetricsContext (bestehend, für optionale Laufzeitdaten)

**Storage**: N/A (reiner UI-State, kein Persistence)

**Testing**: Vitest + React Testing Library (bestehend)

**Target Platform**: Desktop-Browser (≥1024 px)

**Project Type**: Inkrementelles UI-Feature auf bestehender React-Anwendung

**Performance Goals**: Panel-Render < 100 ms nach Node-Klick (synchroner React-State-Update)

**Constraints**: Kein neuer Service, kein neuer Port, kein direkter Temporal/Prometheus-Zugriff; read-only in v1; kein Mobile-Breakpoint

**Scale/Scope**: Lokales Dev-Setup; max. ~30 Nodes pro Workflow-Dokument

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Code Quality | ✅ PASS | TypeScript strict; neue Komponente hat klare Verantwortlichkeit; kein Cross-Layer-Leak |
| II. Testing Standards | ✅ PASS | Unit-Tests für NodeDetailPanel (alle Felder, leere Felder, kein Overlay); Snapshot-Test |
| III. UX Consistency | ✅ PASS | WCAG 2.1 AA: Escape-Handling, Keyboard-Navigation; graceful degradation ohne Overlay |
| IV. Performance | ✅ PASS | Synchroner State-Update; kein zusätzlicher Netzwerkrequest beim Panel-Öffnen |
| Quality Gates | ✅ PASS | Keine Secrets; read-only Panel; Temporal-Logik bleibt im Worker/Proxy |
| Development Workflow | ✅ PASS | Kleine, isolierte Änderungen; klare Acceptance Criteria |

**Gate Result: PASS** — Keine Verletzungen. Weiter mit Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/005-node-detail-view/
├── plan.md              # Dieses Dokument
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── node-detail-panel-contract.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (betroffene Pfade)

```text
src/web/workflow-react-flow-visualizer-mvp/src/
├── App.tsx                                 # Layout-Split (flex-row), selectedNodeId-State
├── components/
│   ├── WorkflowVisualizer.tsx              # +onNodeSelect callback prop, +onPaneClick
│   └── NodeDetailPanel.tsx                 # NEU: Detail-Panel-Komponente
└── components/nodes/                       # Keine Änderungen
```

**Structure Decision**: Inkrementelle Erweiterung. Keine neuen Packages. Eine neue Komponente, zwei modifizierte Dateien.

## Phase 0: Research

Siehe [research.md](./research.md) für vollständige Findings.

**Kritische Entscheidungen:**

| Frage | Entscheidung | Begründung |
|-------|-------------|------------|
| Node-Klick-API | `onNodeClick` + `onPaneClick` auf `<ReactFlow>` | Standard-ReactFlow-v12-API; liefert vollständiges `Node<WorkflowNodeData>`-Objekt |
| Deselect | Toggle: gleiche ID → `null`; `onPaneClick` → `null` | Idiomatisch, kein Custom-Hook nötig |
| Escape-Handling | `useEffect` + `document.addEventListener("keydown")` in `NodeDetailPanel` | Direktes, verbreitetes React-Muster |
| Layout-Split | `flex flex-row` in App.tsx: Graph `flex-1 min-w-0`, Panel `w-80 shrink-0` | Stabile Panelbreite; Graph nutzt restlichen Raum |
| State-Location | `selectedNodeId: string \| null` in `App.tsx` | Minimaler Scope; kein globaler Context nötig für v1 |

## Phase 1: Design & Contracts

Siehe [data-model.md](./data-model.md) und [contracts/node-detail-panel-contract.md](./contracts/node-detail-panel-contract.md).

### Datenfluss

```
User klickt Node im ReactFlow-Graphen
  → onNodeClick(event, node) in WorkflowVisualizer
  → ruft onNodeSelect(node.id) Callback auf (toggle: gleiche ID → null)
  → App.tsx: setSelectedNodeId(id | null)
  → NodeDetailPanel erhält node: WorkflowGraphNode | undefined
  → Panel rendert alle verfügbaren Felder aus node.raw + MetricsContext
  → Layout: div.flex.flex-row; Graph bekommt flex-1 min-w-0; Panel w-80

User drückt Escape / klickt auf Pane / klickt Schließen-Button
  → App.tsx: setSelectedNodeId(null)
  → Panel verschwindet; Graph nimmt volle Breite
```

### Quickstart

Siehe [quickstart.md](./quickstart.md) für den vollständigen Validierungsleitfaden.

## Constitution Check (Post-Design)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Code Quality | ✅ PASS | `NodeDetailPanel` hat eine einzige Verantwortlichkeit; Props klar typisiert |
| II. Testing Standards | ✅ PASS | Snapshot-Test, Unit-Tests für Öffnen/Schließen und Overlay-absent-State |
| III. UX Consistency | ✅ PASS | Escape + Schließen-Button; WCAG-konforme `role`-Attribute; kein Fehlerzustand ohne Overlay |
| IV. Performance | ✅ PASS | Synchroner State-Update; kein Netzwerkrequest |

**Gate Result: PASS**

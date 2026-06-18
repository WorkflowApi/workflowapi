# Tasks: Node Detail View

**Input**: Design documents from `specs/005-node-detail-view/`

**Prerequisites**: plan.md ✅ · spec.md ✅ · research.md ✅ · data-model.md ✅ · contracts/ ✅

**Tests**: Nicht explizit in der Spezifikation gefordert — keine Test-Tasks generiert.

## Format: `[ID] [P?] [Story?] Beschreibung`

- **[P]**: Parallel ausführbar (unterschiedliche Dateien, keine gegenseitigen Abhängigkeiten)
- **[Story]**: Zugehörige User Story (`US1`, `US2`, `US3`)

---

## Phase 1: Setup

**Purpose**: Keine neuen Abhängigkeiten oder Projektstruktur nötig — bestehendes Vite/React-Projekt wird erweitert.

- [x] T001 Verifiziere dass `src/web/workflow-react-flow-visualizer-mvp` mit `npm install` und `npm run dev` fehlerfrei startet (Baseline-Check vor Änderungen)

---

## Phase 2: Foundational (Blocking Prerequisite)

**Purpose**: Wiederverwendbarer Escape-Key-Hook, der von `App.tsx` (US1 + US2) benötigt wird.

**⚠️ CRITICAL**: Muss vor US1 und US2 abgeschlossen sein.

- [x] T002 Erstelle `useEscapeKey(enabled: boolean, onEscape: () => void): void` Hook in `src/web/workflow-react-flow-visualizer-mvp/src/hooks/useEscapeKey.ts` — nutzt `useEffect` + `window.addEventListener("keydown", ...)`, ist nur aktiv wenn `enabled === true`

**Checkpoint**: Hook exportiert und aufrufbar — User-Story-Implementierung kann beginnen.

---

## Phase 3: User Story 1 — Node inspizieren durch Klick (Priority: P1) 🎯 MVP

**Goal**: Klick auf einen Node öffnet ein Detail-Panel rechts mit Name, Typ, Beschreibung und typ-spezifischen Referenzfeldern. Klick auf anderen Node aktualisiert das Panel.

**Independent Test**: Vite Dev-Server starten, auf Activity-Node klicken → Panel erscheint rechts mit Daten; auf anderen Node klicken → Panel aktualisiert; angeklickter Node ist visuell hervorgehoben.

### Implementation für User Story 1

- [x] T003 [P] [US1] Erweitere `WorkflowVisualizerProps` um optionale Props `onNodeSelect?: (nodeId: string | null) => void` und `selectedNodeId?: string | null` in `src/web/workflow-react-flow-visualizer-mvp/src/components/WorkflowVisualizer.tsx`; füge `onNodeClick`-Handler (ruft `onNodeSelect(node.id)` auf), `onPaneClick`-Handler (ruft `onNodeSelect(null)` auf) und `displayNodes`-Memo (setzt `selected: node.id === selectedNodeId`) hinzu; übergib `displayNodes` statt `nodes` an `<ReactFlow>`
- [x] T004 [P] [US1] Erstelle `NodeDetailPanel`-Komponente in `src/web/workflow-react-flow-visualizer-mvp/src/components/NodeDetailPanel.tsx` mit Props `node: WorkflowGraphNode | undefined` und `onClose: () => void`; rendert `null` wenn `node === undefined`; enthält: Header (Node-Name als `h2`, Kind-Badge, Schließen-Button mit `aria-label="Details schließen"`), optionalen Beschreibungs-Abschnitt (nur wenn `node.description` vorhanden), Details-Abschnitt (Typ, `activityType` nur bei `kind==="activity"`, `workflowRef` nur bei `kind==="childWorkflow"`, `bridgeRef` nur bei `kind==="bridge"`, Node-ID); Wrapper hat `role="complementary"` und `aria-label="Node Details"`
- [x] T005 [US1] Aktualisiere `App.tsx` in `src/web/workflow-react-flow-visualizer-mvp/src/App.tsx`: füge `selectedNodeId: string | null` State hinzu, `handleNodeSelect`-Funktion (Toggle: gleiche ID → `null`), Escape-Handling via `useEscapeKey(selectedNodeId !== null, () => setSelectedNodeId(null))`; ändere Layout-Container auf `flex flex-row`; Graph-Div bekommt `min-w-0 flex-1`; wenn `selectedNodeId !== null` rendere `<aside className="w-80 shrink-0 overflow-y-auto border-l border-slate-200">` mit `<NodeDetailPanel node={selectedNode} onClose={() => setSelectedNodeId(null)} />`; übergib `selectedNodeId` und `onNodeSelect={handleNodeSelect}` an `WorkflowVisualizer`
- [x] T006 [P] [US1] Füge `selected`-Ring-Styling zu `ActivityNode` hinzu in `src/web/workflow-react-flow-visualizer-mvp/src/components/nodes/ActivityNode.tsx` — `selected` aus `NodeProps` lesen; `ring-2 ring-sky-400 border-sky-600` wenn `selected === true`
- [x] T007 [P] [US1] Füge `selected`-Ring-Styling zu `ChildWorkflowNode` hinzu in `src/web/workflow-react-flow-visualizer-mvp/src/components/nodes/ChildWorkflowNode.tsx` — analog zu T006
- [x] T008 [P] [US1] Füge `selected`-Ring-Styling zu `BridgeNode` hinzu in `src/web/workflow-react-flow-visualizer-mvp/src/components/nodes/BridgeNode.tsx` — analog zu T006
- [x] T009 [P] [US1] Füge `selected`-Ring-Styling zu `StepNode` hinzu in `src/web/workflow-react-flow-visualizer-mvp/src/components/nodes/StepNode.tsx` — analog zu T006

**Checkpoint**: User Story 1 vollständig funktional und unabhängig testbar — Panel öffnet und aktualisiert sich, Nodes werden hervorgehoben.

---

## Phase 4: User Story 2 — Detail-Panel schließen (Priority: P2)

**Goal**: Nutzer kann das Panel über Escape-Taste schließen (Close-Button und Pane-Klick sind bereits durch T003–T005 abgedeckt).

**Independent Test**: Panel öffnen → Escape drücken → Panel verschwindet; Close-Button klicken → Panel verschwindet; in Pane klicken → Panel verschwindet; Graph nimmt volle Breite ein.

### Implementation für User Story 2

- [x] T010 [US2] Verifiziere und vervollständige alle Schließ-Pfade in `App.tsx` (`src/web/workflow-react-flow-visualizer-mvp/src/App.tsx`): stelle sicher dass `useEscapeKey` (aus T002) korrekt eingebunden ist und alle vier Schließ-Wege funktionieren (Escape, Schließen-Button via `onClose`, Pane-Klick via `onPaneClick` in WorkflowVisualizer, Toggle-Klick); prüfe dass Graph nach dem Schließen wieder `flex-1`-Breite einnimmt

**Checkpoint**: Alle Schließ-Mechanismen funktionieren; User Stories 1 und 2 vollständig unabhängig testbar.

---

## Phase 5: User Story 3 — Runtime-Overlay Metriken (Priority: P3)

**Goal**: Wenn MetricsContext Daten liefert, zeigt das Panel Ausführungszähler. Fehlt das Overlay, bleibt das Panel fehlerfrei und zeigt nur statische Daten.

**Independent Test**: Mit laufendem Proxy: Activity-Node anklicken → Metriken-Abschnitt erscheint mit Zahl. Ohne Proxy: Activity-Node anklicken → kein Metriken-Abschnitt, kein Fehler.

### Implementation für User Story 3

- [x] T011 [US3] Ergänze Metriken-Abschnitt in `NodeDetailPanel` in `src/web/workflow-react-flow-visualizer-mvp/src/components/NodeDetailPanel.tsx`: importiere `useMetrics` aus `MetricsContext`; wenn `isUnavailable === true` → kein Metriken-Abschnitt; wenn `isLoading === true` → Spinner/Lade-Indikator; wenn Zähler für `activityType` in `counts` oder `workflowRef` in `workflowCounts` vorhanden → zeige Abschnitt „Ausführungen" mit der Zahl; kein Abschnitt wenn kein passender Key gefunden

**Checkpoint**: Alle drei User Stories unabhängig testbar und vollständig funktional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: UX-Feinschliff und abschließende Validierung.

- [x] T012 [P] Füge Expand/Collapse für lange Inhalte (z. B. lange Node-IDs, Refs) in `NodeDetailPanel.tsx` hinzu — nutze native `<details>`/`<summary>` HTML-Elemente; Text über 80 Zeichen wird standardmäßig eingeklappt
- [ ] T013 Führe alle Validierungsszenarien aus `specs/005-node-detail-view/quickstart.md` manuell durch und hake die Akzeptanzkriterien-Checkliste ab

---

## Dependencies & Execution Order

### Phase-Abhängigkeiten

- **Phase 1 (Setup)**: Keine Abhängigkeiten — sofort startbar
- **Phase 2 (Foundational)**: Abhängig von Phase 1 — **blockiert US1 und US2**
- **Phase 3 (US1)**: Abhängig von Phase 2 — T003, T004, T006, T007, T008, T009 parallel; T005 erst nach T003 und T004
- **Phase 4 (US2)**: Abhängig von T005 — T010 baut auf App.tsx-Änderungen auf
- **Phase 5 (US3)**: Abhängig von T004 (NodeDetailPanel muss existieren)
- **Phase 6 (Polish)**: Abhängig von T004; T013 erst nach allen vorherigen Phasen

### User-Story-Abhängigkeiten

- **US1 (P1)**: Kann nach Phase 2 starten — keine Abhängigkeit zu US2/US3
- **US2 (P2)**: Baut auf App.tsx (T005) und WorkflowVisualizer (T003) aus US1 auf
- **US3 (P3)**: Baut auf NodeDetailPanel (T004) aus US1 auf — unabhängig von US2

### Innerhalb Phase 3

```
T002 (Hook) ──────────────────────────────────────────┐
                                                       ▼
T003 (WorkflowVisualizer) ─┬──────────────────────▶ T005 (App.tsx)
T004 (NodeDetailPanel)     ┘
T006 (ActivityNode)    ─ parallel ─┐
T007 (ChildWorkflowNode) ─ parallel─┤ (unabhängig, können gleichzeitig mit T003/T004 laufen)
T008 (BridgeNode)      ─ parallel ─┤
T009 (StepNode)        ─ parallel ─┘
```

---

## Parallel Example: User Story 1

```bash
# Gleichzeitig startbar nach T002:
Task T003: WorkflowVisualizer.tsx erweitern
Task T004: NodeDetailPanel.tsx erstellen
Task T006: ActivityNode.tsx — selected-Styling
Task T007: ChildWorkflowNode.tsx — selected-Styling
Task T008: BridgeNode.tsx — selected-Styling
Task T009: StepNode.tsx — selected-Styling

# Erst nach T003 + T004:
Task T005: App.tsx — Layout-Split und State-Verdrahtung
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Baseline-Check (T001)
2. Phase 2: `useEscapeKey` Hook (T002)
3. Phase 3: US1 vollständig (T003–T009)
4. **STOP und VALIDIEREN**: Panel öffnet sich, Daten korrekt, Nodes hervorgehoben
5. Demo-fähig nach Phase 3

### Incremental Delivery

1. T001 → T002 → T003–T009 → **Demo US1** (Panel öffnet mit statischen Daten)
2. T010 → **Demo US2** (alle Schließ-Mechanismen)
3. T011 → **Demo US3** (Metriken im Panel)
4. T012–T013 → **Finales Release** (Polish + Validierung)

---

## Notes

- [P] Tasks betreffen unterschiedliche Dateien und haben keine gegenseitigen Abhängigkeiten
- [Story]-Labels ermöglichen unabhängige Implementation und Test pro User Story
- MVP ist nach Phase 3 (US1) voll demo-fähig
- `WorkflowVisualizer`-Änderungen sind rückwärtskompatibel (neue Props optional)
- Keine neuen npm-Pakete erforderlich

# Contract: NodeDetailPanel Component

## Zweck

Definiert die öffentliche Schnittstelle der `NodeDetailPanel`-Komponente sowie die Änderungen an `WorkflowVisualizer`.

---

## NodeDetailPanel

### Props

```ts
interface NodeDetailPanelProps {
  /** Der aktuell ausgewählte Graph-Node. undefined = Panel ist leer / nicht sichtbar. */
  node: WorkflowGraphNode | undefined;
  /** Callback zum Schließen des Panels (Schließen-Button). */
  onClose: () => void;
}
```

### Verhalten

| Szenario | Verhalten |
|----------|-----------|
| `node === undefined` | Komponente rendert nichts (`null`) |
| `node.description` fehlt | Beschreibungs-Abschnitt wird ausgeblendet |
| `node.kind === "activity"` und `activityType` vorhanden | Activity-Typ-Zeile wird angezeigt |
| `isUnavailable === true` im MetricsContext | Metriken-Abschnitt wird vollständig ausgeblendet |
| `isLoading === true` im MetricsContext | Metriken-Abschnitt zeigt Lade-Indikator |
| `MetricsContext` liefert Zähler | Zähler werden im Metriken-Abschnitt angezeigt |

### Abschnitte (gerenderte Sektionen)

```
┌─────────────────────────────────────┐
│  [×]  Node-Name (h2)                │ ← immer
│       Badge: Node-Typ               │ ← immer
├─────────────────────────────────────┤
│  Beschreibung                       │ ← nur wenn vorhanden
│  "..."                              │
├─────────────────────────────────────┤
│  Details                            │ ← immer
│  Typ:          activity             │
│  Activity-Typ: FetchRiskData        │ ← nur bei activity + activityType
│  Workflow-Ref: CalculateRisk...     │ ← nur bei childWorkflow + workflowRef
│  Bridge-Ref:   ...                  │ ← nur bei bridge + bridgeRef
│  Node-ID:      workflow::nodeId     │ ← immer (technisches Feld, klein)
├─────────────────────────────────────┤
│  Ausführungen                       │ ← nur wenn Overlay aktiv und Zähler bekannt
│  Gesamt: 1.234                      │
└─────────────────────────────────────┘
```

### Accessibility

- Wrapper-Element hat `role="complementary"` und `aria-label="Node Details"`
- Schließen-Button hat `aria-label="Details schließen"`
- Escape-Key schließt das Panel (via `useEscapeKey`-Hook in `App.tsx`)
- Alle Texte sind screen-reader-lesbar (kein `aria-hidden` auf Inhalt)

---

## WorkflowVisualizer (Änderungen)

### Neue Props

```ts
interface WorkflowVisualizerProps {
  graph: WorkflowGraph;
  /** Callback wenn ein Node angeklickt wird (Toggle: gleiche ID = Deselect). */
  onNodeSelect?: (nodeId: string | null) => void;
  /** ID des aktuell ausgewählten Nodes für Highlight-Darstellung. */
  selectedNodeId?: string | null;
}
```

### Verhalten

- `onNodeClick` auf `<ReactFlow>`: ruft `onNodeSelect(node.id)` auf (Toggle-Logik liegt in `App.tsx`)
- `onPaneClick` auf `<ReactFlow>`: ruft `onNodeSelect(null)` auf
- `displayNodes` (via `useMemo`): setzt `selected: true` auf dem Node mit `id === selectedNodeId`
- Beide Props sind optional (rückwärtskompatibel, bestehende Tests brechen nicht)

---

## App.tsx Layout-Kontrakt

```tsx
// Pseudostruktur
<main>
  <header>{/* Titel + TimeRangeSelector */}</header>
  <div className="flex h-[70vh] ...">
    <div className="min-w-0 flex-1">
      <WorkflowVisualizer
        graph={graph}
        selectedNodeId={selectedNodeId}
        onNodeSelect={handleNodeSelect}
      />
    </div>
    {selectedNode && (
      <aside className="w-80 shrink-0 border-l overflow-y-auto">
        <NodeDetailPanel node={selectedNode} onClose={() => setSelectedNodeId(null)} />
      </aside>
    )}
  </div>
  <footer>{/* Statuszeilen */}</footer>
</main>
```

---

## useEscapeKey Hook

```ts
// hooks/useEscapeKey.ts
export function useEscapeKey(enabled: boolean, onEscape: () => void): void
```

| Parameter | Beschreibung |
|-----------|-------------|
| `enabled` | Hook ist nur aktiv wenn `true` (verhindert unnötige Listener) |
| `onEscape` | Callback bei Escape-Tastendruck |

---

## Rückwärtskompatibilität

- `WorkflowVisualizer` ist rückwärtskompatibel: neue Props `onNodeSelect` und `selectedNodeId` sind optional.
- Bestehende Tests, die `WorkflowVisualizer` ohne diese Props rendern, brechen nicht.
- `NodeDetailPanel` ist eine neue Datei; keine bestehende Datei wird destruktiv verändert.

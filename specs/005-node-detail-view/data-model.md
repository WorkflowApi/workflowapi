# Data Model: Node Detail View

## Übersicht

Das Detail-Panel ist ein rein lesendes UI-Feature. Es benötigt keine neue Persistenzschicht und kein neues Backend-Modell. Die relevanten Datenstrukturen sind bestehend und werden lediglich für die Darstellung im Panel ausgelesen.

---

## Bestehende Entitäten (relevant für das Panel)

### `WorkflowGraphNode` (bestehend, unveränderlich)

Quelle: `src/web/workflow-react-flow-visualizer-mvp/src/types/workflow-graph-model.ts`

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| `id` | `string` | Eindeutige Node-ID (`"workflowName::localNodeId"`) |
| `kind` | `WorkflowNodeKind` | Typ des Nodes (`activity`, `step`, `bridge`, `childWorkflow`, `subflow`, ...) |
| `label` | `string` | Anzeigename des Nodes |
| `description` | `string \| undefined` | Optionale Zusammenfassung / Summary |
| `sourcePath` | `string \| undefined` | Optionaler Referenzpfad im Quelldokument |
| `raw` | `unknown` | Vollständiges Original-Objekt aus dem WorkflowAPI-Dokument |

### `WorkflowNodeData` (bestehend, unveränderlich)

Quelle: `src/web/workflow-react-flow-visualizer-mvp/src/adapters/graph-to-reactflow.ts`

Wird als `data`-Prop von ReactFlow-Nodes verwendet. Enthält alle Felder aus `WorkflowGraphNode` plus aufgelöste Referenzen:

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| `label` | `string` | Anzeigename |
| `description` | `string \| undefined` | Optionale Beschreibung |
| `kind` | `string` | Node-Typ |
| `id` | `string` | Node-ID |
| `localId` | `string \| undefined` | Lokale ID im Workflow |
| `activityType` | `string \| undefined` | Klassen-/Typname der Activity (für Metrik-Lookup) |
| `workflowRef` | `string \| undefined` | Referenz auf einen Child-Workflow (für Metrik-Lookup) |
| `[key: string]` | `unknown` | Weitere Felder aus `raw` |

### `ActivityMetricsState` (bestehend, unveränderlich)

Quelle: `src/web/workflow-react-flow-visualizer-mvp/src/hooks/useActivityMetrics.ts`

Bereitgestellt via `MetricsContext`. Wird im Panel als optionaler Abschnitt genutzt.

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| `counts` | `Record<string, number>` | Activity-Ausführungszähler (Key: `activityType`) |
| `workflowCounts` | `Record<string, number>` | Workflow-Ausführungszähler (Key: `workflowRef`) |
| `isLoading` | `boolean` | Metriken werden geladen |
| `isUnavailable` | `boolean` | Metriken nicht verfügbar (kein Overlay) |

---

## Neuer UI-State

### `selectedNodeId: string | null`

Verwaltung in `App.tsx`. Kein Context, kein globaler Store.

| Zustand | Wert | Bedeutung |
|---------|------|-----------|
| Panel geschlossen | `null` | Kein Node ausgewählt |
| Panel offen | `string` | ID des ausgewählten Nodes |

---

## Anzuzeigende Felder pro Node-Typ

### Alle Node-Typen (statische Felder)

| Anzeige-Label | Quelle | Bedingung |
|---------------|--------|-----------|
| Name | `node.label` | Immer |
| Typ | `node.kind` | Immer |
| Beschreibung | `node.description` | Nur wenn vorhanden |
| Node-ID | `node.id` | Immer (technisch, eingeklappt) |

### Activity-Node (`kind: "activity"`)

| Anzeige-Label | Quelle | Bedingung |
|---------------|--------|-----------|
| Activity-Typ | `(node.raw as any).activityType` | Wenn vorhanden |
| Ausführungen | `MetricsContext.counts[activityType]` | Wenn Overlay aktiv und activityType bekannt |

### ChildWorkflow-Node (`kind: "childWorkflow"`)

| Anzeige-Label | Quelle | Bedingung |
|---------------|--------|-----------|
| Workflow-Referenz | `(node.raw as any).workflowRef` | Wenn vorhanden |
| Ausführungen | `MetricsContext.workflowCounts[workflowRef]` | Wenn Overlay aktiv und workflowRef bekannt |

### Bridge-Node (`kind: "bridge"`)

| Anzeige-Label | Quelle | Bedingung |
|---------------|--------|-----------|
| Bridge-Referenz | `(node.raw as any).bridgeRef` | Wenn vorhanden |

### Step-Node (`kind: "step"`)

Nur Basisfelder (Name, Typ). Start/End-Nodes haben keine weiteren Metadaten.

---

## Validierungsregeln

- Fehlende optionale Felder führen zu **ausgeblendeten Abschnitten** (nicht zu leeren Zeilen).
- `raw`-Felder werden typsicher über `typeof`-Guards ausgelesen.
- Metriken werden nur angezeigt, wenn `isUnavailable === false` und der Metrik-Key vorhanden ist.
- Metriken im Lade-Zustand (`isLoading === true`) zeigen ein Lade-Indikator, keine Zahl.

---

## Zustandsübergänge

```
null (Panel geschlossen)
  ──[onNodeClick(nodeId)]──▶  nodeId (Panel offen, Panel zeigt Node-Details)
                                  │
  ◀──[onNodeClick(gleiche ID)]────┤ (Toggle-Close)
  ◀──[onPaneClick]────────────────┤
  ◀──[Escape-Taste]───────────────┤
  ◀──[Schließen-Button]───────────┘
  
  ──[onNodeClick(andereId)]──▶  andereId (Panel aktualisiert sich)
```

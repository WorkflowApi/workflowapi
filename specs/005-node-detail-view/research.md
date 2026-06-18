# Research: Node Detail View

**Phase 0 Findings** — alle NEEDS CLARIFICATION aufgelöst.

---

## 1. ReactFlow v12 Node-Klick-API

**Entscheidung**: `onNodeClick` + `onPaneClick` Props auf `<ReactFlow />`

**Findings**:

```ts
// Type-Signatur in @xyflow/react v12
type NodeMouseHandler<NodeType extends Node = Node> =
  (event: React.MouseEvent, node: NodeType) => void;
```

```tsx
<ReactFlow
  onNodeClick={(_event, node) => handleNodeSelect(node.id)}
  onPaneClick={() => setSelectedNodeId(null)}
/>
```

`node.id` und `node.data` (= `WorkflowNodeData`) sind direkt verfügbar. Der `Node`-Typ ist generisch typisierbar: `Node<WorkflowNodeData>`.

**Alternativen verworfen**:
- `onSelectionChange`: Feuert auch bei programmatischen Änderungen, ungeeignet als primärer Trigger.
- Custom Event auf dem Node-Element: Zu viel Boilerplate, kein Mehrwert.

---

## 2. Deselect-Mechanismus

**Entscheidung**: Toggle-Pattern in `onNodeClick` + `onPaneClick` → `null`

```tsx
const onNodeClick = useCallback((_e: React.MouseEvent, node: Node) => {
  setSelectedNodeId((current) => (current === node.id ? null : node.id));
}, []);

const onPaneClick = useCallback(() => {
  setSelectedNodeId(null);
}, []);
```

ReactFlow v12 deselektiert **nicht** automatisch bei erneutem Klick auf denselben Node — das Toggle muss explizit implementiert werden.

---

## 3. Layout-Split (70/30)

**Entscheidung**: Tailwind `flex` + `basis-[70%]` / `basis-[30%]` mit `min-w-0` und `overflow-hidden`

```tsx
<div className="flex h-[70vh] overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
  <div className="min-w-0 flex-1">
    {/* ReactFlow Graph */}
  </div>
  {selectedNodeId && (
    <aside className="w-80 shrink-0 overflow-y-auto border-l border-slate-200 bg-white">
      {/* NodeDetailPanel */}
    </aside>
  )}
</div>
```

Wichtig: `min-w-0` auf dem Graph-Container verhindert, dass ReactFlow unter dem Panel überläuft. Feste Breite `w-80` (320 px) statt prozentualer Teilung, da das Panel eine stabile Breite braucht.

---

## 4. Escape-Handling

**Entscheidung**: Wiederverwendbarer `useEscapeKey`-Hook mit `useEffect` + `window.addEventListener`

```tsx
// hooks/useEscapeKey.ts
function useEscapeKey(enabled: boolean, onEscape: () => void): void {
  useEffect(() => {
    if (!enabled) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape") onEscape();
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [enabled, onEscape]);
}
```

Verwendung in `App.tsx`:
```tsx
useEscapeKey(selectedNodeId !== null, () => setSelectedNodeId(null));
```

`enabled`-Flag verhindert einen aktiven Listener wenn das Panel geschlossen ist.

---

## 5. Selektions-Highlight im Graphen

**Entscheidung**: `selected: boolean` direkt auf den Node-Objekten setzen via `useMemo`

```tsx
const displayNodes = useMemo(
  () => nodes.map((n) => ({ ...n, selected: n.id === selectedNodeId })),
  [nodes, selectedNodeId]
);

<ReactFlow nodes={displayNodes} ... />
```

In `NodeProps` ist `selected` dann direkt als Prop verfügbar:

```tsx
function ActivityNode({ data, selected }: NodeProps) {
  return (
    <div className={`... ${selected ? "ring-2 ring-sky-400 border-sky-500" : ""}`}>
      ...
    </div>
  );
}
```

**Wichtig**: Das bestehende `nodes`-State aus `useNodesState` wird **nicht** direkt mutiert. Stattdessen wird `displayNodes` als abgeleitetes Memo übergeben. Das verhindert Konflikte mit ReactFlows internem State-Management.

---

## Zusammenfassung der getroffenen Entscheidungen

| Thema | Entscheidung | Begründung |
|-------|-------------|------------|
| Click-Handling | `onNodeClick` + `onPaneClick` | Standard-API; liefert vollständiges Node-Objekt |
| Deselect | Toggle in `onNodeClick` | ReactFlow macht es nicht automatisch |
| Layout | `flex` + `min-w-0 flex-1` Graph + `w-80 shrink-0` Panel | Stabile Panelbreite; kein Layout-Overflow |
| Escape | `useEscapeKey` Custom Hook | Wiederverwendbar; kein aktiver Listener wenn Panel zu |
| Highlight | `selected`-Flag via `useMemo` | Kein State-Konflikt mit ReactFlow-Internals |
| State-Location | `selectedNodeId` in `App.tsx` | Minimaler Scope; kein neuer Context nötig |

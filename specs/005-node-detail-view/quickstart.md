# Quickstart: Node Detail View — Validierungsleitfaden

## Voraussetzungen

- Docker Compose Stack läuft (`docker compose up` aus `src/`)
- Oder: Vite Dev-Server direkt (`cd src/web/workflow-react-flow-visualizer-mvp && npm run dev`)
- Browser auf `http://localhost:5173`

---

## Szenario 1: Panel öffnet sich beim Node-Klick (P1 — MVP)

**Ziel**: FR-001, FR-002

1. Seite laden — Workflow-Graph ist sichtbar, kein Panel rechts.
2. Auf einen **Activity-Node** (blauer Rahmen, z. B. "Fetch Risk Data") klicken.
3. **Erwartetes Ergebnis**:
   - Rechts neben dem Graphen erscheint ein Panel mit:
     - Node-Name als Überschrift
     - Typ-Badge (`activity`)
     - Beschreibung (wenn vorhanden)
     - Detail-Zeile „Activity-Typ: FetchRiskData" (o. ä.)
     - Node-ID
   - Der angeklickte Node ist im Graphen visuell hervorgehoben (Ring/Rahmen).

---

## Szenario 2: Panel wechselt bei Klick auf anderen Node (P1)

**Ziel**: FR-003

1. Szenario 1 ausgeführt (Panel zeigt Node A).
2. Auf einen **anderen Node** (z. B. einen ChildWorkflow-Node) klicken.
3. **Erwartetes Ergebnis**:
   - Panel aktualisiert sich sofort mit den Daten des neuen Nodes.
   - Neuer Node ist hervorgehoben; alter Node hat keinen Highlight mehr.

---

## Szenario 3: Panel schließen (P2)

**Ziel**: FR-004, FR-005

Drei Wege zum Schließen testen:

| Aktion | Erwartetes Ergebnis |
|--------|---------------------|
| Schließen-Button (×) im Panel klicken | Panel verschwindet; Graph nimmt volle Breite ein |
| `Escape`-Taste drücken | Panel verschwindet; Graph nimmt volle Breite ein |
| Erneut auf denselben Node klicken | Panel verschwindet (Toggle) |
| In den leeren Bereich des Graphen klicken (Pane) | Panel verschwindet |

---

## Szenario 4: Graceful Degradation bei fehlenden Feldern (P1 — Edge Cases)

**Ziel**: FR-006

1. Einen **Step-Node** (Start/End, runde Form) anklicken.
2. **Erwartetes Ergebnis**:
   - Panel öffnet sich mit Name und Typ.
   - **Kein** leerer Beschreibungs-Abschnitt sichtbar.
   - **Kein** leerer Activity-Typ- oder Workflow-Ref-Abschnitt.
   - Kein Fehler in der Browser-Konsole.

---

## Szenario 5: Metriken im Panel (P3 — Runtime Overlay)

**Ziel**: FR-007, FR-008

### Mit aktivem Overlay (Prometheus + Proxy laufen):

1. Auf einen Activity-Node klicken.
2. **Erwartetes Ergebnis**: Panel zeigt unter „Ausführungen" eine Zahl (oder Lade-Spinner).

### Ohne Overlay (Proxy nicht erreichbar):

1. Proxy stoppen oder Umgebung ohne Proxy starten.
2. Auf beliebigen Node klicken.
3. **Erwartetes Ergebnis**:
   - Panel öffnet sich normal mit statischen Daten.
   - **Kein** Metriken-Abschnitt sichtbar.
   - **Keine** Fehlermeldung im Panel.

---

## Szenario 6: Tastaturnavigation / Accessibility (P1)

**Ziel**: FR-009

1. Mit `Tab` in den Graphen navigieren.
2. Mit `Enter` einen Node aktivieren (falls fokussiert).
3. Panel öffnet sich.
4. Mit `Tab` den Schließen-Button im Panel ansteuern.
5. Mit `Enter` oder `Space` den Schließen-Button aktivieren.
6. **Erwartetes Ergebnis**: Panel schließt sich; Fokus kehrt zum Graphen zurück.

---

## Automatisierte Tests

Ausführen mit:

```bash
cd src/web/workflow-react-flow-visualizer-mvp
npm run test
```

Relevante Testdateien (nach Implementierung):

| Datei | Testet |
|-------|--------|
| `NodeDetailPanel.test.tsx` | Render aller Felder, fehlende Felder, Schließen-Button, Overlay-absent |
| `WorkflowVisualizer.test.tsx` | `onNodeSelect`-Callback, `selectedNodeId`-Prop |
| `useEscapeKey.test.ts` | Hook feuert bei Escape, feuert nicht wenn `enabled=false` |
| `App.test.tsx` (Snapshot) | Panel-Layout bei geöffnetem/geschlossenem State |

---

## Akzeptanzkriterien-Checkliste

- [ ] Panel öffnet sich beim Klick auf einen Activity-Node mit Name, Typ, Beschreibung
- [ ] Panel öffnet sich beim Klick auf einen ChildWorkflow-Node mit Workflow-Ref
- [ ] Panel aktualisiert sich bei Klick auf anderen Node
- [ ] Schließen via × Button, Escape und Toggle-Klick funktionieren
- [ ] Graph nimmt nach Schließen wieder volle Breite ein
- [ ] Kein leerer Abschnitt bei Nodes ohne optionale Felder
- [ ] Metriken erscheinen wenn Overlay aktiv, verschwinden still wenn nicht aktiv
- [ ] Keyboard-Navigation und Schließen via Tastatur funktionieren
- [ ] Keine Konsolenfehler in allen Szenarien

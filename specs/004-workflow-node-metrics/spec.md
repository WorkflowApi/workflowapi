# Feature Specification: Workflow Node Execution Metrics

**Feature Branch**: `004-workflow-node-metrics`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "Die Anzeige der Anzahl der runs wird jetzt in allen nodes angezeigt. Die neuen Nodes bekommen jedoch keine Daten. implementiere auch die metriken im temporal worker."

## Context

Feature 003 introduced execution count badges on Activity nodes backed by `temporal_activity_task_completed_total` Prometheus counters. The UI was subsequently extended to render badges on all node types (ChildWorkflow, Subflow, Step), but these nodes never receive data because:

1. The Temporal worker only records activity-level counters — no workflow-level counters exist.
2. The metrics proxy only queries `temporal_activity_task_completed_total` — it does not query workflow execution counts.
3. The visualizer's ChildWorkflow and Subflow nodes look up keys that are never populated in the metrics response.

This feature closes the gap end-to-end: worker → Prometheus → metrics proxy → visualizer.

## User Scenarios & Testing *(mandatory)*

### User Story 1 – Child Workflow Nodes zeigen Ausführungszähler (Priority: P1)

Ein Entwickler betrachtet den Workflow-Graphen und sieht auf jedem ChildWorkflow-Node (z. B. `CalculateRiskWorkflow`, `ExternalChecksWorkflow`) einen Zähler, der zeigt, wie oft dieses Child Workflow im gewählten Zeitraum ausgeführt wurde — genau wie es bereits bei Activity Nodes funktioniert.

**Why this priority**: ChildWorkflow-Nodes sind die unmittelbar betroffene Node-Kategorie ohne Daten. Die Implementierung des Workflow-Counters im Worker ist die Voraussetzung für alle weiteren Node-Typen.

**Independent Test**: Kann getestet werden, indem der Visualizer bei laufendem Temporal geöffnet wird und geprüft wird, ob die ChildWorkflow-Nodes (CalculateRiskWorkflow, ExternalChecksWorkflow) numerische Zähler ≥ 0 im Badge anzeigen.

**Acceptance Scenarios**:

1. **Given** der Worker läuft und hat Workflows abgeschlossen, **When** der Visualizer geladen wird, **Then** zeigt jede ChildWorkflow-Node ein Badge mit der Anzahl der Workflow-Ausführungen im gewählten Zeitraum an.
2. **Given** ein Child Workflow wurde im gewählten Zeitraum noch nie ausgeführt, **When** der Benutzer die Node betrachtet, **Then** zeigt das Badge „0".
3. **Given** der Zeitraum wird gewechselt, **When** die neuen Daten geladen werden, **Then** aktualisiert sich der Zähler auf der ChildWorkflow-Node korrekt.

---

### User Story 2 – Subflow Nodes zeigen Ausführungszähler (Priority: P2)

Ein Entwickler sieht auf Subflow-Group-Nodes ebenfalls Ausführungszähler, die die Anzahl der zugehörigen Workflow-Ausführungen widerspiegeln.

**Why this priority**: Subflow-Nodes sind eine weitere betroffene Node-Kategorie. Da sie auf dieselbe Prometheus-Datenquelle (workflow_type Counter) zurückgreifen, ist die Implementierung nach P1 nahezu kostenlos.

**Independent Test**: Kann getestet werden, indem geprüft wird, ob Subflow-Nodes im Visualizer Zähler anzeigen (statt leerem/unavailable Badge).

**Acceptance Scenarios**:

1. **Given** der Worker läuft und der Visualizer ist geladen, **When** ein Subflow-Node dargestellt wird, **Then** zeigt das Badge die Anzahl der Ausführungen des zugehörigen Workflows an.
2. **Given** kein passender Workflow-Counter existiert (z. B. manuell erstellter Subflow-Name ohne Temporal-Entsprechung), **When** der Node dargestellt wird, **Then** zeigt das Badge „–" (nicht zuordbar) statt „0".

---

### User Story 3 – Worker exportiert Workflow-Execution-Metriken (Priority: P1)

Der Temporal Worker exportiert einen neuen Prometheus-Counter `temporal_workflow_task_completed` mit dem Label `workflow_type`, der bei jeder abgeschlossenen Workflow-Ausführung inkrementiert wird. Prometheus scrapt diesen Counter automatisch über den bestehenden Metrics-Endpoint.

**Why this priority**: Ohne diesen Counter können keine Workflow-Ausführungszahlen angezeigt werden. Dies ist die Grundlage für P1 und P2.

**Independent Test**: Kann getestet werden, indem der Metrics-Endpoint des Workers (`/metrics`) aufgerufen wird und geprüft wird, ob `temporal_workflow_task_completed_total{workflow_type="..."}` in der Ausgabe erscheint.

**Acceptance Scenarios**:

1. **Given** der Worker läuft und ein Workflow abgeschlossen wird, **When** der Metrics-Endpoint des Workers abgerufen wird, **Then** enthält die Ausgabe `temporal_workflow_task_completed_total{workflow_type="RiskEnrichmentWorkflow"}` mit einem Wert ≥ 1.
2. **Given** mehrere Workflow-Typen (RiskEnrichmentWorkflow, CalculateRiskWorkflow, ExternalChecksWorkflow) wurden ausgeführt, **When** der Metrics-Endpoint abgerufen wird, **Then** gibt es für jeden Typ einen separaten Counter-Eintrag.
3. **Given** ein Workflow mit einem Retry oder nach einem Fehler terminiert, **When** der Counter abgerufen wird, **Then** wurde er einmal pro terminalem Zustand inkrementiert (Completed, Failed und Cancelled zählen je einmal).

---

### Edge Cases

- Was passiert, wenn der `workflow_type`-Label-Wert im Worker vom `activityType`/`label`-Wert im WorkflowAPI-Dokument abweicht (z. B. durch Namespace-Prefix)? → Das Badge zeigt „–" (nicht zuordbar) — kein automatisches Mapping.
- Was passiert, wenn derselbe Name sowohl als Activity als auch als Workflow in Prometheus vorkommt? → Activity-Counts und Workflow-Counts werden getrennt abgefragt und in separaten Maps zurückgegeben; kein Konflikt.
- Was passiert, wenn Prometheus noch keinen Scrape für den neuen Counter durchgeführt hat (direkt nach Worker-Start)? → Der Counter fehlt in den Ergebnissen; betroffene Nodes zeigen „–" bis der erste Scrape vorliegt.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Der Temporal Worker MUSS bei **jedem terminalen Ausgang** eines Workflows (Completed, Failed, Cancelled) einen Counter `temporal_workflow_task_completed` mit dem Label `workflow_type` (Wert: Klassen-/Workflow-Name ohne Namespace) inkrementieren.
- **FR-002**: Der Counter MUSS über den bestehenden OpenTelemetry/Prometheus-HTTP-Endpoint (`/metrics`) exportiert werden — kein neuer Port oder Endpoint erforderlich.
- **FR-003**: Der Metrics Proxy MUSS eine zusätzliche PromQL-Abfrage für Workflow-Execution-Counts (`temporal_workflow_task_completed_total`) ausführen und die Ergebnisse im API-Response als separates Feld `workflowCounts` zurückgeben (neben dem bestehenden `counts`-Feld für Activities).
- **FR-004**: Der Metrics Proxy MUSS beide Abfragen (Activity- und Workflow-Counts) in einem einzigen API-Call bündeln, um unnötige Roundtrips zu vermeiden.
- **FR-005**: Die Visualizer-ChildWorkflow-Nodes MÜSSEN den Zähler aus `workflowCounts` statt aus `counts` lesen, mit dem Schlüssel gleich dem `workflowRef`-Feld aus dem WorkflowAPI-Dokument (z. B. `"CalculateRiskWorkflow"`). Der Adapter `graph-to-reactflow.ts` MUSS `workflowRef` aus `node.raw` in das `nodeData`-Objekt als eigenes Feld übertragen, damit es im Node-Renderer verfügbar ist.
- **FR-006**: Die Visualizer-Subflow-Nodes MÜSSEN denselben `workflowCounts`-Lookup wie ChildWorkflow-Nodes verwenden.
- **FR-007**: Die Visualizer-ActivityNode MUSS weiterhin ausschließlich `counts` (Activity-Counts) verwenden — keine Änderung am bestehenden Verhalten.
- **FR-008**: Das MetricsContext-Interface im Visualizer MUSS um `workflowCounts: Record<string, number>` erweitert werden; der Hook `useActivityMetrics` (oder ein Nachfolger) MUSS beide Felder zurückgeben.
- **FR-009**: Wenn `workflowCounts` für einen Node-Typ kein passendes Ergebnis liefert (kein Key-Match), MUSS das Badge „–" (unavailable) anzeigen — kein „0".
- **FR-010**: Alle bestehenden Anforderungen aus Feature 003 (Graceful Degradation, Loading-State, Race-Condition-Schutz, Zahlenformatierung) MÜSSEN für die neuen Node-Typen ebenfalls gelten.

### Key Entities

- **WorkerMetrics** (`WorkerMetrics.cs`): Bestehende Klasse — wird um einen `WorkflowExecutions`-Counter erweitert.
- **Workflow Execution Counter**: Neuer OpenTelemetry-Counter `temporal_workflow_task_completed{workflow_type="..."}` im Worker.
- **Metrics Proxy Route** (`activity-counts.ts` / `prometheus-client.ts`): Wird erweitert, um zusätzlich Workflow-Counts abzufragen und im Response-Body zurückzugeben.
- **MetricsContext / useActivityMetrics**: Visualizer-Hook — wird um `workflowCounts` erweitert.
- **ChildWorkflowNode / SubflowNode**: Nodes im Visualizer — lesen jetzt aus `workflowCounts`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Auf ChildWorkflow-Nodes zeigt der Visualizer innerhalb von 3 Sekunden nach Seitenladen korrekte Ausführungszähler an (sofern Temporal und Prometheus erreichbar sind und mindestens ein Scrape-Intervall vergangen ist).
- **SC-002**: Der Worker-Metrics-Endpoint enthält nach Abschluss eines Workflows einen `temporal_workflow_task_completed_total{workflow_type="..."}` Counter-Eintrag.
- **SC-003**: Das API-Response des Metrics Proxy enthält sowohl `counts` (Activities) als auch `workflowCounts` (Workflows) als separate Objekte.
- **SC-004**: Activity Nodes behalten ihr bisheriges Verhalten unverändert — keine Regression.
- **SC-005**: Nodes ohne passenden Metric-Key (kein Match in `workflowCounts`) zeigen „–" statt „0".

## Assumptions

- Der Workflow-Typ-Name, der als `workflow_type`-Label im Counter verwendet wird, entspricht dem einfachen Klassennamen ohne Namespace (z. B. `RiskEnrichmentWorkflow`, nicht `B2B.RiskService.Workflows.RiskEnrichmentWorkflow`).
- Dieser Name stimmt 1:1 mit dem `workflowRef`-Feld im WorkflowAPI-Dokument überein (z. B. `CalculateRiskWorkflow`). Der `graph-to-reactflow.ts`-Adapter muss `node.raw.workflowRef` als `workflowRef`-Feld in `WorkflowNodeData` weiterleiten. Das bisherige `activityType`-Feld bleibt unverändert (nur für Activities gesetzt). Falls `workflowRef` nicht vorhanden ist (z. B. manuell erstellte Nodes), bleibt das Badge leer ("–") — kein Mapping-Mechanismus.
- Der bestehende Metrics-Proxy-Endpoint `/api/activity-counts` wird rückwärtskompatibel erweitert (neues Feld `workflowCounts` hinzugefügt); bestehende Clients, die nur `counts` lesen, sind nicht betroffen.
- Alle drei Worker-Workflows (RiskEnrichmentWorkflow, CalculateRiskWorkflow, ExternalChecksWorkflow) erhalten den Aufruf von `WorkerMetrics.RecordWorkflowExecution(...)` am Ende ihrer `RunAsync`-Methode (und in allen catch/cancel-Pfaden — siehe Clarifications).
- Der Prometheus-Scrape-Intervall beträgt weiterhin ~15 Sekunden. Neue Counters erscheinen erst nach dem ersten Scrape in Prometheus.
- Step-Nodes (StepNode) haben kein direktes Temporal-Pendant und werden in dieser Iteration nicht mit Daten versorgt. Sie zeigen weiterhin „–".

## Clarifications

### Session 2026-06-18

- Q: Welcher Key soll für den Metric-Lookup in ChildWorkflow-Nodes verwendet werden — `workflowRef`, Node-Label oder lokale Node-ID? → A: `workflowRef` aus dem WorkflowAPI-Dokument (z. B. `"CalculateRiskWorkflow"`). Der Adapter gibt ihn als eigenes Feld `workflowRef` in `nodeData` weiter. Das `activityType`-Feld bleibt ausschließlich für Activity-Nodes reserviert.
- Q: Was soll `temporal_workflow_task_completed` zählen — nur erfolgreiche Completions oder alle terminalen Zustände? → A: Alle terminalen Zustände (Completed, Failed, Cancelled) — zeigt Gesamtdurchsatz. Counter wird am Ende von `RunAsync` und in allen catch/cancel-Pfaden inkrementiert.
- Q: Soll das Badge auf dem Trigger-Node (`ChildWorkflowNode`) allein erscheinen oder auch auf dem Gruppen-Container (`ChildWorkflowGroupNode`)? → A: Nur auf dem Trigger-Node. Der Gruppen-Container zeigt kein Badge.

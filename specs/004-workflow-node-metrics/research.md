# Research: Workflow Node Execution Metrics

**Feature**: 004-workflow-node-metrics | **Date**: 2026-06-18

## 1. Workflow-Counter-Mechanismus im .NET Worker

**Decision**: `try/finally`-Block in jeder `[WorkflowRun]`-Methode

**Rationale**:
- Temporalio führt `[WorkflowRun]`-Methoden deterministisch aus. Eine Exception, die aus der Methode entkommt, terminiert den Workflow als `Failed`.
- Eine `CancellationToken`-Abbruch führt zu einem `OperationCanceledException` oder einem expliziten Signal-Pfad (wie in `RiskEnrichmentWorkflow` via `cancelRequested`-Flag).
- `try/finally` stellt sicher, dass der Counter in **allen** drei Fällen (Completed, Failed, Cancelled) inkrementiert wird, unabhängig vom Pfad.
- Alternative „AOP via Interceptor" wurde verworfen: Temporalio Interceptors sind für Activity-Interceptors gut dokumentiert, Workflow-Interceptors sind komplexer und nicht nötig für einen einfachen Counter.

**Alternatives Considered**:
- Temporal SDK Workflow Interceptor: würde außerhalb der Workflow-Klassen arbeiten, aber erhöhte Komplexität ohne Mehrwert.
- Middleware/Decorator Pattern: nicht nötig bei nur 3 Workflows.

**Implementation Pattern**:
```csharp
[WorkflowRun]
public async Task<RiskEnrichmentResult> RunAsync(RiskEnrichmentRequest request)
{
    try
    {
        // ... existing workflow logic ...
        return result;
    }
    finally
    {
        WorkerMetrics.RecordWorkflowExecution(nameof(RiskEnrichmentWorkflow));
    }
}
```

## 2. OpenTelemetry Counter-Benennung

**Decision**: `temporal_workflow_task_completed` mit Label `workflow_type`

**Rationale**:
- Konsistenz mit dem bestehenden Activity-Counter `temporal_activity_task_completed` — gleiche Namenskonvention.
- Prometheus-Exporter hängt automatisch `_total` an (OpenTelemetry .NET Prometheus-Konvention), sodass der Metric-Name in Prometheus `temporal_workflow_task_completed_total` lautet.
- Label `workflow_type` ist analog zu `activity_type` — einheitliches Label-Schema.

**Alternatives Considered**:
- `workflow_execution_completed`: kein Temporal-Präfix, inkonsistent mit Feature 003.
- `temporal_workflow_run_completed`: `run` vs `task` — `task` ist konsistenter mit dem SDK-Vokabular.

## 3. Proxy API-Erweiterung — Rückwärtskompatibilität

**Decision**: Neues optionales Feld `workflowCounts` im bestehenden `/api/activity-counts`-Response

**Rationale**:
- Kein neuer Endpoint nötig — beide Zählertypen gehören zum gleichen Kontext (Metrics für einen Zeitraum).
- Rückwärtskompatibel: bestehender Visualizer-Code, der nur `counts` liest, läuft unverändert weiter.
- Kein Versionierungsbedarf für einen Breaking Change.

**Alternatives Considered**:
- Neuer Endpoint `/api/workflow-counts`: würde zwei separate Requests erfordern (erhöhte Latenz, komplexerer Client-Code).
- Kombination in `counts` unter anderem Key-Namespace: birgt Kollisionsgefahr bei gleichen Namen.

## 4. Parallel-Queries im Proxy

**Decision**: `Promise.all([queryActivityCounts(...), queryWorkflowCounts(...)])`

**Rationale**:
- Beide PromQL-Queries sind unabhängig voneinander — sequenzielles Ausführen würde die Proxy-Latenz verdoppeln.
- Bei Ausfall von Prometheus schlägt `Promise.all` als Ganzes fehl → bestehender Error-Handling-Pfad greift unverändert.

## 5. Metric-Key im Visualizer — `workflowRef` als einziger zuverlässiger Key

**Decision**: `workflowRef` aus `node.raw` (z. B. `"CalculateRiskWorkflow"`)

**Rationale** (aus Code-Analyse):
- `ChildWorkflowNode` hat **kein** `activityType` gesetzt (nur für Activity-Nodes).
- `node.label` ist der Anzeigename (`"Calculate risk"`) — stimmt nicht mit dem Klassennamen überein.
- `node.raw.workflowRef` (aus dem YAML: `workflowRef: CalculateRiskWorkflow`) entspricht exakt dem C#-Klassennamen, der als `workflow_type`-Label emittiert wird.
- Der Adapter `graph-to-reactflow.ts` muss `node.raw.workflowRef` als `workflowRef`-Feld in `WorkflowNodeData` übertragen.

**Existing Code**:
```ts
// In graph-to-reactflow.ts, beim Erstellen von childWorkflow-Nodes:
const workflowRef = node.raw && typeof node.raw === "object" && "workflowRef" in node.raw
  ? (node.raw.workflowRef as string)
  : undefined;
```

## 6. SubflowNode — Verhalten ohne `workflowRef`

**Decision**: Subflow-Nodes haben kein `workflowRef` (kein Temporal-Workflow-Pendant für Subflows in diesem Projekt) → Badge zeigt `–`

**Rationale**:
- In `risk-enrichment.workflowapi.yaml` sind Subflows via `subflowRef` referenziert, nicht via `workflowRef`.
- Es gibt keinen Temporal-Counter, der exakt einem Subflow-Namen entspricht.
- FR-006 schreibt vor, denselben `workflowCounts`-Lookup zu verwenden — da kein Key vorhanden ist, zeigt das Badge korrekt `–` (undefined in workflowCounts → ExecutionCountBadge zeigt `–`).
- Kein Workaround oder Mapping erforderlich.

## 7. Bestehende Infrastruktur — keine Änderungen

- Prometheus-Konfiguration: kein Update nötig — scrapt weiterhin den Worker-Metrics-Endpoint; neue Counter werden automatisch aufgenommen.
- Docker Compose: kein neuer Service.
- Prometheus-Scrape-Intervall: ~15 s (aus Feature 003).

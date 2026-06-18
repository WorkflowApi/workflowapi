# Data Model: Workflow Node Execution Metrics

**Feature**: 004-workflow-node-metrics | **Date**: 2026-06-18

## Änderungen im Überblick

Diese Feature erweitert bestehende Datenstrukturen — es werden keine neuen Entitäten eingeführt.

---

## 1. Worker: WorkerMetrics (C#)

**Datei**: `src/temporal-local/worker/Metrics/WorkerMetrics.cs`

```csharp
// Bestehend (unverändert)
public static readonly Counter<long> ActivityExecutions =
    Meter.CreateCounter<long>("temporal_activity_task_completed", ...);

public static void RecordActivityExecution(string activityType) => ...

// NEU
public static readonly Counter<long> WorkflowExecutions =
    Meter.CreateCounter<long>(
        "temporal_workflow_task_completed",
        description: "Total terminal workflow task executions (Completed, Failed, Cancelled)");

public static void RecordWorkflowExecution(string workflowType) =>
    WorkflowExecutions.Add(1, new KeyValuePair<string, object?>("workflow_type", workflowType));
```

**Prometheus-Metric-Name**: `temporal_workflow_task_completed_total{workflow_type="..."}`

**Gültige Werte für `workflow_type`**:
| Wert | Quelle |
|------|--------|
| `RiskEnrichmentWorkflow` | `nameof(RiskEnrichmentWorkflow)` |
| `CalculateRiskWorkflow` | `nameof(CalculateRiskWorkflow)` |
| `ExternalChecksWorkflow` | `nameof(ExternalChecksWorkflow)` |

---

## 2. Proxy: Prometheus-Client

**Datei**: `src/web/metrics-proxy/src/services/prometheus-client.ts`

```typescript
// Bestehend (unverändert)
export function buildActivityCountsQuery(range: TimeRange): string { ... }
export async function queryActivityCounts(...): Promise<Record<string, number>> { ... }

// NEU
export function buildWorkflowCountsQuery(range: TimeRange): string {
  return `sum by(workflow_type)(increase(temporal_workflow_task_completed_total[${range}]))`;
}

export async function queryWorkflowCounts(
  prometheusUrl: string,
  range: TimeRange,
): Promise<Record<string, number>> { ... }
// Gleiche Implementierung wie queryActivityCounts, aber mit workflow_type Label
```

---

## 3. Proxy: API-Response (rückwärtskompatibel)

**Datei**: `src/web/metrics-proxy/src/routes/activity-counts.ts`

```typescript
// Bisheriger Response
{
  range: "1d",
  counts: { "IdentifyCompanyActivity": 42, ... },
  timestamp: "2026-06-18T..."
}

// Erweiterter Response (NEU: workflowCounts)
{
  range: "1d",
  counts: { "IdentifyCompanyActivity": 42, ... },       // Activity-Counts (unverändert)
  workflowCounts: { "RiskEnrichmentWorkflow": 7, ... },  // NEU
  timestamp: "2026-06-18T..."
}
```

`workflowCounts` ist **immer** vorhanden (leeres Objekt `{}` wenn Prometheus nicht erreichbar).

---

## 4. Frontend: MetricsResponse

**Datei**: `src/web/workflow-react-flow-visualizer-mvp/src/services/metrics-client.ts`

```typescript
// Bestehend
export interface MetricsResponse {
  range: TimeRange;
  counts: Record<string, number>;
  timestamp: string;
  error?: string;
}

// Erweitert (NEU: workflowCounts)
export interface MetricsResponse {
  range: TimeRange;
  counts: Record<string, number>;
  workflowCounts: Record<string, number>;   // NEU — leeres Objekt wenn nicht vorhanden
  timestamp: string;
  error?: string;
}
```

---

## 5. Frontend: ActivityMetricsState

**Datei**: `src/web/workflow-react-flow-visualizer-mvp/src/hooks/useActivityMetrics.ts`

```typescript
// Bestehend
export interface ActivityMetricsState {
  counts: Record<string, number>;
  isLoading: boolean;
  isUnavailable: boolean;
  errorMessage?: string;
}

// Erweitert (NEU: workflowCounts)
export interface ActivityMetricsState {
  counts: Record<string, number>;
  workflowCounts: Record<string, number>;   // NEU
  isLoading: boolean;
  isUnavailable: boolean;
  errorMessage?: string;
}
```

**Default-State**: `workflowCounts: {}` (analog zu `counts`).

---

## 6. Frontend: WorkflowNodeData

**Datei**: `src/web/workflow-react-flow-visualizer-mvp/src/adapters/graph-to-reactflow.ts`

```typescript
// Bestehend
export interface WorkflowNodeData {
  label: string;
  description?: string;
  kind: string;
  id: string;
  localId?: string;
  activityType?: string;    // Nur für Activity-Nodes gesetzt
  [key: string]: unknown;
}

// Erweitert (NEU: workflowRef)
export interface WorkflowNodeData {
  label: string;
  description?: string;
  kind: string;
  id: string;
  localId?: string;
  activityType?: string;    // Nur für Activity-Nodes — unverändert
  workflowRef?: string;     // NEU — Nur für ChildWorkflow-Nodes gesetzt (z. B. "CalculateRiskWorkflow")
  [key: string]: unknown;
}
```

**Setzen von `workflowRef`** im Adapter (childWorkflow-Node-Pfad):
```typescript
const workflowRef =
  node.kind === "childWorkflow" &&
  node.raw &&
  typeof node.raw === "object" &&
  "workflowRef" in node.raw &&
  typeof node.raw.workflowRef === "string"
    ? node.raw.workflowRef
    : undefined;
```

---

## 7. Frontend: MetricsContext

**Datei**: `src/web/workflow-react-flow-visualizer-mvp/src/contexts/MetricsContext.ts`

Kein Interface-Wechsel erforderlich — `MetricsContext` nimmt `ActivityMetricsState` direkt ab. Da `ActivityMetricsState` um `workflowCounts` erweitert wird, steht `workflowCounts` automatisch via `useMetrics()` zur Verfügung.

**Default-State**: `{ counts: {}, workflowCounts: {}, isLoading: true, isUnavailable: false }`

---

## Validierungsregeln

| Feld | Regel |
|------|-------|
| `workflowCounts[key]` | Immer `≥ 0`, gerundet auf ganze Zahlen (wie `counts`) |
| `workflowRef` in `WorkflowNodeData` | Entspricht exakt dem C#-Klassennamen des Workflows (keine Namespace-Präfixe) |
| `workflowCounts` im Response | Immer vorhanden; leeres Objekt `{}` wenn Prometheus nicht erreichbar |

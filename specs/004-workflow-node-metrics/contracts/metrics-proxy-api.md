# Contract: Metrics Proxy API

**Feature**: 004-workflow-node-metrics | **Date**: 2026-06-18  
**Service**: `src/web/metrics-proxy`  
**Base URL**: `http://localhost:4000` (Development)

---

## GET /api/activity-counts

Liefert aggregierte Activity- und Workflow-Ausführungszähler für einen gewählten Zeitraum.

> Diese Erweiterung ist **rückwärtskompatibel**: bestehende Clients, die nur `counts` lesen, sind nicht betroffen.

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `range` | `"1h" \| "1d" \| "7d" \| "30d"` | No | `"1d"` | Auswertungszeitraum |

### Response: 200 OK

```json
{
  "range": "1d",
  "counts": {
    "IdentifyCompanyActivity": 42,
    "EnrichDnbActivity": 41,
    "ScoreCompanyRiskActivity": 38
  },
  "workflowCounts": {
    "RiskEnrichmentWorkflow": 14,
    "CalculateRiskWorkflow": 14,
    "ExternalChecksWorkflow": 14
  },
  "timestamp": "2026-06-18T09:00:00.000Z"
}
```

**Neu in Feature 004**: `workflowCounts` — Map von Workflow-Klassennamen auf Ausführungszähler.

| Feld | Type | Beschreibung |
|------|------|-------------|
| `range` | `string` | Zurückgespiegelter Zeitraum |
| `counts` | `Record<string, number>` | Activity-Ausführungszähler (unverändert) |
| `workflowCounts` | `Record<string, number>` | **NEU** — Workflow-Ausführungszähler; leeres Objekt `{}` wenn keine Daten |
| `timestamp` | `string` | ISO-8601-Zeitstempel der Abfrage |

### Response: 400 Bad Request

```json
{
  "error": "Invalid range parameter. Allowed values: 1h, 1d, 7d, 30d"
}
```

### Response: 503 Service Unavailable

Prometheus nicht erreichbar.

```json
{
  "range": "1d",
  "counts": {},
  "workflowCounts": {},
  "timestamp": "2026-06-18T09:00:00.000Z",
  "error": "Prometheus unreachable: connection refused"
}
```

**Wichtig**: Auch im 503-Fall enthält `workflowCounts` ein leeres Objekt (nie `undefined`).

---

## Prometheus Queries (intern)

| Query | Beschreibung |
|-------|-------------|
| `sum by(activity_type)(increase(temporal_activity_task_completed_total[{range}]))` | Activity-Counts (bestehend) |
| `sum by(workflow_type)(increase(temporal_workflow_task_completed_total[{range}]))` | **NEU** — Workflow-Counts |

Beide Queries werden **parallel** ausgeführt (`Promise.all`).

---

## Worker Metrics Endpoint

**URL**: `http://risk-worker:9090/metrics` (im Docker-Netz) / `http://localhost:9090/metrics` (lokal)

**Neu in Feature 004**: `temporal_workflow_task_completed_total`

```
# HELP temporal_workflow_task_completed_total Total terminal workflow task executions
# TYPE temporal_workflow_task_completed_total counter
temporal_workflow_task_completed_total{workflow_type="RiskEnrichmentWorkflow"} 14
temporal_workflow_task_completed_total{workflow_type="CalculateRiskWorkflow"} 14
temporal_workflow_task_completed_total{workflow_type="ExternalChecksWorkflow"} 14
```

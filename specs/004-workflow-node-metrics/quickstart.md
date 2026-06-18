# Quickstart / Validation Guide: Workflow Node Execution Metrics

**Feature**: 004-workflow-node-metrics | **Date**: 2026-06-18

## Voraussetzungen

- Docker und Docker Compose installiert
- Feature-Branch `004-workflow-node-metrics` ausgecheckt
- Alle Änderungen implementiert (Worker, Proxy, Visualizer)

## Stack starten

```bash
cd src
docker compose up --build -d
```

Warten bis alle Services healthy sind (~30 Sekunden):

```bash
docker compose ps
```

## Schritt 1: Worker-Counter verifizieren

Warte 30 Sekunden (mindestens 1 Simulator-Durchlauf + 1 Prometheus-Scrape-Intervall), dann:

```bash
curl -s http://localhost:9090/metrics | grep temporal_workflow
```

**Erwartetes Ergebnis**:
```
temporal_workflow_task_completed_total{workflow_type="RiskEnrichmentWorkflow"} 1
temporal_workflow_task_completed_total{workflow_type="CalculateRiskWorkflow"} 1
temporal_workflow_task_completed_total{workflow_type="ExternalChecksWorkflow"} 1
```

Wenn nichts erscheint: Simulator läuft noch nicht. Warten und erneut versuchen.

## Schritt 2: Metrics Proxy verifizieren

```bash
curl -s "http://localhost:4000/api/activity-counts?range=1h" | jq '{counts: .counts | keys, workflowCounts: .workflowCounts}'
```

**Erwartetes Ergebnis**:
```json
{
  "counts": ["AggregateRiskScoreActivity", "EnrichDnbActivity", ...],
  "workflowCounts": {
    "RiskEnrichmentWorkflow": 1,
    "CalculateRiskWorkflow": 1,
    "ExternalChecksWorkflow": 1
  }
}
```

Wenn `workflowCounts` leer `{}` ist: Prometheus hat den neuen Counter noch nicht gescrapt. Warte weitere 15 Sekunden.

## Schritt 3: Visualizer prüfen

Browser öffnen: `http://localhost:5173`

**Zu prüfen**:

1. **ChildWorkflow Trigger-Nodes** (`Calculate risk`, `External checks`): zeigen ein grünes Badge mit Zahl ≥ 1
2. **Activity Nodes**: zeigen weiterhin korrekte Zähler (keine Regression)
3. **Subflow-Nodes**: zeigen `–` (kein Temporal-Pendant — erwartetes Verhalten)
4. **Start/Stop-Nodes**: kein Badge

**Zeitraum wechseln**: Dropdown von „Letzter Tag" auf „Letzte Stunde" — ChildWorkflow-Badges aktualisieren sich.

## Schritt 4: Graceful Degradation testen

Prometheus stoppen:

```bash
docker compose stop prometheus
```

Visualizer im Browser neu laden — ChildWorkflow-Badges zeigen `–` (statt Zahlen). Activity-Badges ebenfalls `–`. Graph bleibt navigierbar.

Prometheus wieder starten:

```bash
docker compose start prometheus
```

Nach ~20 Sekunden (1 Poll-Intervall + 1 Scrape): Badges zeigen wieder Zahlen.

## Schritt 5: Fehlerfall — Worker-Counter in try/finally

Stoppe den Worker (simuliert Workflow-Fehler), starte ihn neu:

```bash
docker compose restart risk-worker
```

Warte 60 Sekunden (Simulator startet Workflows, einige könnten fehlschlagen beim Neustart). Counter-Wert in Prometheus sollte trotzdem steigen, da `try/finally` auch bei Fehlern greift.

## Schnellreferenz: Erwartete Badge-Zustände

| Node-Typ | Mit Daten | Ohne Match | Prometheus unavailable |
|----------|-----------|------------|----------------------|
| ActivityNode | Zahl (grün) | `–` | `–` |
| ChildWorkflowNode | Zahl (grün) | `–` | `–` |
| SubflowNode | `–` (kein Match) | `–` | `–` |
| StepNode | `–` (kein Pendant) | `–` | `–` |
| Start/Stop | kein Badge | — | — |

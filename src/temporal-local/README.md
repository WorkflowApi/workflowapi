# Local Temporal Environment

This package provides a local Temporal stack for `examples/risk-enrichment.workflowapi.yaml`.

## Prerequisites

- Docker Engine 24+
- Docker Compose v2

## Quick start

```bash
cd src/temporal-local
docker compose up --build
```

Services:

- Temporal gRPC: `localhost:7233`
- Temporal UI: `http://localhost:8233`
- Namespace: `B2B.RiskService`
- Task queue: `risk-enrichment`

## Start a workflow manually

```bash
docker compose exec temporal temporal workflow start \
  --namespace B2B.RiskService \
  --task-queue risk-enrichment \
  --type RiskEnrichmentWorkflow \
  --input '{"companyId":"test-001","country":"DE","companyName":"Test GmbH"}'
```

## Teardown

```bash
docker compose down
docker compose down -v
```

## Environment variables

| Variable | Default | Purpose |
|---|---|---|
| `TEMPORAL_VERSION` | `1.29.2` | Temporal server Docker image tag |
| `TEMPORAL_UI_VERSION` | `2.51.0` | Temporal UI Docker image tag |
| `POSTGRES_VERSION` | `16` | PostgreSQL Docker image tag |
| `TEMPORAL_NAMESPACE` | `B2B.RiskService` | Namespace used by server and worker |
| `TEMPORAL_TASK_QUEUE` | `risk-enrichment` | Worker task queue |
| `SIMULATOR_ENABLED` | `true` | Enables/disables workload simulator |
| `SIMULATOR_INTERVAL_SECONDS` | `5` | Start interval between simulated workflows |
| `ACTIVITY_FAILURE_RATE` | `0.10` | Failure probability for selected activities |
| `ACTIVITY_MIN_DELAY_MS` | `500` | Global minimum simulated activity delay |
| `ACTIVITY_MAX_DELAY_MS` | `2000` | Global maximum simulated activity delay |

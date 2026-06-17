# Local Temporal Environment

This package provides a local Temporal stack for `examples/risk-enrichment.workflowapi.yaml`.

## Prerequisites

- Docker Engine 24+
- Docker Compose v2

## Quick start

```bash
cd src
docker compose up --build
```

The compose entrypoint is `src/docker-compose.yml`.

Services:

- Temporal gRPC: `localhost:7233`
- Temporal UI: `http://localhost:8233`
- Workflow Visualizer: `http://localhost:3000` (or `http://localhost:${VISUALIZER_PORT}`)
- Metrics Proxy: `http://localhost:4000` (or `http://localhost:${METRICS_PROXY_PORT}`)
- Prometheus UI: `http://localhost:9090` (or `http://localhost:${PROMETHEUS_PORT}`)
- Worker metrics endpoint: `http://localhost:9091/metrics` (or `http://localhost:${METRICS_PORT}/metrics`)
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
| `VISUALIZER_PORT` | `3000` | Host port for Workflow Visualizer (`must not be 7233/8233`) |
| `METRICS_PROXY_PORT` | `4000` | Host port for Metrics Proxy API |
| `PROMETHEUS_PORT` | `9090` | Host port for Prometheus UI |
| `METRICS_PORT` | `9091` | Host port mapped to worker metrics endpoint (`/metrics`) |

Create local runtime values with:

```bash
cp .env.example .env
```

## Troubleshooting

- **Port conflict (`VISUALIZER_PORT`)**: Choose a free host port in `src/.env`, then restart:
  `docker compose up --build -d workflow-visualizer`
- **No activity counts**: Verify metrics pipeline health:
  - `curl http://localhost:${METRICS_PORT:-9091}/metrics | grep temporal_activity_task_completed_total`
  - `curl http://localhost:${METRICS_PROXY_PORT:-4000}/api/activity-counts?range=1h`
- **Visualizer build failure**: Inspect `docker compose logs workflow-visualizer` for TypeScript or npm errors and rebuild after fixing the source.

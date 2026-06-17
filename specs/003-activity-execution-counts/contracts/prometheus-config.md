# Contract: Prometheus Configuration

**Service**: `prometheus`
**Image**: `prom/prometheus:v3.4.1`
**Internal URL**: `http://prometheus:9090`

## Scrape Configuration

File: `src/prometheus/prometheus.yml`

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: "risk-worker"
    static_configs:
      - targets: ["risk-worker:9090"]
    metrics_path: /metrics
    scrape_interval: 5s
```

**Notes**:
- The Worker exposes metrics on port `9090` via `OpenTelemetry.Exporter.Prometheus.HttpListener`
- Scrape interval is 5 seconds (shorter than global) to support the 2-second frontend polling with reasonable freshness
- The target uses the Docker Compose service name `risk-worker`

## Docker Compose Service

```yaml
prometheus:
  image: prom/prometheus:v3.4.1
  container_name: prometheus
  volumes:
    - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
  ports:
    - "${PROMETHEUS_PORT:-9090}:9090"
  networks:
    - temporal-local
  healthcheck:
    test: ["CMD", "wget", "--spider", "-q", "http://localhost:9090/-/healthy"]
    interval: 10s
    timeout: 5s
    retries: 5
    start_period: 10s
  restart: unless-stopped
```

## Worker Metrics Endpoint

The .NET Worker exposes Prometheus-format metrics at:
- **Internal**: `http://risk-worker:9090/metrics`
- **Port**: 9090 (configurable via `METRICS_PORT` env var)

### Expected Metrics

| Metric Name | Type | Labels | Description |
|-------------|------|--------|-------------|
| `temporal_activity_task_completed_total` | Counter | `activity_type` | Total completed activity executions |

### Example Scrape Output

```
# HELP temporal_activity_task_completed_total Total completed activity task executions
# TYPE temporal_activity_task_completed_total counter
temporal_activity_task_completed_total{activity_type="IdentifyCompanyActivity"} 142
temporal_activity_task_completed_total{activity_type="EnrichDnbActivity"} 138
temporal_activity_task_completed_total{activity_type="ScoreCompanyRiskActivity"} 130
temporal_activity_task_completed_total{activity_type="AggregateRiskScoreActivity"} 128
temporal_activity_task_completed_total{activity_type="PublishRiskResultActivity"} 125
temporal_activity_task_completed_total{activity_type="FetchPaymentHistoryActivity"} 130
temporal_activity_task_completed_total{activity_type="FetchCreditLimitActivity"} 129
temporal_activity_task_completed_total{activity_type="NormaliseRiskSignalsActivity"} 127
temporal_activity_task_completed_total{activity_type="SanctionsCheckActivity"} 120
temporal_activity_task_completed_total{activity_type="PoliticallyExposedPersonCheckActivity"} 118
temporal_activity_task_completed_total{activity_type="AdverseMediaCheckActivity"} 115
temporal_activity_task_completed_total{activity_type="GeneratePdfActivity"} 122
```

## Retention

Default Prometheus retention (15 days) is sufficient for the "last month" (30d) time range. For accurate 30d queries, configure:

```yaml
# In docker-compose command override (if needed):
command:
  - "--storage.tsdb.retention.time=45d"
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `PROMETHEUS_PORT` | `9090` | Host-exposed port for Prometheus UI (optional debugging) |

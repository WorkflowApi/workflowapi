# Contract: Activity Metrics Proxy API

**Service**: `metrics-proxy`
**Base URL**: `http://localhost:${METRICS_PROXY_PORT:-4000}`
**Protocol**: HTTP/JSON

## Endpoints

### GET /api/activity-counts

Returns aggregated activity execution counts for the specified time range.

**Query Parameters**:

| Parameter | Type | Required | Default | Values |
|-----------|------|----------|---------|--------|
| `range` | string | No | `1d` | `1h`, `1d`, `7d`, `30d` |

**Success Response** (`200 OK`):

```json
{
  "range": "1h",
  "counts": {
    "IdentifyCompanyActivity": 42,
    "EnrichDnbActivity": 38,
    "ScoreCompanyRiskActivity": 35,
    "AggregateRiskScoreActivity": 35,
    "PublishRiskResultActivity": 34,
    "GeneratePdfActivity": 33,
    "FetchPaymentHistoryActivity": 35,
    "FetchCreditLimitActivity": 35,
    "NormaliseRiskSignalsActivity": 34,
    "SanctionsCheckActivity": 30,
    "PoliticallyExposedPersonCheckActivity": 29,
    "AdverseMediaCheckActivity": 28
  },
  "timestamp": "2026-06-17T14:00:00.000Z"
}
```

**Fields**:
- `range`: The time range that was queried (echo of request parameter)
- `counts`: Object mapping `activity_type` → integer execution count. Values are rounded from PromQL float results. Missing activities (no data in range) are omitted.
- `timestamp`: ISO 8601 UTC timestamp of when the query was executed

**Error Response — Prometheus unreachable** (`503 Service Unavailable`):

```json
{
  "range": "1h",
  "counts": {},
  "timestamp": "2026-06-17T14:00:00.000Z",
  "error": "Prometheus unreachable: ECONNREFUSED"
}
```

**Error Response — Invalid range parameter** (`400 Bad Request`):

```json
{
  "error": "Invalid range parameter. Allowed values: 1h, 1d, 7d, 30d"
}
```

### GET /health

Health check endpoint for Docker Compose.

**Success Response** (`200 OK`):

```json
{ "status": "ok" }
```

## CORS

The proxy MUST set CORS headers to allow requests from the Visualizer origin:

```
Access-Control-Allow-Origin: http://localhost:${VISUALIZER_PORT:-3000}
Access-Control-Allow-Methods: GET
Access-Control-Allow-Headers: Content-Type
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `PROMETHEUS_URL` | `http://prometheus:9090` | Prometheus server URL (internal Docker network) |
| `PORT` | `4000` | Port the proxy listens on |
| `CORS_ORIGIN` | `http://localhost:3000` | Allowed CORS origin |

## Internal Behavior

1. Parse `range` query parameter → validate against allowed set
2. Build PromQL query: `sum by(activity_type)(increase(temporal_activity_task_completed[${range}]))`
3. Call Prometheus HTTP API: `GET ${PROMETHEUS_URL}/api/v1/query?query=...`
4. Parse response → extract `metric.activity_type` and `value[1]` (float string)
5. Round float values to integers → build counts map
6. Return JSON response

**Timeout**: 5 seconds for Prometheus query. Return 503 on timeout.

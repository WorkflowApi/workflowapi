# Contract: Environment Variables

**Date**: 2026-06-17 | **Feature**: 001-local-temporal-environment

## Worker Container Environment Variables

These are the configurable environment variables for the `risk-worker` container.

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `TEMPORAL_ADDRESS` | Yes | `temporal:7233` | Temporal server gRPC endpoint |
| `TEMPORAL_NAMESPACE` | Yes | `B2B.RiskService` | Temporal namespace to use |
| `TEMPORAL_TASK_QUEUE` | Yes | `risk-enrichment` | Task queue to poll |
| `SIMULATOR_ENABLED` | No | `true` | Enable/disable workload simulator |
| `SIMULATOR_INTERVAL_SECONDS` | No | `5` | Seconds between simulated workflow starts |
| `ACTIVITY_FAILURE_RATE` | No | `0.10` | Probability of simulated activity failure (0.0-1.0) |
| `ACTIVITY_MIN_DELAY_MS` | No | `500` | Minimum simulated activity duration in ms |
| `ACTIVITY_MAX_DELAY_MS` | No | `2000` | Maximum simulated activity duration in ms |

## Temporal Server Environment Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `DB` | `postgres12` | Database driver |
| `DB_PORT` | `5432` | PostgreSQL port |
| `POSTGRES_USER` | `temporal` | Database username |
| `POSTGRES_PWD` | `temporal` | Database password |
| `POSTGRES_SEEDS` | `postgresql` | Database hostname (Docker service name) |
| `DEFAULT_NAMESPACE` | `B2B.RiskService` | Auto-created namespace on first boot |
| `BIND_ON_IP` | `0.0.0.0` | Bind to all interfaces (required in Docker) |
| `DYNAMIC_CONFIG_FILE_PATH` | `config/dynamicconfig/development-sql.yaml` | Dynamic config path |

## PostgreSQL Environment Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `POSTGRES_USER` | `temporal` | Superuser name |
| `POSTGRES_PASSWORD` | `temporal` | Superuser password |

## Temporal UI Environment Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `TEMPORAL_ADDRESS` | `temporal:7233` | Backend gRPC address |
| `TEMPORAL_CORS_ORIGINS` | `http://localhost:3000` | CORS origins |
| `TEMPORAL_NOTIFY_ON_NEW_VERSION` | `false` | Disable update banner |

## .env File (defaults for docker-compose)

```env
# src/temporal-local/.env
# Temporal Server
TEMPORAL_VERSION=1.31.1
TEMPORAL_UI_VERSION=2.51.0
POSTGRES_VERSION=16

# Worker Configuration
TEMPORAL_NAMESPACE=B2B.RiskService
TEMPORAL_TASK_QUEUE=risk-enrichment

# Simulator
SIMULATOR_ENABLED=true
SIMULATOR_INTERVAL_SECONDS=5

# Activity Simulation
ACTIVITY_FAILURE_RATE=0.10
ACTIVITY_MIN_DELAY_MS=500
ACTIVITY_MAX_DELAY_MS=2000
```

## Security Notes

- No external credentials required (FR-012)
- PostgreSQL password `temporal` is for local dev only — never use in production
- No TLS configured (local network only)
- No secrets in source code — all values are non-sensitive defaults for local development

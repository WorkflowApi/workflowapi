# Contract: Docker Compose Service Definitions

**Date**: 2026-06-17 | **Feature**: 001-local-temporal-environment

## Service Topology

```
┌─────────────────────────────────────────────────────────────┐
│                    docker-compose.yml                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────┐    ┌─────────────────┐    ┌──────────────┐   │
│  │PostgreSQL│◄───│ Temporal Server  │◄───│  Temporal UI │   │
│  │  :5432   │    │     :7233        │    │    :8233     │   │
│  └──────────┘    └────────┬─────────┘    └──────────────┘   │
│                           │                                  │
│                           ▼                                  │
│                  ┌─────────────────┐                         │
│                  │   Risk Worker   │                         │
│                  │ (worker + sim)  │                         │
│                  └─────────────────┘                         │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Services

### postgresql

| Property | Value |
|----------|-------|
| Image | `postgres:16` |
| Internal Port | 5432 |
| Host Port | Not exposed (internal only) |
| Volumes | Anonymous volume for data (ephemeral) |
| Health Check | `pg_isready -U temporal` |
| Restart Policy | `unless-stopped` |

### temporal

| Property | Value |
|----------|-------|
| Image | `temporalio/auto-setup:1.31.1` |
| Internal Port | 7233 (gRPC) |
| Host Port | 7233 |
| Depends On | `postgresql` (healthy) |
| Health Check | `nc -z localhost 7233` |
| Restart Policy | `unless-stopped` |
| Namespace | `B2B.RiskService` (auto-created) |

### temporal-ui

| Property | Value |
|----------|-------|
| Image | `temporalio/ui:2.51.0` |
| Internal Port | 8080 |
| Host Port | **8233** (per FR-010) |
| Depends On | `temporal` (healthy) |
| Health Check | HTTP GET on port 8080 |
| Restart Policy | `unless-stopped` |

### risk-worker

| Property | Value |
|----------|-------|
| Build Context | `./worker` |
| Dockerfile | `./worker/Dockerfile` |
| Depends On | `temporal` (healthy) |
| Health Check | Process alive (default) |
| Restart Policy | `on-failure:3` |
| Task Queue | `risk-enrichment` |

## Network

| Property | Value |
|----------|-------|
| Network Name | `temporal-local` |
| Driver | `bridge` |
| Scope | All services attached |

## Startup Order (enforced by health checks)

1. `postgresql` starts → becomes healthy when accepting connections
2. `temporal` starts → waits for PostgreSQL healthy, creates schema, creates namespace → becomes healthy on port 7233
3. `temporal-ui` starts → waits for Temporal healthy → serves UI on :8233
4. `risk-worker` starts → waits for Temporal healthy → connects to gRPC :7233, polls task queue

## docker-compose.yml Contract

```yaml
# src/temporal-local/docker-compose.yml
services:
  postgresql:
    image: postgres:16
    container_name: temporal-postgresql
    environment:
      POSTGRES_USER: temporal
      POSTGRES_PASSWORD: temporal
    volumes:
      - /var/lib/postgresql/data
    networks:
      - temporal-local
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U temporal"]
      interval: 5s
      timeout: 5s
      retries: 60
      start_period: 10s
    restart: unless-stopped

  temporal:
    image: temporalio/auto-setup:1.31.1
    container_name: temporal-server
    depends_on:
      postgresql:
        condition: service_healthy
    environment:
      - DB=postgres12
      - DB_PORT=5432
      - POSTGRES_USER=temporal
      - POSTGRES_PWD=temporal
      - POSTGRES_SEEDS=postgresql
      - DEFAULT_NAMESPACE=B2B.RiskService
      - BIND_ON_IP=0.0.0.0
      - DYNAMIC_CONFIG_FILE_PATH=config/dynamicconfig/development-sql.yaml
    ports:
      - "7233:7233"
    volumes:
      - ./dynamicconfig:/etc/temporal/config/dynamicconfig
    networks:
      - temporal-local
    healthcheck:
      test: ["CMD", "nc", "-z", "localhost", "7233"]
      interval: 5s
      timeout: 3s
      retries: 60
      start_period: 30s
    restart: unless-stopped

  temporal-ui:
    image: temporalio/ui:2.51.0
    container_name: temporal-ui
    depends_on:
      temporal:
        condition: service_healthy
    environment:
      - TEMPORAL_ADDRESS=temporal:7233
      - TEMPORAL_CORS_ORIGINS=http://localhost:3000
      - TEMPORAL_NOTIFY_ON_NEW_VERSION=false
    ports:
      - "8233:8080"
    networks:
      - temporal-local
    healthcheck:
      test: ["CMD", "wget", "--spider", "-q", "http://localhost:8080"]
      interval: 10s
      timeout: 5s
      retries: 30
      start_period: 10s
    restart: unless-stopped

  risk-worker:
    build:
      context: ./worker
      dockerfile: Dockerfile
    container_name: risk-worker
    depends_on:
      temporal:
        condition: service_healthy
    environment:
      - TEMPORAL_ADDRESS=temporal:7233
      - TEMPORAL_NAMESPACE=B2B.RiskService
      - TEMPORAL_TASK_QUEUE=risk-enrichment
      - SIMULATOR_ENABLED=true
      - SIMULATOR_INTERVAL_SECONDS=5
    networks:
      - temporal-local
    restart: on-failure:3

networks:
  temporal-local:
    driver: bridge
    name: temporal-local
```

## Dynamic Config Contract

```yaml
# src/temporal-local/dynamicconfig/development-sql.yaml
limit.maxIDLength:
  - value: 255
    constraints: {}
system.forceSearchAttributesCacheRefreshOnRead:
  - value: true
    constraints: {}
```

## Volumes

No named volumes — all data is ephemeral. `docker compose down` cleanly removes all state.

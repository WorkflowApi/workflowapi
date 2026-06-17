# Service Interfaces: Visualizer Docker Compose Integration

**Phase**: 1 — Design  
**Feature**: `002-visualizer-docker-compose`  
**Date**: 2026-06-17

This document defines the externally observable service interfaces exposed by the Docker Compose stack after this feature is implemented.

---

## Interface 1: Workflow Visualizer HTTP Endpoint

**Service**: `workflow-visualizer`  
**Protocol**: HTTP  
**Container port**: `80`  
**Host port**: `${VISUALIZER_PORT:-3000}` (default: `3000`)  
**URL**: `http://localhost:3000` (with default port)

### Contract

| Property | Value |
|---|---|
| Method | `GET` |
| Path | `/` (and all SPA sub-paths via `try_files` fallback) |
| Response Content-Type | `text/html` (index.html) |
| Response Status | `200 OK` |
| Static assets | `/assets/*` — JS bundles, CSS; served with `Content-Type` inferred by nginx |
| Availability | Ready when `workflow-visualizer` container health check passes |
| Authentication | None |
| CORS | Not configured (browser-only static SPA) |

### Invariants

- The endpoint serves only pre-built static files from `dist/`. No server-side rendering, no API proxy.
- All unknown paths return `index.html` (SPA fallback) with status `200`.
- No `proxy_pass` to Temporal or any other service.
- The service does NOT depend on Temporal being healthy; it starts independently.

---

## Interface 2: Temporal gRPC Endpoint (existing, unchanged)

**Service**: `temporal`  
**Protocol**: gRPC  
**Host port**: `7233`  
**URL**: `localhost:7233`

*Unchanged from current implementation. Documented here for completeness.*

---

## Interface 3: Temporal UI HTTP Endpoint (existing, unchanged)

**Service**: `temporal-ui`  
**Protocol**: HTTP  
**Host port**: `8233`  
**URL**: `http://localhost:8233`

*Unchanged from current implementation. Documented here for completeness.*

---

## Interface 4: Docker Compose Service Control

**Invocation path**: `src/` directory  
**Tool**: Docker Compose v2 (`docker compose`)

### Commands

| Command | Effect |
|---|---|
| `docker compose up --build` | Build all images and start all five services |
| `docker compose up --build -d` | Same, detached (background) |
| `docker compose down` | Stop and remove all containers |
| `docker compose down -v` | Stop containers and remove named/anonymous volumes |
| `docker compose ps` | Show status of all services |
| `docker compose logs workflow-visualizer` | Show visualizer container logs |
| `docker compose build workflow-visualizer` | Rebuild only the visualizer image |

### Service Startup Order

```
postgresql (healthy)
    └─▶ temporal (healthy)
            ├─▶ temporal-ui
            └─▶ risk-worker
workflow-visualizer  ◀── starts independently (no depends_on)
```

### Port Allocation Summary

| Port | Service | Protocol | Configurable |
|---|---|---|---|
| `3000` | `workflow-visualizer` | HTTP | ✅ `VISUALIZER_PORT` |
| `7233` | `temporal` | gRPC | ❌ |
| `8233` | `temporal-ui` | HTTP | ❌ |

---

## Interface 5: `workflow-visualizer` Container Health Check

**Purpose**: Verifies nginx is serving the SPA.

```
test: ["CMD", "wget", "--spider", "-q", "http://localhost:80"]
interval: 10s
timeout: 5s
retries: 3
start_period: 5s
```

**Expected states**:
- `starting` — container launched, within `start_period`
- `healthy` — `wget` exits 0 (nginx serving `index.html`)
- `unhealthy` — nginx not responding (build failure, crash)

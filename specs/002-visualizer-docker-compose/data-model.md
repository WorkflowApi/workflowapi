# Data Model: Visualizer Docker Compose Integration

**Phase**: 1 — Design  
**Feature**: `002-visualizer-docker-compose`  
**Date**: 2026-06-17

---

This feature is an infrastructure configuration change, not a domain data model change. The "entities" here are the Docker Compose service definitions, the environment configuration, and the Dockerfile build stages. They are documented below as the authoritative structural reference for implementation.

---

## Entity 1: `docker-compose.yml` (moved to `src/`)

**Location**: `src/docker-compose.yml`  
**Predecessor**: `src/temporal-local/docker-compose.yml` (to be deleted)

### Services

| Service Name | Image / Build | Ports | Depends On | Network | Health Check |
|---|---|---|---|---|---|
| `postgresql` | `postgres:${POSTGRES_VERSION:-16}` | — | — | `temporal-local` | `pg_isready -U temporal` |
| `temporal` | `temporalio/auto-setup:${TEMPORAL_VERSION:-1.29.2}` | `7233:7233` | `postgresql` (healthy) | `temporal-local` | `nc -z localhost 7233` |
| `temporal-ui` | `temporalio/ui:${TEMPORAL_UI_VERSION:-2.51.0}` | `8233:8080` | `temporal` (healthy) | `temporal-local` | `wget --spider -q http://localhost:8080` |
| `risk-worker` | build context `./temporal-local/worker` | — | `temporal` (healthy) | `temporal-local` | `ps -eo args \| grep dotnet RiskWorker.dll` |
| `workflow-visualizer` | build context `./web/workflow-react-flow-visualizer-mvp` | `${VISUALIZER_PORT:-3000}:80` | — | `temporal-local` | `wget --spider -q http://localhost:80` |

### Networks

| Name | Driver | External Name |
|---|---|---|
| `temporal-local` | `bridge` | `temporal-local` |

### Path Changes from Predecessor

| Field | Old value | New value |
|---|---|---|
| `temporal.volumes[0]` | `./dynamicconfig:/etc/temporal/config/dynamicconfig` | `./temporal-local/dynamicconfig:/etc/temporal/config/dynamicconfig` |
| `risk-worker.build.context` | `./worker` | `./temporal-local/worker` |
| `workflow-visualizer.build.context` | N/A (new) | `./web/workflow-react-flow-visualizer-mvp` |

---

## Entity 2: Environment Variables (`.env.example`)

**Location**: `src/.env.example`  
**Runtime file**: `src/.env` (gitignored)  
**Predecessor**: `src/temporal-local/.env` (to be deleted)

| Variable | Default | Service(s) | Purpose |
|---|---|---|---|
| `TEMPORAL_VERSION` | `1.29.2` | `temporal` | Temporal server Docker image tag |
| `TEMPORAL_UI_VERSION` | `2.51.0` | `temporal-ui` | Temporal UI Docker image tag |
| `POSTGRES_VERSION` | `16` | `postgresql` | PostgreSQL Docker image tag |
| `TEMPORAL_NAMESPACE` | `B2B.RiskService` | `temporal`, `risk-worker` | Namespace used by server and worker |
| `TEMPORAL_TASK_QUEUE` | `risk-enrichment` | `risk-worker` | Worker task queue |
| `SIMULATOR_ENABLED` | `true` | `risk-worker` | Enables/disables workload simulator |
| `SIMULATOR_INTERVAL_SECONDS` | `5` | `risk-worker` | Seconds between simulated workflow starts |
| `ACTIVITY_FAILURE_RATE` | `0.10` | `risk-worker` | Probability of activity failure in simulation |
| `ACTIVITY_MIN_DELAY_MS` | `500` | `risk-worker` | Minimum simulated activity delay |
| `ACTIVITY_MAX_DELAY_MS` | `2000` | `risk-worker` | Maximum simulated activity delay |
| `VISUALIZER_PORT` | `3000` | `workflow-visualizer` | Host port for the Workflow Visualizer UI |

**Validation rules**:
- `VISUALIZER_PORT` must not equal `8233` (Temporal UI) or `7233` (Temporal gRPC).
- `ACTIVITY_FAILURE_RATE` must be a float in `[0.0, 1.0]`.
- Image version variables must be non-empty strings.

---

## Entity 3: `workflow-visualizer` Dockerfile

**Location**: `src/web/workflow-react-flow-visualizer-mvp/Dockerfile`

### Build Stages

| Stage | Base Image | Purpose | Key Commands |
|---|---|---|---|
| `build` | `node:22-alpine` | Install deps + compile TypeScript + Vite bundle | `npm ci`, `npm run build` |
| `serve` | `nginx:1.27-alpine` | Serve static `dist/` files | `COPY --from=build /app/dist /usr/share/nginx/html` |

### Build Stage (`build`) — Fields

| Field | Value |
|---|---|
| `WORKDIR` | `/app` |
| Layer 1 | `COPY package.json package-lock.json ./` |
| Layer 2 | `RUN npm ci` |
| Layer 3 | `COPY . .` |
| Layer 4 | `RUN npm run build` |
| Output artefact | `/app/dist/` |

### Serve Stage (`serve`) — Fields

| Field | Value |
|---|---|
| `WORKDIR` | `/usr/share/nginx/html` |
| Source copy | `COPY --from=build /app/dist .` |
| nginx config | Custom `default.conf` (see Entity 4) |
| `EXPOSE` | `80` |

---

## Entity 4: nginx Configuration

**Embedded in**: `src/web/workflow-react-flow-visualizer-mvp/Dockerfile` (via `RUN printf` or `COPY nginx.conf`)  
**Overrides**: `/etc/nginx/conf.d/default.conf`

### Configuration Fields

| Directive | Value | Rationale |
|---|---|---|
| `listen` | `80` | Container-internal port; host port mapped via Compose |
| `root` | `/usr/share/nginx/html` | Vite `dist/` output location |
| `index` | `index.html` | Vite output entry point |
| `try_files` | `$uri $uri/ /index.html` | SPA fallback; ensures direct URL navigation works |
| `gzip` | `on` (JS, CSS, HTML, JSON, SVG) | Compress text assets for faster transfer |
| `proxy_pass` | — (absent) | App makes no runtime backend calls |

---

## Entity 5: `src/temporal-local/README.md` (updated)

**Location**: `src/temporal-local/README.md`  
**Change**: Update `cd src/temporal-local` → `cd src` in Quick Start section. Add note that `docker-compose.yml` has moved to `src/`.

### Fields (updated)

| Field | Old value | New value |
|---|---|---|
| Quick start `cd` path | `cd src/temporal-local` | `cd src` |
| Compose invocation | `docker compose up --build` | `docker compose up --build` (unchanged) |
| Note about compose file | (absent) | Add: "The `docker-compose.yml` file is located at `src/docker-compose.yml`." |

---

## Relationships

```
src/docker-compose.yml
  ├── uses: src/temporal-local/dynamicconfig/  (volume mount)
  ├── builds: src/temporal-local/worker/       (risk-worker build context)
  └── builds: src/web/workflow-react-flow-visualizer-mvp/  (workflow-visualizer build context)
        └── Dockerfile (multi-stage)
              ├── Stage: build  (node:22-alpine)
              └── Stage: serve  (nginx:1.27-alpine, custom default.conf)

src/.env  (runtime, gitignored)
  └── src/.env.example  (committed, all defaults)
        └── consumed by: src/docker-compose.yml (all 5 services)
```

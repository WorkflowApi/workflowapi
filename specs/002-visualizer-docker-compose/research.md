# Research: Visualizer Docker Compose Integration

**Phase**: 0 — Pre-design research  
**Feature**: `002-visualizer-docker-compose`  
**Date**: 2026-06-17

---

## R-001: Multi-Stage Dockerfile for Vite/React → nginx

**Decision**: Two-stage Dockerfile: Stage 1 (`node:22-alpine`) runs `npm ci && npm run build` (TypeScript compile + Vite bundle → `dist/`). Stage 2 (`nginx:1.27-alpine`) copies `dist/` into `/usr/share/nginx/html/` and serves it statically.

**Rationale**:
- Multi-stage builds keep the final image small (~50MB for nginx:alpine vs ~1GB for a node image with full deps).
- `node:22-alpine` is the Active LTS release (as of June 2026); `22-alpine` is smaller than `22-bullseye` and aligns with the spec requirement.
- `nginx:1.27-alpine` is the latest stable minor pinned in the spec; Alpine variant is consistent with the Node base image choice and keeps the runtime image minimal.
- `npm ci` (not `npm install`) is used in the build stage because `package-lock.json` exists, ensuring reproducible installs.

**Alternatives considered**:
- *Vite Dev Server in container*: Rejected (explicitly excluded by spec; hot-reload in prod container is an anti-pattern for this use case).
- *Caddy or serve (npx)*: Rejected in favour of nginx; nginx is production-grade, well-understood, and has zero JS runtime overhead.
- `node:22` (Debian): Larger image (~400MB uncompressed); Alpine provides equivalent functionality at ~170MB uncompressed in the build stage.

**Resolved**: No NEEDS CLARIFICATION. All image tags are pinned in the spec.

---

## R-002: nginx Static SPA Serving — Configuration Requirements

**Decision**: Use the nginx default configuration (`/etc/nginx/conf.d/default.conf`) with a custom `server` block that:
1. Listens on port `80` (container-internal).
2. Sets `root /usr/share/nginx/html`.
3. Adds `index index.html`.
4. Adds `try_files $uri $uri/ /index.html` to support client-side React Router navigation if any routes are added in the future.
5. Enables `gzip` for JS/CSS assets.

**Rationale**:
- The Vite build output (`dist/`) contains only static files; no backend proxy is needed.
- `try_files ... /index.html` is the standard SPA pattern; without it, direct URL navigation to sub-routes returns 404.
- The visualizer currently has no client-side router but adding this rule is zero-cost insurance.
- A custom `nginx.conf` is NOT needed at the `/etc/nginx/nginx.conf` level; overriding only `conf.d/default.conf` keeps the change minimal.

**Alternatives considered**:
- *No custom nginx config (pure default)*: The default nginx `location /` does not include `try_files`, which would break SPA navigation. Minimal override required.
- *`proxy_pass` to Temporal gRPC*: Explicitly excluded by spec (app makes no runtime backend calls).

**Resolved**: A minimal `nginx.conf` is embedded in the Dockerfile via `COPY` or `RUN` (preferred: inline `RUN printf ...` or a separate `nginx.conf` file in the build context).

---

## R-003: Docker Compose `.env` Variable Interpolation for Port Configuration

**Decision**: Add `VISUALIZER_PORT` to a new `src/.env.example` file with default value `3000`. The existing `src/temporal-local/.env` (with Temporal/DB variables) is merged into `src/.env.example` and the runtime `.env` moves to `src/.env`. Docker Compose v2 automatically loads `src/.env` when `docker compose` is run from `src/`.

**Rationale**:
- Docker Compose v2 auto-loads `.env` from the directory where `docker compose` is invoked. Since the Compose file moves to `src/docker-compose.yml`, the `.env` file must also live at `src/.env`.
- `.env` is gitignored (security baseline); `.env.example` is committed as the canonical reference.
- The existing `src/temporal-local/.env` contains non-secret runtime tuning parameters (no credentials); they are merged into the new `src/.env.example` alongside `VISUALIZER_PORT`.
- Variable syntax in Compose: `"${VISUALIZER_PORT:-3000}:80"` provides a default even without a `.env` file, satisfying the "no manual steps after git clone" requirement (SC-001).

**Alternatives considered**:
- *Keep `.env` in `src/temporal-local/`*: Would require `--env-file` flag on every `docker compose` invocation; breaks the "single command" UX goal.
- *Hardcode port 3000 in Compose*: Violates FR-007 (port must be configurable).

**Resolved**: New `src/.env.example` merges all variables; `src/.env` is the runtime file (gitignored); `src/temporal-local/.env` is removed.

---

## R-004: Relative Path Adjustments After Compose File Move

**Decision**: When `docker-compose.yml` moves from `src/temporal-local/` to `src/`, all paths relative to the old location must be updated:

| Reference | Old path (relative to `src/temporal-local/`) | New path (relative to `src/`) |
|---|---|---|
| `risk-worker` build context | `./worker` | `./temporal-local/worker` |
| `risk-worker` Dockerfile | `Dockerfile` (within context) | unchanged (within context) |
| `temporal` dynamic config volume | `./dynamicconfig` | `./temporal-local/dynamicconfig` |
| Visualizer build context | N/A | `./web/workflow-react-flow-visualizer-mvp` |

**Rationale**: Docker Compose resolves all relative paths against the directory of the Compose file. After the move, references that were `./worker` (correct from `src/temporal-local/`) become invalid at `src/` unless updated to `./temporal-local/worker`.

**Resolved**: All four path adjustments are required and documented.

---

## R-005: Visualizer Service Health Check Strategy

**Decision**: Use a lightweight `wget --spider -q http://localhost:80` health check inside the visualizer container (same pattern as `temporal-ui` in the existing Compose file). No `depends_on` on Temporal services since the visualizer has no runtime dependency on them (spec assumption explicitly confirmed).

**Rationale**:
- nginx exposes port 80 inside the container; `wget` is available in `nginx:1.27-alpine` out of the box.
- No `depends_on: temporal` is needed: the static app loads workflow data from bundled example files, not from a live Temporal connection.
- Not having `depends_on` on Temporal also means the visualizer starts immediately without waiting for the ~30s Temporal startup sequence, improving perceived startup time.

**Alternatives considered**:
- *`curl` health check*: `curl` is not included in nginx:alpine by default; `wget` is (via BusyBox).
- *No health check*: Acceptable but reduces observability; the existing services all have health checks; consistency warrants adding one.

**Resolved**: `wget --spider -q http://localhost:80` health check, `interval: 10s`, `timeout: 5s`, `retries: 3`, `start_period: 5s`.

---

## R-006: Vite Build Output Directory

**Decision**: Vite's default output directory is `dist/` (confirmed in `vite.config.ts` — no custom `build.outDir` is set). The nginx Dockerfile stage copies from `dist/` in the build stage.

**Rationale**: `vite.config.ts` shows `defineConfig({ plugins: [react()] })` with no `build` override. Vite default is `dist/`. No configuration change needed.

**Resolved**: Dockerfile `COPY --from=build /app/dist /usr/share/nginx/html`.

---

## Summary of All Decisions

| ID | Decision | Status |
|----|----------|--------|
| R-001 | Two-stage Dockerfile: `node:22-alpine` + `nginx:1.27-alpine` | ✅ Resolved |
| R-002 | Custom nginx `default.conf` with `try_files` for SPA, no `proxy_pass` | ✅ Resolved |
| R-003 | `src/.env.example` merges all variables; `VISUALIZER_PORT` defaults to `3000` | ✅ Resolved |
| R-004 | Four path adjustments required in Compose file after move | ✅ Resolved |
| R-005 | `wget` health check for visualizer; no `depends_on` on Temporal | ✅ Resolved |
| R-006 | Vite outputs to `dist/`; no config change needed | ✅ Resolved |

**All NEEDS CLARIFICATION items resolved. Phase 1 can proceed.**

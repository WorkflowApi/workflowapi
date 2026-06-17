# Quickstart Validation Guide: Visualizer Docker Compose Integration

**Phase**: 1 — Design  
**Feature**: `002-visualizer-docker-compose`  
**Date**: 2026-06-17

This guide documents how to validate that the feature is correctly implemented and all acceptance scenarios pass. It is not a full implementation guide — refer to `data-model.md` and `contracts/` for detailed specifications.

---

## Prerequisites

- Docker Engine 24+ installed and running
- Docker Compose v2 (`docker compose version` → `v2.x`)
- Git (repository cloned, `002-visualizer-docker-compose` branch checked out)
- No process already using ports `3000`, `7233`, or `8233` on the host

Verify Docker availability:
```bash
docker --version       # Docker version 24+
docker compose version # Docker Compose version v2.x
```

---

## Setup

```bash
# From the repository root
cd src

# (Optional) Copy and customise environment
cp .env.example .env
# Edit .env if port 3000 is in use on your machine: set VISUALIZER_PORT=3001
```

No other setup is required (no `npm install`, no local toolchain needed).

---

## Scenario 1 — Full stack starts with a single command (SC-001, FR-006)

```bash
# From src/
docker compose up --build
```

**Expected outcome**:
- Docker builds the `workflow-visualizer` image (first run takes ~60s; subsequent runs use layer cache).
- All five services appear in the startup log: `postgresql`, `temporal`, `temporal-ui`, `risk-worker`, `workflow-visualizer`.
- No fatal errors in the compose output.

**Verify all services are healthy**:
```bash
docker compose ps
```

Expected `STATUS` for each service:

| Service | Expected STATUS |
|---|---|
| `postgresql` | `healthy` |
| `temporal` | `healthy` |
| `temporal-ui` | `healthy` |
| `risk-worker` | `healthy` or `running` |
| `workflow-visualizer` | `healthy` |

---

## Scenario 2 — Visualizer is reachable in browser (SC-002, FR-004, User Story 1)

After all services are healthy (default visualizer port `3000`):

```bash
# Quick smoke test (without browser)
curl -s -o /dev/null -w "%{http_code}" http://localhost:3000/
# Expected: 200
```

Or open `http://localhost:3000` in a browser — the React flow visualizer UI should render within 60 seconds of `docker compose up`.

---

## Scenario 3 — Existing services are unaffected (SC-003, FR-002, User Story 2)

```bash
# Temporal gRPC is reachable
docker compose exec temporal temporal workflow list --namespace B2B.RiskService
# Expected: no connection error

# Temporal UI is reachable
curl -s -o /dev/null -w "%{http_code}" http://localhost:8233/
# Expected: 200

# Risk worker is processing (start a workflow to confirm)
docker compose exec temporal temporal workflow start \
  --namespace B2B.RiskService \
  --task-queue risk-enrichment \
  --type RiskEnrichmentWorkflow \
  --input '{"companyId":"test-001","country":"DE","companyName":"Test GmbH"}'
# Expected: workflow starts successfully, appears in Temporal UI at http://localhost:8233
```

---

## Scenario 4 — Compose file lives at `src/docker-compose.yml` (FR-001, FR-008, User Story 2)

```bash
# From repository root
# Verify new location exists
ls src/docker-compose.yml
# Expected: file found

# Verify old location is gone
ls src/temporal-local/docker-compose.yml
# Expected: No such file or directory
```

---

## Scenario 5 — Port is configurable via `.env` (SC-004, FR-007)

```bash
# From src/ — change the port
cp .env.example .env
sed -i.bak 's/^VISUALIZER_PORT=.*/VISUALIZER_PORT=3001/' .env && rm .env.bak

# Restart only the visualizer
docker compose up --build -d workflow-visualizer

# Verify new port
curl -s -o /dev/null -w "%{http_code}" http://localhost:3001/
# Expected: 200

# Verify old port is no longer bound
curl -s -o /dev/null -w "%{http_code}" http://localhost:3000/ 2>/dev/null
# Expected: connection refused (or timeout)
```

---

## Scenario 6 — Teardown stops all services (User Story 1, Scenario 3)

```bash
# From src/
docker compose down
docker compose ps
# Expected: no running containers
```

---

## Scenario 7 — Visualizer starts independently (FR-005, spec assumption)

```bash
# Start only the visualizer (no Temporal dependency)
docker compose up --build workflow-visualizer

# Expected: container starts and reaches healthy state
# without waiting for postgresql/temporal/temporal-ui
docker compose ps workflow-visualizer
# STATUS: healthy
```

---

## Edge Case Validation

### Build failure (visualizer TypeScript error)

Manually introduce a TypeScript error in `src/web/workflow-react-flow-visualizer-mvp/src/App.tsx`, then:
```bash
docker compose up --build workflow-visualizer
```
**Expected**: `workflow-visualizer` exits with a non-zero code. Other services (`temporal`, `risk-worker`, etc.) continue running unaffected.

### Port conflict

Set `VISUALIZER_PORT=8233` in `src/.env`, then:
```bash
docker compose up --build workflow-visualizer
```
**Expected**: Docker Compose reports a port binding error. The other services continue running.

---

## Reference

- Service ports and health check specs: [`contracts/service-interfaces.md`](./contracts/service-interfaces.md)
- Environment variable defaults and validation: [`contracts/env-variables.md`](./contracts/env-variables.md)
- Full entity and file structure: [`data-model.md`](./data-model.md)

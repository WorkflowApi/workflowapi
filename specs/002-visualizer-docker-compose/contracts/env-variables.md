# Environment Variables Contract

**Phase**: 1 — Design  
**Feature**: `002-visualizer-docker-compose`  
**Date**: 2026-06-17

This document is the authoritative reference for all environment variables consumed by `src/docker-compose.yml`. It defines the full content of `src/.env.example` and the validation rules for each variable.

---

## `.env.example` — Canonical Content

```dotenv
# =============================================================================
# WorkflowAPI Local Stack — Environment Variables
# Copy this file to .env and adjust values as needed.
# src/.env is gitignored and must not be committed.
# =============================================================================

# --- Temporal Server ---
TEMPORAL_VERSION=1.29.2
TEMPORAL_UI_VERSION=2.51.0
POSTGRES_VERSION=16

# --- Namespace & Task Queue ---
TEMPORAL_NAMESPACE=B2B.RiskService
TEMPORAL_TASK_QUEUE=risk-enrichment

# --- Risk Worker Simulator ---
SIMULATOR_ENABLED=true
SIMULATOR_INTERVAL_SECONDS=5
ACTIVITY_FAILURE_RATE=0.10
ACTIVITY_MIN_DELAY_MS=500
ACTIVITY_MAX_DELAY_MS=2000

# --- Workflow Visualizer ---
# Default port: 3000. Change if 3000 is already in use on your machine.
# Must not conflict with Temporal UI (8233) or Temporal gRPC (7233).
VISUALIZER_PORT=3000
```

---

## Variable Reference

| Variable | Type | Default | Consumed By | Validation |
|---|---|---|---|---|
| `TEMPORAL_VERSION` | string | `1.29.2` | `temporal` service image tag | Non-empty; valid semver recommended |
| `TEMPORAL_UI_VERSION` | string | `2.51.0` | `temporal-ui` service image tag | Non-empty; valid semver recommended |
| `POSTGRES_VERSION` | string | `16` | `postgresql` service image tag | Non-empty |
| `TEMPORAL_NAMESPACE` | string | `B2B.RiskService` | `temporal` (DEFAULT_NAMESPACE), `risk-worker` (TEMPORAL_NAMESPACE) | Non-empty; no special characters |
| `TEMPORAL_TASK_QUEUE` | string | `risk-enrichment` | `risk-worker` (TEMPORAL_TASK_QUEUE) | Non-empty |
| `SIMULATOR_ENABLED` | boolean | `true` | `risk-worker` (SIMULATOR_ENABLED) | `true` or `false` |
| `SIMULATOR_INTERVAL_SECONDS` | integer | `5` | `risk-worker` (SIMULATOR_INTERVAL_SECONDS) | Positive integer |
| `ACTIVITY_FAILURE_RATE` | float | `0.10` | `risk-worker` (ACTIVITY_FAILURE_RATE) | Float in `[0.0, 1.0]` |
| `ACTIVITY_MIN_DELAY_MS` | integer | `500` | `risk-worker` (ACTIVITY_MIN_DELAY_MS) | Non-negative integer; ≤ `ACTIVITY_MAX_DELAY_MS` |
| `ACTIVITY_MAX_DELAY_MS` | integer | `2000` | `risk-worker` (ACTIVITY_MAX_DELAY_MS) | Non-negative integer; ≥ `ACTIVITY_MIN_DELAY_MS` |
| `VISUALIZER_PORT` | integer | `3000` | `workflow-visualizer` port mapping | Integer in `[1024, 65535]`; ≠ `7233`, ≠ `8233` |

---

## Precedence and Defaults

Docker Compose v2 variable resolution order (highest to lowest priority):

1. Shell environment variables (set before running `docker compose`)
2. `src/.env` file (auto-loaded when `docker compose` is run from `src/`)
3. Inline defaults in `docker-compose.yml` (e.g., `${VISUALIZER_PORT:-3000}`)

The inline defaults in `docker-compose.yml` ensure the stack starts correctly even without a `.env` file (requirement: single command after `git clone`).

---

## Files Affected

| File | Action | Description |
|---|---|---|
| `src/.env.example` | CREATE | New canonical template; all defaults documented above |
| `src/.env` | CREATE (gitignored, per-developer) | Runtime values; not committed |
| `src/temporal-local/.env` | DELETE | Superseded by `src/.env.example` |
| `.gitignore` | VERIFY/UPDATE | Ensure `src/.env` is gitignored (pattern `src/.env` or `**/.env`) |

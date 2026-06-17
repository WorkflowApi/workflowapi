# Quickstart: Activity Execution Counts

**Feature**: 003-activity-execution-counts
**Date**: 2026-06-17

## Prerequisites

- Docker and Docker Compose installed
- Repository cloned: `git clone <repo> && cd workflowapi`
- No local processes using ports 3000, 4000, 7233, 8233, 9090

## Start the Stack

```bash
cd src/
docker compose up --build
```

**Expected**: All services start (postgresql, temporal, temporal-ui, risk-worker, workflow-visualizer, prometheus, metrics-proxy). The simulator generates workflow executions every 5 seconds.

## Validation Scenarios

### Scenario 1: Badges appear on Activity Nodes

1. Open browser: `http://localhost:3000`
2. Wait 3 seconds for initial data load

**Expected**: Each Activity Node in the graph displays a small badge with a number (or "0" if just started). Non-activity nodes (start, end, bridges) do NOT show badges.

### Scenario 2: Time Range Selector works

1. Locate the time range selector (top of visualizer)
2. Switch from "Letzter Tag" to "Letzte Stunde"

**Expected**: Badge values update within 2 seconds. Values for "Letzte Stunde" should be ≤ values for "Letzter Tag".

### Scenario 3: Auto-Refresh (2 seconds)

1. Watch any Activity Node badge
2. Wait 10+ seconds while the simulator runs

**Expected**: Badge values increment as new workflow executions complete. Updates happen silently (no full-page reload, no flicker).

### Scenario 4: Graceful Degradation

1. Stop the worker: `docker compose stop risk-worker`
2. Observe the visualizer

**Expected**: Graph remains fully visible and navigable. Badges continue to show last known values or "–" if Prometheus goes stale.

3. Stop Prometheus: `docker compose stop prometheus`
4. Wait for next poll cycle (2 seconds)

**Expected**: Badges show "–" (unavailable state). No errors in browser console beyond failed fetch.

5. Restart everything: `docker compose up -d risk-worker prometheus`
6. Wait 5 seconds

**Expected**: Badges recover and show current values again.

### Scenario 5: Large Numbers Format

1. Let the simulator run for extended time OR check with dev tools:
   - Open browser DevTools → Console
   - Run: `fetch('http://localhost:4000/api/activity-counts?range=30d').then(r=>r.json()).then(console.log)`

2. Verify the proxy returns integer counts

**Expected**: If count > 1000, badge shows compact format (e.g., "1.2k"). Hovering over badge shows full number in tooltip.

### Scenario 6: Prometheus UI (Debug)

1. Open browser: `http://localhost:9090`
2. Query: `temporal_activity_task_completed_total`

**Expected**: Prometheus shows the raw counter values with `activity_type` labels for all registered activities.

## Verify Individual Services

### Worker Metrics Endpoint

```bash
curl http://localhost:9090/metrics 2>/dev/null | grep temporal_activity_task_completed
```

**Note**: Port 9090 is shared with Prometheus on the host. Use Docker exec for direct worker check:
```bash
docker exec risk-worker curl -s http://localhost:9090/metrics | grep temporal_activity
```

### Metrics Proxy

```bash
curl http://localhost:4000/api/activity-counts?range=1h
```

**Expected**: JSON response with `counts` object containing activity type keys and integer values.

### Metrics Proxy Health

```bash
curl http://localhost:4000/health
```

**Expected**: `{"status":"ok"}`

## Cleanup

```bash
cd src/
docker compose down -v
```

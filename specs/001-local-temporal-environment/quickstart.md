# Quickstart: Local Temporal Environment Validation

**Date**: 2026-06-17 | **Feature**: 001-local-temporal-environment

## Prerequisites

- Docker Engine 24+ with Docker Compose v2
- ~4 GB RAM available for containers
- Ports 7233 and 8233 available on localhost
- No internet access required beyond initial image pulls

## Start the Environment

```bash
cd src/temporal-local/
docker compose up --build
```

**Expected**: All 4 services start and reach healthy state within 60 seconds:
- `temporal-postgresql` — healthy
- `temporal-server` — healthy  
- `temporal-ui` — healthy
- `risk-worker` — running

## Validation Scenarios

### Scenario 1: Verify Environment Health (SC-001, User Story 1)

**Steps**:
```bash
# Check all containers are healthy
docker compose ps

# Verify Temporal server is accepting connections
docker compose exec temporal temporal operator namespace describe B2B.RiskService
```

**Expected output**:
- All containers show `healthy` or `running` status
- Namespace `B2B.RiskService` is described with retention period and active state

### Scenario 2: Verify Temporal UI (FR-010, User Story 1 AC-2)

**Steps**:
1. Open browser: `http://localhost:8233`
2. Select namespace `B2B.RiskService` from the dropdown
3. Navigate to "Task Queues" and find `risk-enrichment`

**Expected**:
- Temporal UI loads and shows the namespace selector
- `B2B.RiskService` namespace is visible
- `risk-enrichment` task queue shows at least 1 registered worker

### Scenario 3: Verify Workflow Execution (User Story 2)

**Steps**:
```bash
# Start a workflow manually via Temporal CLI
docker compose exec temporal temporal workflow start \
  --namespace B2B.RiskService \
  --task-queue risk-enrichment \
  --type RiskEnrichmentWorkflow \
  --input '{"companyId":"test-001","country":"DE","companyName":"Test GmbH"}'

# Wait for completion (should complete within 30 seconds)
docker compose exec temporal temporal workflow list \
  --namespace B2B.RiskService \
  --query 'ExecutionStatus="Completed"'
```

**Expected**:
- Workflow starts successfully and returns a workflow ID
- Workflow completes within 30 seconds with status `Completed`
- Result contains `companyId`, `riskClass` (one of: low, medium, high), and `status: completed`

### Scenario 4: Verify GetStatus Query (FR-005, User Story 2 AC-2)

**Steps**:
```bash
# Start a workflow and immediately query it
WORKFLOW_ID=$(docker compose exec temporal temporal workflow start \
  --namespace B2B.RiskService \
  --task-queue risk-enrichment \
  --type RiskEnrichmentWorkflow \
  --input '{"companyId":"query-test","country":"US"}' \
  --output json | jq -r '.workflowId')

# Query status while running
docker compose exec temporal temporal workflow query \
  --namespace B2B.RiskService \
  --workflow-id "$WORKFLOW_ID" \
  --type GetStatus
```

**Expected**:
- Query returns `RiskEnrichmentStatus` with `currentStep` (e.g., "enriching") and `updatedAt` timestamp

### Scenario 5: Verify Workload Simulator (FR-007, User Story 3)

**Steps**:
1. Wait 2 minutes after `docker compose up`
2. Open Temporal UI at `http://localhost:8233`
3. Navigate to `B2B.RiskService` → Workflows

**Expected**:
- At least 20 workflow executions visible (1 every 5 seconds × 120 seconds)
- Mix of `Running` and `Completed` statuses
- Different `companyId` values in each workflow input
- Different `riskClass` values in completed workflows (low, medium, high)

### Scenario 6: Verify Retry Behaviour (FR-004a)

**Steps**:
1. Open Temporal UI at `http://localhost:8233`
2. Navigate to a completed workflow's history
3. Look for activities with multiple attempts

**Expected**:
- Some activities (EnrichDnb, GeneratePdf, SanctionsCheck, PEPCheck, AdverseMedia) show retry attempts
- Retries are visible in the event history with exponential backoff
- All retried activities eventually succeed (within 5 attempts)

### Scenario 7: Verify Cancel Signal (FR-006, Edge Case)

**Steps**:
```bash
# Start a workflow
WORKFLOW_ID=$(docker compose exec temporal temporal workflow start \
  --namespace B2B.RiskService \
  --task-queue risk-enrichment \
  --type RiskEnrichmentWorkflow \
  --input '{"companyId":"cancel-test","country":"GB"}' \
  --output json | jq -r '.workflowId')

# Send cancel signal
docker compose exec temporal temporal workflow signal \
  --namespace B2B.RiskService \
  --workflow-id "$WORKFLOW_ID" \
  --name Cancel \
  --input '{"reason":"Testing cancellation"}'

# Check final status
docker compose exec temporal temporal workflow describe \
  --namespace B2B.RiskService \
  --workflow-id "$WORKFLOW_ID"
```

**Expected**:
- Workflow transitions to `Cancelled` or `Completed` state
- GetStatus query shows `state: cancelled`

### Scenario 8: Verify Worker Recovery (SC-005, Edge Case)

**Steps**:
```bash
# Note currently running workflows
docker compose exec temporal temporal workflow list \
  --namespace B2B.RiskService \
  --query 'ExecutionStatus="Running"'

# Restart worker container
docker compose restart risk-worker

# Wait 10 seconds, then check workflows resumed
sleep 10
docker compose exec temporal temporal workflow list \
  --namespace B2B.RiskService \
  --query 'ExecutionStatus="Running" OR ExecutionStatus="Completed"'
```

**Expected**:
- Worker recovers within 10 seconds
- Previously running workflows resume from their last checkpoint
- No workflow data loss

## Teardown

```bash
# Stop and remove all containers and networks
cd src/temporal-local/
docker compose down

# Remove volumes too (full cleanup)
docker compose down -v
```

**Expected**: All containers, networks, and volumes cleanly removed. No orphaned resources.

## Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| Port 7233 in use | Another Temporal instance | `lsof -i :7233` and stop conflicting process |
| Port 8233 in use | Another service on that port | Change port mapping in docker-compose.yml |
| Worker fails to connect | Temporal not ready | Wait for `temporal-server` health check to pass |
| No workflows appearing | Simulator disabled | Check `SIMULATOR_ENABLED=true` in environment |
| OOM errors | Insufficient Docker memory | Increase Docker memory to 4+ GB |

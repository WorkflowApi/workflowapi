# Research: Local Temporal Environment

**Date**: 2026-06-17 | **Feature**: 001-local-temporal-environment

## 1. Temporal Server Docker Configuration

### Decision: Use `temporalio/auto-setup:1.31.1` for single-image simplicity

**Rationale**: While the new recommended approach (v1.30+) separates `temporalio/server` + `temporalio/admin-tools`, the legacy `auto-setup` image is still published at `1.31.1` and provides a simpler single-container experience for local development. Since this is ephemeral dev tooling (not production), the convenience of auto-setup outweighs the separation benefit.

**Alternatives considered**:
- `temporalio/server` + `temporalio/admin-tools` (new pattern): More production-like but adds complexity with separate schema-setup containers and `service_completed_successfully` dependencies. Rejected for local dev simplicity.
- `temporalio/temporal:latest` with SQLite (dev mode): Too minimal — no visibility search, no UI customization, no PostgreSQL for realistic testing.

### Key Configuration

| Component | Image | Version |
|-----------|-------|---------|
| Temporal Server | `temporalio/auto-setup` | `1.31.1` |
| Temporal UI | `temporalio/ui` | `2.51.0` |
| PostgreSQL | `postgres` | `16` |
| Worker | Custom (.NET 10) | N/A |

### Ports

| Port | Service | Purpose |
|------|---------|---------|
| 7233 | Temporal Server | Frontend gRPC (SDK connections) |
| 8233 | Temporal UI | Web UI (mapped from container 8080) |
| 5432 | PostgreSQL | Database (internal only, not exposed to host) |

### Namespace Creation

The `auto-setup` image supports `DEFAULT_NAMESPACE=B2B.RiskService` environment variable for automatic namespace creation on first boot. The namespace name with dots is valid per Temporal's naming rules.

## 2. Temporal .NET SDK

### Decision: Use `Temporalio` 1.15.0 with Generic Host pattern

**Rationale**: The Generic Host pattern (`Temporalio.Extensions.Hosting`) provides built-in DI, graceful shutdown, and `IHostedService` lifecycle management. It's the recommended production pattern and naturally supports running the workload simulator as a `BackgroundService` alongside the worker.

**Alternatives considered**:
- Raw `TemporalWorker` without host: Simpler code but loses DI, logging configuration, and graceful shutdown handling. Rejected because the workload simulator benefits from `BackgroundService` lifecycle.
- Separate simulator container: Would require a second .NET project and Docker image. Rejected per YAGNI — one container can host both worker and simulator.

### NuGet Packages

```xml
<PackageReference Include="Temporalio" Version="1.15.0" />
<PackageReference Include="Temporalio.Extensions.Hosting" Version="1.15.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
```

### Key SDK Patterns

- **Workflows**: Decorated with `[Workflow]`, entry point `[WorkflowRun]`
- **Activities**: Decorated with `[Activity]`, can be instance or static methods
- **Signals**: `[WorkflowSignal]` async Task methods (e.g., `CancelAsync`)
- **Queries**: `[WorkflowQuery]` synchronous non-void methods (e.g., `GetStatus()`)
- **Child workflows**: `Workflow.ExecuteChildWorkflowAsync<T>()`
- **Random in activities**: `Random.Shared` is safe (only workflow code must be deterministic)
- **Random in workflows**: `Workflow.Random` provides deterministic replay-safe randomness

## 3. .NET Worker Dockerfile

### Decision: Multi-stage build with `mcr.microsoft.com/dotnet/runtime:10.0`

**Rationale**: The worker is a pure console application (no HTTP endpoints), so `runtime:10.0` is sufficient and smaller than `aspnet:10.0`. Multi-stage build keeps the final image minimal.

**Alternatives considered**:
- Single-stage build: Larger image (includes SDK). Rejected for production best practices.
- `aspnet:10.0`: Includes ASP.NET Core runtime, unnecessary for a worker. Rejected for image size.
- Self-contained publish: Larger image but no runtime dependency. Rejected — the base image is reliable and well-maintained.

### Dockerfile Pattern

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "RiskWorker.dll"]
```

## 4. Workload Simulation Patterns

### Decision: `BackgroundService` in same container with configurable interval

**Rationale**: A `BackgroundService` alongside the hosted worker keeps infrastructure simple (one container, one image) while providing independent lifecycle control. The simulator is enabled by default but can be disabled via environment variable.

**Alternatives considered**:
- Separate Docker container: More isolated but doubles build/image overhead. Rejected per YAGNI.
- Temporal scheduled workflow: Adds Temporal-specific complexity for a dev tool. Rejected.
- Simple `Task.Run` loop: Loses graceful shutdown and structured logging. Rejected.

### Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `SIMULATOR_ENABLED` | `true` | Enable/disable workload simulator |
| `SIMULATOR_INTERVAL_SECONDS` | `5` | Seconds between workflow starts |

### Mock Data Strategy

- Company IDs: Random GUIDs
- Countries: Randomly selected from `["DE", "US", "GB", "FR", "NL", "CH"]`
- Company names: Randomly selected from a pool of 20 realistic company names
- Risk outcomes: Determined by simulated scoring logic (score 0-100: low <30, medium 30-70, high >70)

## 5. Activity Failure Simulation

### Decision: ~10% `ApplicationFailureException` on bridge and external-check activities

**Rationale**: `ApplicationFailureException` with `nonRetryable: false` triggers Temporal's retry mechanism naturally. The 10% rate is high enough to see retries in the UI but low enough that workflows still complete reliably with default retry policies.

**Activities with failure simulation** (bridges + external checks):
- `EnrichDnbActivity` (bridge)
- `GeneratePdfActivity` (bridge)
- `SanctionsCheckActivity` (external check)
- `PoliticallyExposedPersonCheckActivity` (external check)
- `AdverseMediaCheckActivity` (external check)

**Retry policy for failing activities**:
```csharp
RetryPolicy = new RetryPolicy
{
    MaximumAttempts = 5,
    InitialInterval = TimeSpan.FromMilliseconds(500),
    BackoffCoefficient = 2.0f,
    MaximumInterval = TimeSpan.FromSeconds(10),
}
```

## 6. Health Checks

### Decision: TCP port checks for Temporal, `pg_isready` for PostgreSQL, HTTP for worker

**Rationale**: Docker Compose health checks with `depends_on: condition: service_healthy` ensure proper startup order. TCP checks are the simplest reliable approach for Temporal server readiness.

| Service | Health Check | Interval | Retries |
|---------|-------------|----------|---------|
| PostgreSQL | `pg_isready -U temporal` | 5s | 60 |
| Temporal Server | `nc -z localhost 7233` (TCP) | 5s | 60 |
| Temporal UI | HTTP GET on port 8080 | 10s | 30 |
| Worker | Process alive (default Docker) | 10s | 3 |

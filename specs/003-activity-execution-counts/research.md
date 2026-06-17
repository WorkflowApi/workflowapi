# Research: Activity Execution Counts

**Feature**: 003-activity-execution-counts
**Date**: 2026-06-17

## R1: Data Source for Activity Counts

**Decision**: OpenTelemetry/Prometheus Metrics from Worker SDK

**Rationale**: The .NET Worker can export custom metrics via `System.Diagnostics.Metrics` and OpenTelemetry's Prometheus HttpListener exporter. This approach:
- Provides exact per-activity-type counters natively labeled
- Supports native time-range aggregation via PromQL (`increase()` function)
- Scales independently of workflow execution volume
- Requires no Temporal Visibility API availability guarantees
- Is backend-agnostic (Prometheus locally, Datadog in production)

**Alternatives considered**:
- Temporal `CountActivityExecutions` API: Potentially unavailable in self-hosted v1.29.2; experimental status
- Workflow History Events: O(n) API calls per workflow execution; unacceptable for polling every 2 seconds

---

## R2: .NET Worker Metrics Export (No ASP.NET Core)

**Decision**: Use `OpenTelemetry.Exporter.Prometheus.HttpListener` package

**Rationale**: The Worker uses `Host.CreateApplicationBuilder` (not `WebApplication.CreateBuilder`), so it has no ASP.NET Core HTTP pipeline. The `Prometheus.HttpListener` exporter starts a lightweight `System.Net.HttpListener` on a configurable port without requiring Kestrel or ASP.NET Core middleware.

**Implementation approach**:
```csharp
// NuGet packages:
// - OpenTelemetry.Extensions.Hosting
// - OpenTelemetry.Exporter.Prometheus.HttpListener

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("WorkflowApi.Temporal.RiskWorker")
            .AddPrometheusHttpListener(options =>
            {
                options.UriPrefixes = new[] { "http://+:9090/" };
            });
    });
```

**Custom counter definition**:
```csharp
using System.Diagnostics.Metrics;

internal static class WorkerMetrics
{
    public static readonly Meter Meter = new("WorkflowApi.Temporal.RiskWorker", "1.0.0");
    public static readonly Counter<long> ActivityExecutions =
        Meter.CreateCounter<long>("temporal_activity_task_completed",
            description: "Total completed activity task executions");
}

// In each activity:
WorkerMetrics.ActivityExecutions.Add(1, new("activity_type", "IdentifyCompanyActivity"));
```

**Alternatives considered**:
- `OpenTelemetry.Exporter.Prometheus.AspNetCore`: Requires ASP.NET Core hosting (not applicable)
- OTLP Exporter → Prometheus Remote Write: More complex, adds OTEL Collector as intermediate

---

## R3: PromQL Time-Range Aggregation

**Decision**: Use `increase()` function with time-range selectors

**Rationale**: PromQL's `increase(metric[duration])` calculates the total increase of a counter over the given duration. This maps directly to "number of executions in time range".

**Query patterns**:
```promql
# Last hour — all activity types
sum by(activity_type)(increase(temporal_activity_task_completed[1h]))

# Last day
sum by(activity_type)(increase(temporal_activity_task_completed[1d]))

# Last week
sum by(activity_type)(increase(temporal_activity_task_completed[7d]))

# Last month (30 days)
sum by(activity_type)(increase(temporal_activity_task_completed[30d]))
```

**Note**: `increase()` returns a floating-point value (interpolated). Round to integer in the proxy before returning to frontend.

**Alternatives considered**:
- `rate()` × duration: Same result, more code
- `count_over_time()`: Only works for gauges, not counters

---

## R4: Frontend Polling Pattern

**Decision**: Custom React hook with `setInterval` + `AbortController`

**Rationale**: Standard pattern for 2-second polling in React. AbortController prevents race conditions when:
- The component unmounts mid-request
- A new interval tick fires before the previous request completes
- The time range changes while a request is in-flight

**Implementation approach**:
```typescript
function usePollingFetch<T>(url: string, interval: number = 2000) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<Error | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    let mounted = true;
    const fetchData = async () => {
      abortRef.current?.abort();
      abortRef.current = new AbortController();
      try {
        const res = await fetch(url, { signal: abortRef.current.signal });
        const json = await res.json();
        if (mounted) setData(json);
      } catch (err) {
        if (mounted && (err as Error).name !== "AbortError") setError(err as Error);
      }
    };
    fetchData(); // initial
    const id = setInterval(fetchData, interval);
    return () => { mounted = false; clearInterval(id); abortRef.current?.abort(); };
  }, [url, interval]);

  return { data, error };
}
```

**Alternatives considered**:
- SWR/React Query: Adds dependency for a single polling use case; overkill
- WebSocket: Higher complexity, Prometheus doesn't support push natively
- Server-Sent Events: More infrastructure; polling is simpler for 2s intervals

---

## R5: Metrics Proxy Architecture

**Decision**: Node.js/Express (TypeScript) with single endpoint

**Rationale**: Lightweight, same language as frontend (TypeScript throughout), minimal container size with Alpine base. Single responsibility: translate time-range parameter to PromQL, query Prometheus HTTP API, return JSON.

**API Design**:
```
GET /api/activity-counts?range=1h|1d|7d|30d

Response:
{
  "range": "1h",
  "counts": {
    "IdentifyCompanyActivity": 42,
    "EnrichDnbActivity": 38,
    "ScoreCompanyRiskActivity": 35,
    ...
  },
  "timestamp": "2026-06-17T14:00:00Z"
}
```

**Prometheus HTTP API call** (from proxy):
```
GET http://prometheus:9090/api/v1/query?query=sum by(activity_type)(increase(temporal_activity_task_completed[1h]))
```

**Alternatives considered**:
- .NET Minimal API: Heavier container, different language from frontend
- Go: Smallest binary, but introduces new language to project
- Direct browser → Prometheus: Constitution violation (no direct external access from browser)

---

## R6: Number Formatting (Compact)

**Decision**: Custom utility with suffixed compact format

**Rationale**: International, language-neutral format. Fits in limited badge space.

**Implementation**:
```typescript
function formatCompact(n: number): string {
  if (n < 1000) return n.toString();
  if (n < 1_000_000) return (n / 1000).toFixed(1).replace(/\.0$/, '') + 'k';
  return (n / 1_000_000).toFixed(1).replace(/\.0$/, '') + 'M';
}
// 0 → "0", 999 → "999", 1000 → "1k", 12483 → "12.5k", 1200000 → "1.2M"
```

**Alternatives considered**:
- `Intl.NumberFormat` with `notation: "compact"`: Locale-dependent output (may show "Tsd." in DE)
- Always full number: Doesn't fit in badge at high volumes

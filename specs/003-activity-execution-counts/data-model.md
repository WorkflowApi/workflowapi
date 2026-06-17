# Data Model: Activity Execution Counts

**Feature**: 003-activity-execution-counts
**Date**: 2026-06-17

## Entities

### 1. ActivityMetric

Represents the execution count for a single activity type within a time range.

| Field | Type | Description |
|-------|------|-------------|
| activityType | string | Activity type name (matches WorkflowAPI `activityRef` and Prometheus `activity_type` label) |
| count | integer | Number of completed executions in the selected time range |
| range | TimeRange | The time range this count covers |

**Validation**:
- `activityType` must be a non-empty string
- `count` must be ≥ 0
- `range` must be one of the valid TimeRange values

---

### 2. TimeRange (Enum)

| Value | PromQL Duration | Display Label |
|-------|----------------|---------------|
| `1h` | `[1h]` | Letzte Stunde |
| `1d` | `[1d]` | Letzter Tag |
| `7d` | `[7d]` | Letzte Woche |
| `30d` | `[30d]` | Letzter Monat |

**Default**: `1d` (Letzter Tag)

---

### 3. MetricsResponse

Response shape from the Activity Metrics Proxy.

| Field | Type | Description |
|-------|------|-------------|
| range | TimeRange | Requested time range |
| counts | Record<string, number> | Map of activityType → execution count |
| timestamp | ISO 8601 string | Server timestamp of the query |
| error | string \| null | Error message if Prometheus unreachable |

---

### 4. BadgeState (UI State)

Represents the display state of an execution count badge on an Activity Node.

| State | Display | Condition |
|-------|---------|-----------|
| `loading` | Spinner/skeleton pulse | Initial load or time-range change in progress |
| `value` | Formatted count (e.g. "12.5k") | Data successfully loaded |
| `unavailable` | "–" | Proxy unreachable or activity not found in metrics |
| `error` | "!" with tooltip | Unexpected error |

**Transitions**:
- `loading` → `value` (successful response)
- `loading` → `unavailable` (timeout or 503)
- `loading` → `error` (unexpected failure)
- `value` → `loading` (time-range change)
- `value` → `value` (auto-refresh with new data, no flicker)
- `unavailable` → `loading` (retry on next poll cycle)

---

### 5. PrometheusMetric (Internal — Proxy)

Parsed response from Prometheus HTTP API query.

| Field | Type | Description |
|-------|------|-------------|
| metric | `{ activity_type: string }` | Label set |
| value | `[number, string]` | Timestamp + string-encoded float value |

---

## Relationships

```
┌──────────────────┐         ┌─────────────────────┐
│  WorkflowAPI     │         │  Prometheus         │
│  Document        │ 1:1     │  Metrics            │
│  (activityRef)   │◄────────│  (activity_type)    │
└──────────────────┘  match  └─────────────────────┘
        │                              │
        │ displayed on                 │ queried by
        ▼                              ▼
┌──────────────────┐         ┌─────────────────────┐
│  Activity Node   │◄────────│  Metrics Proxy      │
│  (Badge)         │  JSON   │  (PromQL → JSON)    │
└──────────────────┘         └─────────────────────┘
```

## Identity & Matching

The **activity_type** label in Prometheus MUST match the `activityRef` field in the WorkflowAPI document exactly (1:1 string match). This is guaranteed by the Worker's `[Activity("...")]` attribute using the same name as the WorkflowAPI `activityRef`.

Example mapping:
| WorkflowAPI `activityRef` | Worker `[Activity]` | Prometheus `activity_type` |
|---------------------------|--------------------|-----------------------------|
| `IdentifyCompanyActivity` | `[Activity("IdentifyCompanyActivity")]` | `IdentifyCompanyActivity` |
| `ScoreCompanyRiskActivity` | `[Activity("ScoreCompanyRiskActivity")]` | `ScoreCompanyRiskActivity` |
| `PublishRiskResultActivity` | `[Activity("PublishRiskResultActivity")]` | `PublishRiskResultActivity` |

## Data Flow

```
[.NET Worker]                    [Prometheus]              [Metrics Proxy]         [Visualizer]
     │                               │                          │                      │
     │  Counter.Add(1, tag)          │                          │                      │
     │──────────────────────────────►│ scrape /metrics (15s)    │                      │
     │                               │                          │                      │
     │                               │◄─────────────────────────│ PromQL query         │
     │                               │  increase(...[range])    │                      │
     │                               │─────────────────────────►│                      │
     │                               │                          │  JSON response       │
     │                               │                          │─────────────────────►│
     │                               │                          │                      │ render badge
     │                               │                          │                      │
     │                               │                          │◄─────────────────────│ poll (2s)
```

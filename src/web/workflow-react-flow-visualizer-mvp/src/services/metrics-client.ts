export const VALID_TIME_RANGES = ["1h", "1d", "7d", "30d"] as const;
export type TimeRange = (typeof VALID_TIME_RANGES)[number];

export interface MetricsResponse {
  range: TimeRange;
  counts: Record<string, number>;
  workflowCounts: Record<string, number>;
  p95Latencies: Record<string, number>;
  workflowP95Latencies: Record<string, number>;
  timestamp: string;
  error?: string;
}

export class MetricsUnavailableError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "MetricsUnavailableError";
  }
}

const METRICS_PROXY_URL = import.meta.env.VITE_METRICS_PROXY_URL ?? "http://localhost:4000";

export async function fetchActivityCounts(
  range: TimeRange,
  signal?: AbortSignal,
): Promise<MetricsResponse> {
  const requestUrl = new URL("/api/activity-counts", METRICS_PROXY_URL);
  requestUrl.searchParams.set("range", range);

  const response = await fetch(requestUrl, { signal });
  const payload = (await response.json()) as MetricsResponse | { error?: string };

  if (response.status === 503) {
    throw new MetricsUnavailableError(payload.error ?? "Metrics service unavailable");
  }

  if (!response.ok) {
    throw new Error(payload.error ?? `Unexpected metrics response: HTTP ${response.status}`);
  }

  return payload as MetricsResponse;
}

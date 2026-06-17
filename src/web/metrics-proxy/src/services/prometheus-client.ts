export const VALID_RANGES = ["1h", "1d", "7d", "30d"] as const;

export type TimeRange = (typeof VALID_RANGES)[number];

interface PrometheusQueryResult {
  metric?: {
    activity_type?: string;
  };
  value?: [number, string];
}

interface PrometheusApiResponse {
  status: string;
  data?: {
    resultType?: string;
    result?: PrometheusQueryResult[];
  };
}

export class PrometheusUnavailableError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "PrometheusUnavailableError";
  }
}

function isTimeRange(value: string): value is TimeRange {
  return (VALID_RANGES as readonly string[]).includes(value);
}

export function parseTimeRange(value: string | undefined): TimeRange {
  if (value === undefined || value === "") {
    return "1d";
  }

  if (isTimeRange(value)) {
    return value;
  }

  throw new Error("Invalid range parameter. Allowed values: 1h, 1d, 7d, 30d");
}

export function buildActivityCountsQuery(range: TimeRange): string {
  return `sum by(activity_type)(increase(temporal_activity_task_completed_total[${range}]))`;
}

export async function queryActivityCounts(
  prometheusUrl: string,
  range: TimeRange,
): Promise<Record<string, number>> {
  const query = buildActivityCountsQuery(range);
  const queryUrl = new URL("/api/v1/query", prometheusUrl);
  queryUrl.searchParams.set("query", query);

  let response: Response;
  try {
    response = await fetch(queryUrl, { signal: AbortSignal.timeout(5000) });
  } catch (error) {
    const message = error instanceof Error ? error.message : "Unknown error";
    throw new PrometheusUnavailableError(`Prometheus unreachable: ${message}`);
  }

  if (!response.ok) {
    throw new PrometheusUnavailableError(`Prometheus unreachable: HTTP ${response.status}`);
  }

  const payload = (await response.json()) as PrometheusApiResponse;
  if (payload.status !== "success") {
    throw new Error("Prometheus query failed");
  }

  const counts: Record<string, number> = {};
  const results = payload.data?.result ?? [];
  for (const result of results) {
    const activityType = result.metric?.activity_type;
    const valueAsString = result.value?.[1];
    if (!activityType || valueAsString === undefined) {
      continue;
    }

    const value = Number.parseFloat(valueAsString);
    if (!Number.isFinite(value)) {
      continue;
    }

    counts[activityType] = Math.max(0, Math.round(value));
  }

  return counts;
}

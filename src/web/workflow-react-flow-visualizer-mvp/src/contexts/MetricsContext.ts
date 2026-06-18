import { createContext, useContext } from "react";
import type { ActivityMetricsState } from "../hooks/useActivityMetrics";

const defaultState: ActivityMetricsState = {
  counts: {},
  workflowCounts: {},
  p95Latencies: {},
  workflowP95Latencies: {},
  isLoading: true,
  isUnavailable: false,
};

export const MetricsContext = createContext<ActivityMetricsState>(defaultState);

export function useMetrics(): ActivityMetricsState {
  return useContext(MetricsContext);
}

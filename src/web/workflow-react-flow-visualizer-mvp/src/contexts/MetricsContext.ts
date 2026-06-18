import { createContext, useContext } from "react";
import type { ActivityMetricsState } from "../hooks/useActivityMetrics";

const defaultState: ActivityMetricsState = {
  counts: {},
  workflowCounts: {},
  isLoading: true,
  isUnavailable: false,
};

export const MetricsContext = createContext<ActivityMetricsState>(defaultState);

export function useMetrics(): ActivityMetricsState {
  return useContext(MetricsContext);
}

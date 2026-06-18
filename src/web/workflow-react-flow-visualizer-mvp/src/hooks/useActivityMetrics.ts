import { useEffect, useState } from "react";
import {
  fetchActivityCounts,
  MetricsUnavailableError,
  type TimeRange,
} from "../services/metrics-client";

export interface ActivityMetricsState {
  counts: Record<string, number>;
  workflowCounts: Record<string, number>;
  p95Latencies: Record<string, number>;
  workflowP95Latencies: Record<string, number>;
  isLoading: boolean;
  isUnavailable: boolean;
  errorMessage?: string;
}

const POLL_INTERVAL_MS = 2000;
const REQUEST_TIMEOUT_MS = 5000;

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === "AbortError";
}

export function useActivityMetrics(range: TimeRange): ActivityMetricsState {
  const [state, setState] = useState<ActivityMetricsState>({
    counts: {},
    workflowCounts: {},
    p95Latencies: {},
    workflowP95Latencies: {},
    isLoading: true,
    isUnavailable: false,
  });

  useEffect(() => {
    let isUnmounted = false;
    let currentController: AbortController | null = null;

    const fetchCounts = async (withLoadingState: boolean) => {
      currentController?.abort();
      const controller = new AbortController();
      currentController = controller;
      const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

      if (withLoadingState) {
        setState((previous) => ({
          ...previous,
          isLoading: true,
        }));
      }

      try {
        const response = await fetchActivityCounts(range, controller.signal);
        if (isUnmounted) {
          return;
        }

        setState({
          counts: response.counts,
          workflowCounts: response.workflowCounts ?? {},
          p95Latencies: response.p95Latencies ?? {},
          workflowP95Latencies: response.workflowP95Latencies ?? {},
          isLoading: false,
          isUnavailable: false,
        });
      } catch (error) {
        if (isUnmounted || isAbortError(error)) {
          return;
        }

        if (error instanceof MetricsUnavailableError) {
          setState((previous) => ({
            ...previous,
            isLoading: false,
            isUnavailable: true,
            errorMessage: error.message,
          }));
          return;
        }

        const errorMessage = error instanceof Error ? error.message : "Unknown metrics error";
        setState((previous) => ({
          ...previous,
          isLoading: false,
          isUnavailable: true,
          errorMessage,
        }));
      } finally {
        clearTimeout(timeoutId);
      }
    };

    void fetchCounts(true);
    const intervalId = setInterval(() => {
      void fetchCounts(false);
    }, POLL_INTERVAL_MS);

    return () => {
      isUnmounted = true;
      clearInterval(intervalId);
      currentController?.abort();
    };
  }, [range]);

  return state;
}

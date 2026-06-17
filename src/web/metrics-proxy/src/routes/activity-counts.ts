import { Router } from "express";
import {
  parseTimeRange,
  PrometheusUnavailableError,
  queryActivityCounts,
} from "../services/prometheus-client.js";

export function createActivityCountsRouter(prometheusUrl: string): Router {
  const router = Router();

  router.get("/api/activity-counts", async (request, response) => {
    const rangeParam = typeof request.query.range === "string" ? request.query.range : undefined;
    let range: ReturnType<typeof parseTimeRange>;

    try {
      range = parseTimeRange(rangeParam);
    } catch (error) {
      response.status(400).json({
        error: error instanceof Error ? error.message : "Invalid request",
      });
      return;
    }

    try {
      const counts = await queryActivityCounts(prometheusUrl, range);
      response.json({
        range,
        counts,
        timestamp: new Date().toISOString(),
      });
      return;
    } catch (error) {
      if (error instanceof PrometheusUnavailableError) {
        response.status(503).json({
          range,
          counts: {},
          timestamp: new Date().toISOString(),
          error: error.message,
        });
        return;
      }

      response.status(500).json({
        range,
        counts: {},
        timestamp: new Date().toISOString(),
        error: error instanceof Error ? error.message : "Unexpected error",
      });
    }
  });

  return router;
}

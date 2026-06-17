import { useState } from "react";
import { WorkflowVisualizer } from "./components/WorkflowVisualizer";
import { TimeRangeSelector } from "./components/TimeRangeSelector";
import { riskEnrichmentDocument } from "./data/risk-enrichment";
import { workflowApiToGraph } from "./adapters/workflowapi-to-graph";
import { useActivityMetrics } from "./hooks/useActivityMetrics";
import type { TimeRange } from "./services/metrics-client";

function App() {
  const [timeRange, setTimeRange] = useState<TimeRange>("1d");
  const activityMetrics = useActivityMetrics(timeRange);
  const graph = workflowApiToGraph(riskEnrichmentDocument);

  return (
    <main className="min-h-screen bg-slate-50 p-6 text-slate-900">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold">{graph.title}</h1>
        <TimeRangeSelector value={timeRange} onChange={setTimeRange} />
      </div>
      <div className="h-[70vh] rounded-lg border border-slate-200 bg-white shadow-sm">
        <WorkflowVisualizer
          graph={graph}
          activityCounts={activityMetrics.counts}
          metricsLoading={activityMetrics.isLoading}
          metricsUnavailable={activityMetrics.isUnavailable}
        />
      </div>
      <p className="mt-4 text-sm text-slate-600">
        {graph.nodes.length} nodes · {graph.edges.length} edges
      </p>
      {activityMetrics.isUnavailable ? (
        <p className="mt-2 text-sm text-amber-700">
          Ausführungszähler derzeit nicht verfügbar ({activityMetrics.errorMessage ?? "temporärer Fehler"}).
        </p>
      ) : null}
      {graph.diagnostics.length > 0 ? (
        <div className="mt-2">
          {graph.diagnostics.map((diagnostic, index) => (
            <p key={`${diagnostic.message}-${index}`} className="text-sm text-red-700">
              [{diagnostic.severity}] {diagnostic.message}
            </p>
          ))}
        </div>
      ) : null}
    </main>
  );
}

export default App;

import { useMemo, useState } from "react";
import { WorkflowVisualizer, type SelectedWorkflowNode } from "./components/WorkflowVisualizer";
import { TimeRangeSelector } from "./components/TimeRangeSelector";
import { NodeDetailPanel } from "./components/NodeDetailPanel";
import { riskEnrichmentDocument } from "./data/risk-enrichment";
import { workflowApiToGraph } from "./adapters/workflowapi-to-graph";
import { useActivityMetrics } from "./hooks/useActivityMetrics";
import { useEscapeKey } from "./hooks/useEscapeKey";
import { MetricsContext } from "./contexts/MetricsContext";
import type { TimeRange } from "./services/metrics-client";
import type { WorkflowGraphNode, WorkflowNodeKind } from "./types/workflow-graph-model";

function normalizeWorkflowNodeKind(kind: string): WorkflowNodeKind {
  const knownKinds: WorkflowNodeKind[] = [
    "document",
    "host",
    "workflow",
    "operation",
    "activity",
    "step",
    "subflow",
    "childWorkflow",
    "bridge",
    "external",
    "diagnostic",
  ];
  return knownKinds.includes(kind as WorkflowNodeKind) ? (kind as WorkflowNodeKind) : "step";
}

function toWorkflowGraphNode(selected: SelectedWorkflowNode): WorkflowGraphNode {
  return {
    id: selected.id,
    kind: normalizeWorkflowNodeKind(selected.kind),
    label: selected.label,
    description: selected.description,
    raw: selected.raw,
  };
}

function App() {
  const [timeRange, setTimeRange] = useState<TimeRange>("1d");
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);
  const [selectedNode, setSelectedNode] = useState<WorkflowGraphNode | undefined>(undefined);
  const activityMetrics = useActivityMetrics(timeRange);
  const graph = useMemo(() => workflowApiToGraph(riskEnrichmentDocument), []);

  const handleNodeSelect = (node: SelectedWorkflowNode | null) => {
    setSelectedNodeId((current) => {
      if (node === null) {
        setSelectedNode(undefined);
        return null;
      }

      if (current === node.id) {
        setSelectedNode(undefined);
        return null;
      }

      setSelectedNode(toWorkflowGraphNode(node));
      return node.id;
    });
  };

  useEscapeKey(selectedNodeId !== null, () => {
    setSelectedNodeId(null);
    setSelectedNode(undefined);
  });

  return (
    <main className="min-h-screen bg-slate-50 p-6 text-slate-900">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold">{graph.title}</h1>
        <TimeRangeSelector value={timeRange} onChange={setTimeRange} />
      </div>
      <div className="flex h-[70vh] overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
        <MetricsContext.Provider value={activityMetrics}>
          <div className="min-w-0 flex-1">
            <WorkflowVisualizer
              graph={graph}
              onNodeSelect={handleNodeSelect}
              selectedNodeId={selectedNodeId}
            />
          </div>
          {selectedNode ? (
            <aside className="w-80 shrink-0 overflow-y-auto border-l border-slate-200">
              <NodeDetailPanel
                node={selectedNode}
                onClose={() => {
                  setSelectedNodeId(null);
                  setSelectedNode(undefined);
                }}
              />
            </aside>
          ) : null}
        </MetricsContext.Provider>
      </div>
      {activityMetrics.isUnavailable ? (
        <p className="mt-2 text-sm text-amber-700">
          Run counters are currently unavailable ({activityMetrics.errorMessage ?? "temporary error"}).
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

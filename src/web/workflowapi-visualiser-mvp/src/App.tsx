// §15 App composition — parse → project → render pipeline.
// State lives here only (D13: no Zustand/Context for MVP).
import { useState } from "react";
import exampleYaml from "./examples/risk-enrichment.workflowapi.yaml?raw";
import { parseWorkflowApiYaml } from "./workflowapi/parseWorkflowApiYaml";
import { workflowApiToGraph } from "./workflowapi/workflowApiToGraph";
import WorkflowGraph from "./visualiser/WorkflowGraph";
import NodeDetailsPanel from "./visualiser/NodeDetailsPanel";
import DiagnosticsPanel from "./visualiser/DiagnosticsPanel";
import type { WorkflowGraph as WFGraph, WorkflowGraphNode } from "./workflowapi/graphTypes";

// ── Parse & project once at module load (the YAML bundle is immutable) ────────

type ParseResult = { ok: true; graph: WFGraph } | { ok: false; error: string };

function loadGraph(): ParseResult {
  try {
    const doc = parseWorkflowApiYaml(exampleYaml);
    return { ok: true, graph: workflowApiToGraph(doc) };
  } catch (e) {
    return { ok: false, error: e instanceof Error ? e.message : String(e) };
  }
}

const parseResult = loadGraph();

// ── Sub-components ────────────────────────────────────────────────────────────

function ErrorCard({ message }: { message: string }): JSX.Element {
  return (
    <div className="flex h-screen items-center justify-center bg-red-50">
      <div className="max-w-lg rounded-lg border border-red-200 bg-white p-8 shadow">
        <h2 className="mb-2 text-lg font-semibold text-red-700">
          Failed to load WorkflowAPI document
        </h2>
        <pre className="whitespace-pre-wrap text-sm text-red-600">{message}</pre>
      </div>
    </div>
  );
}

const FILENAME = "risk-enrichment.workflowapi.yaml";

// ── Root component ────────────────────────────────────────────────────────────

export default function App(): JSX.Element {
  const [selectedId, setSelectedId] = useState<string | undefined>(undefined);

  if (!parseResult.ok) {
    return <ErrorCard message={parseResult.error} />;
  }

  const { graph } = parseResult;
  const selectedNode: WorkflowGraphNode | undefined = graph.nodes.find(
    (n) => n.id === selectedId
  );

  return (
    <div className="flex h-screen flex-col">
      {/* Header */}
      <header className="flex shrink-0 items-center border-b bg-white px-6 py-3 shadow-sm">
        <h1 className="text-base font-semibold text-slate-800">
          WorkflowAPI Visualiser MVP
        </h1>
      </header>

      {/* Main: graph (left) + details panel (right) */}
      <main className="flex min-h-0 flex-1">
        <div className="min-w-0 flex-1">
          <WorkflowGraph
            graph={graph}
            selectedNodeId={selectedId}
            onNodeSelected={(n) => setSelectedId(n?.id)}
          />
        </div>
        <aside className="w-80 shrink-0 overflow-auto border-l">
          <NodeDetailsPanel selectedNode={selectedNode} />
        </aside>
      </main>

      {/* Diagnostics — collapsible, non-blocking */}
      <DiagnosticsPanel diagnostics={graph.diagnostics} />

      {/* Footer */}
      <footer className="shrink-0 border-t bg-white px-6 py-2 text-center text-xs text-slate-400">
        {FILENAME} · {graph.nodes.length} nodes · {graph.edges.length} edges
      </footer>
    </div>
  );
}

import { WorkflowVisualizer } from "./components/WorkflowVisualizer";
import { riskEnrichmentDocument } from "./data/risk-enrichment";
import { workflowApiToGraph } from "./adapters/workflowapi-to-graph";

function App() {
  const graph = workflowApiToGraph(riskEnrichmentDocument);

  return (
    <main className="min-h-screen bg-slate-50 p-6 text-slate-900">
      <h1 className="mb-4 text-2xl font-semibold">{graph.title}</h1>
      <div className="h-[70vh] rounded-lg border border-slate-200 bg-white shadow-sm">
        <WorkflowVisualizer graph={graph} />
      </div>
      <p className="mt-4 text-sm text-slate-600">
        {graph.nodes.length} nodes · {graph.edges.length} edges
      </p>
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

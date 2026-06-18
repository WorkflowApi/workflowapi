import { Handle, Position, type NodeProps } from "@xyflow/react";
import type { WorkflowNodeData } from "../../adapters/graph-to-reactflow";
import { useMetrics } from "../../contexts/MetricsContext";
import { RunCountLabel } from "./RunCountLabel";

const HIDDEN_HANDLE_STYLE = { opacity: 0 };

export function ChildWorkflowNode({ data, selected }: NodeProps) {
  const nodeData = data as WorkflowNodeData;
  const { workflowCounts, workflowP95Latencies, isLoading, isUnavailable } = useMetrics();

  const executionCount =
    typeof nodeData.workflowRef === "string" ? workflowCounts[nodeData.workflowRef] : undefined;
  const p95Seconds =
    typeof nodeData.workflowRef === "string" ? workflowP95Latencies[nodeData.workflowRef] : undefined;

  return (
    <div
      className={`relative min-w-48 max-w-60 rounded-xl border-2 bg-white text-slate-900 shadow-sm overflow-visible ${
        selected
          ? "border-sky-700 ring-4 ring-sky-400/70 shadow-[0_0_0_4px_rgba(56,189,248,0.2)]"
          : "border-purple-500"
      }`}
      style={{ boxShadow: "0 2px 12px rgba(168, 85, 247, 0.15)" }}
    >
      <Handle type="target" position={Position.Left} style={HIDDEN_HANDLE_STYLE} />
      <Handle type="source" position={Position.Right} style={HIDDEN_HANDLE_STYLE} />
      <Handle type="source" position={Position.Bottom} id="bottom" style={HIDDEN_HANDLE_STYLE} />
      <div className="absolute -top-2.5 left-2.5 z-10">
        <span className="inline-flex items-center whitespace-nowrap rounded bg-purple-500 px-1.5 py-0.5 text-[7px] font-bold uppercase tracking-widest text-white shadow-sm">
          <span aria-hidden className="mr-1 text-[8px]">
            ⎇
          </span>
          Child Workflow
        </span>
      </div>
      <div className="px-3 pb-2.5 pt-3.5">
        <p className="text-[13px] font-semibold leading-snug">{nodeData.label}</p>
        {nodeData.description ? (
          <p className="mt-1.5 text-[9px] leading-relaxed text-slate-600">{nodeData.description}</p>
        ) : null}
        <RunCountLabel
          count={executionCount}
          p95Seconds={p95Seconds}
          isLoading={isLoading}
          isUnavailable={isUnavailable}
          className="mt-1.5 text-[10px] font-medium text-slate-500"
        />
      </div>
    </div>
  );
}

import { Handle, Position, type NodeProps } from "@xyflow/react";
import type { WorkflowNodeData } from "../../adapters/graph-to-reactflow";
import { useMetrics } from "../../contexts/MetricsContext";
import { RunCountLabel } from "./RunCountLabel";

const HIDDEN_HANDLE_STYLE = { opacity: 0 };

export function SubflowNode({ data }: NodeProps) {
  const nodeData = data as WorkflowNodeData;
  const { workflowCounts, isLoading, isUnavailable } = useMetrics();

  // Subflow nodes use workflowRef when available (propagated in graph adapter)
  const executionCount =
    typeof nodeData.workflowRef === "string" ? workflowCounts[nodeData.workflowRef] : undefined;

  return (
    <div className="relative h-full w-full rounded-xl border-2 border-dashed border-teal-400 bg-teal-50/40 overflow-visible">
      <Handle type="target" position={Position.Left} style={HIDDEN_HANDLE_STYLE} />
      <Handle type="source" position={Position.Right} style={HIDDEN_HANDLE_STYLE} />
      <div className="absolute -top-2.5 left-2.5 z-10">
        <span className="inline-flex items-center whitespace-nowrap rounded bg-teal-500 px-1.5 py-0.5 text-[7px] font-bold uppercase tracking-widest text-white shadow-sm">
          <span aria-hidden className="mr-1 text-[8px]">↻</span>
          Subflow
        </span>
      </div>
      <div className="px-3 pt-3">
        <p className="text-[11px] font-semibold text-teal-800">{nodeData.label}</p>
        {nodeData.description ? (
          <p className="text-[8px] text-teal-700/70">{nodeData.description}</p>
        ) : null}
        <RunCountLabel
          count={executionCount}
          isLoading={isLoading}
          isUnavailable={isUnavailable}
          className="mt-1 text-[9px] font-medium text-teal-700/70"
        />
      </div>
    </div>
  );
}

import { Handle, Position, type NodeProps } from "@xyflow/react";
import type { WorkflowNodeData } from "../../adapters/graph-to-reactflow";
import { ExecutionCountBadge } from "./ExecutionCountBadge";
import { useMetrics } from "../../contexts/MetricsContext";

const HIDDEN_HANDLE_STYLE = { opacity: 0 };

export function BridgeNode({ data }: NodeProps) {
  const nodeData = data as WorkflowNodeData;
  const { workflowCounts, isLoading, isUnavailable } = useMetrics();

  // Bridge calls execute once per parent workflow run — use parent workflow's execution count
  const executionCount =
    typeof nodeData.workflowRef === "string" ? workflowCounts[nodeData.workflowRef] : undefined;

  return (
    <div
      className="relative min-w-48 max-w-60 rounded-xl border-2 border-orange-500 bg-white text-slate-900 shadow-sm overflow-visible"
      style={{ boxShadow: "0 2px 12px rgba(251, 146, 60, 0.15)" }}
    >
      <Handle type="target" position={Position.Left} style={HIDDEN_HANDLE_STYLE} />
      <Handle type="source" position={Position.Right} style={HIDDEN_HANDLE_STYLE} />
      <div className="absolute -top-2.5 left-2.5 z-10">
        <span className="inline-flex items-center whitespace-nowrap rounded bg-orange-500 px-1.5 py-0.5 text-[7px] font-bold uppercase tracking-widest text-white shadow-sm">
          <span aria-hidden className="mr-1 text-[8px]">
            ⇄
          </span>
          Bridge
        </span>
      </div>
      <div className="absolute -top-2.5 right-2.5 z-10">
        <ExecutionCountBadge
          count={executionCount}
          isLoading={isLoading}
          isUnavailable={isUnavailable}
        />
      </div>
      <div className="px-3 pb-2.5 pt-3.5">
        <p className="text-[13px] font-semibold leading-snug">{nodeData.label}</p>
        {nodeData.description ? (
          <p className="mt-1.5 text-[9px] leading-relaxed text-slate-600">{nodeData.description}</p>
        ) : null}
      </div>
    </div>
  );
}

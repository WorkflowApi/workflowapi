import { Handle, Position, type NodeProps } from "@xyflow/react";
import type { WorkflowNodeData } from "../../adapters/graph-to-reactflow";
import { useMetrics } from "../../contexts/MetricsContext";
import { RunCountLabel } from "./RunCountLabel";

const HIDDEN_HANDLE_STYLE = { opacity: 0 };

export function ActivityNode({ data, selected }: NodeProps) {
  const nodeData = data as WorkflowNodeData;
  const { counts, p95Latencies, isLoading, isUnavailable } = useMetrics();

  const activityType = typeof nodeData.activityType === "string" ? nodeData.activityType : undefined;
  const executionCount = activityType ? counts[activityType] : undefined;
  const p95Seconds = activityType ? p95Latencies[activityType] : undefined;

  return (
    <div
      className={`relative min-w-48 max-w-60 rounded-xl border-2 bg-white text-slate-900 shadow-sm overflow-visible ${
        selected
          ? "border-sky-700 ring-4 ring-sky-400/70 shadow-[0_0_0_4px_rgba(56,189,248,0.2)]"
          : "border-blue-500"
      }`}
      style={{ boxShadow: "0 2px 12px rgba(59, 130, 246, 0.15)" }}
    >
      <Handle type="target" position={Position.Left} style={HIDDEN_HANDLE_STYLE} />
      <Handle type="source" position={Position.Right} style={HIDDEN_HANDLE_STYLE} />
      <div className="absolute -top-2.5 left-2.5 z-10">
        <span className="inline-flex items-center whitespace-nowrap rounded bg-blue-500 px-1.5 py-0.5 text-[7px] font-bold uppercase tracking-widest text-white shadow-sm">
          <span aria-hidden className="mr-1 text-[8px]">
            ⚙
          </span>
          Activity
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

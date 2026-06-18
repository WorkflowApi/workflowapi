import { Handle, Position, type NodeProps } from "@xyflow/react";
import type { WorkflowNodeData } from "../../adapters/graph-to-reactflow";
import { useMetrics } from "../../contexts/MetricsContext";
import { RunCountLabel } from "./RunCountLabel";

const HIDDEN_HANDLE_STYLE = { opacity: 0 };
const ROTATED_LABEL_STYLE = {
  writingMode: "vertical-rl",
  transform: "rotate(180deg)",
} as const;

export function StepNode({ data, selected }: NodeProps) {
  const nodeData = data as WorkflowNodeData;
  const { counts, isLoading, isUnavailable } = useMetrics();
  const normalizedId = (nodeData.localId ?? nodeData.id).toLowerCase();
  const isStart = normalizedId === "start" || normalizedId.endsWith("::start") || normalizedId.endsWith(":start") || normalizedId.endsWith("/start");
  const isEnd = normalizedId === "end" || normalizedId.endsWith("::end") || normalizedId.endsWith(":end") || normalizedId.endsWith("/end");

  if (isStart || isEnd) {
    const circleClass = isStart
      ? "border-emerald-500 bg-emerald-500 text-white"
      : "border-rose-500 bg-rose-500 text-white";
    const label = isStart ? "Start" : "Stop";

    const handleStyle = { opacity: 0, top: 24 };

    return (
      <div className="flex flex-col items-center gap-1.5">
        <Handle type="target" position={Position.Left} style={handleStyle} />
        <Handle type="source" position={Position.Right} style={handleStyle} />
        <div
          className={`flex h-12 w-12 items-center justify-center rounded-full border-2 shadow-sm ${circleClass} ${
            selected ? "ring-4 ring-sky-400/70 border-sky-700 shadow-[0_0_0_4px_rgba(56,189,248,0.2)]" : ""
          }`}
        >
          {isStart ? (
            <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
              <polygon points="8,5 19,12 8,19" />
            </svg>
          ) : (
            <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
              <rect x="5" y="5" width="14" height="14" rx="2" />
            </svg>
          )}
        </div>
        <span className={`text-[10px] font-semibold ${isStart ? "text-emerald-700" : "text-rose-700"}`}>
          {label}
        </span>
      </div>
    );
  }

  const metricKey = typeof nodeData.activityType === "string" ? nodeData.activityType : nodeData.label;
  const executionCount = counts[metricKey];

  return (
    <div
      className={`relative min-h-[3em] min-w-48 max-w-60 rounded-md border bg-white text-slate-900 shadow-sm overflow-visible ${
        selected
          ? "border-sky-700 ring-4 ring-sky-400/70 shadow-[0_0_0_4px_rgba(56,189,248,0.2)]"
          : "border-blue-400"
      }`}
    >
      <div className="flex justify-start">
        <div className="relative flex w-5 flex-col items-center justify-end rounded-l-sm border-r border-slate-500 bg-gradient-to-b from-slate-700 to-slate-700 text-white">
          <span className="mb-4 text-center text-[9px] font-bold uppercase" style={ROTATED_LABEL_STYLE}>
            ◈ Step
          </span>
        </div>
        <div className="min-w-0 flex-1 p-1">
          <Handle type="target" position={Position.Left} style={HIDDEN_HANDLE_STYLE} />
          <Handle type="source" position={Position.Right} style={HIDDEN_HANDLE_STYLE} />
          <p className="truncate text-xs font-bold">{nodeData.label}</p>
          {nodeData.description ? (
            <p className="mt-1 border-t border-slate-200 pt-1 text-[8px] font-light text-slate-600">
              {nodeData.description}
            </p>
          ) : null}
          <RunCountLabel
            count={executionCount}
            isLoading={isLoading}
            isUnavailable={isUnavailable}
            className="mt-1 text-[9px] font-medium text-slate-500"
          />
        </div>
      </div>
    </div>
  );
}

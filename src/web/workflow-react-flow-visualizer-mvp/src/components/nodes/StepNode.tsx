import { Handle, Position, type NodeProps } from "@xyflow/react";
import type { WorkflowNodeData } from "../../adapters/graph-to-reactflow";

const HIDDEN_HANDLE_STYLE = { opacity: 0 };
const ROTATED_LABEL_STYLE = {
  writingMode: "vertical-rl",
  transform: "rotate(180deg)",
} as const;

export function StepNode({ data }: NodeProps) {
  const nodeData = data as WorkflowNodeData;
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
          className={`flex h-12 w-12 items-center justify-center rounded-full border-2 shadow-sm ${circleClass}`}
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

  return (
    <div className="min-h-[3em] min-w-48 max-w-60 rounded-md border border-blue-400 bg-white text-slate-900 shadow-sm">
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
        </div>
      </div>
    </div>
  );
}

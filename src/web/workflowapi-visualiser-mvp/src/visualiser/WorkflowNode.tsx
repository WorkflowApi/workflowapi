import { Handle, Position, type NodeProps } from "@xyflow/react";
import { nodeKindColors, nodeKindIcons } from "./visualStyles";
import type { WorkflowNodeKind } from "../workflowapi/graphTypes";

export interface WorkflowNodeData extends Record<string, unknown> {
  label: string;
  kind: WorkflowNodeKind;
  description?: string;
}

export default function WorkflowNode(props: NodeProps): JSX.Element {
  const data = props.data as { label: string; kind: WorkflowNodeKind; description?: string };
  const { label, kind, description } = data;
  const Icon = nodeKindIcons[kind];
  const color = nodeKindColors[kind];

  return (
    <div
      className="w-[220px] h-[80px] bg-white rounded shadow flex flex-col justify-center px-3 py-2 border-l-4 overflow-hidden"
      style={{
        borderLeftColor: color,
        boxShadow: props.selected
          ? `0 0 0 3px ${color}`
          : undefined,
      }}
    >
      <Handle type="target" position={Position.Left} />
      <Handle type="source" position={Position.Right} />

      {/* Row 1: icon + kind badge */}
      <div className="flex items-center gap-1 text-xs font-semibold uppercase tracking-wide" style={{ color }}>
        <Icon size={12} />
        <span>{kind}</span>
      </div>

      {/* Row 2: label */}
      <div className="text-sm font-medium text-gray-800 truncate leading-tight">
        {label}
      </div>

      {/* Row 3: description */}
      {description && (
        <div className="text-xs text-gray-400 line-clamp-1 leading-tight">
          {description}
        </div>
      )}
    </div>
  );
}

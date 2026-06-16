import type { WorkflowNodeKind } from "../workflowapi/graphTypes";
export type { WorkflowNodeKind } from "../workflowapi/graphTypes";

import {
  Box,
  Cable,
  CirclePlay,
  Cog,
  ExternalLink,
  FileText,
  GitBranch,
  type LucideIcon,
  Server,
  TriangleAlert,
  Workflow,
} from "lucide-react";

export const nodeKindColors: Record<WorkflowNodeKind, string> = {
  document: "#334155",
  host: "#475569",
  workflow: "#6366f1",
  operation: "#22c55e",
  activity: "#14b8a6",
  step: "#64748b",
  childWorkflow: "#8b5cf6",
  bridge: "#f43f5e",
  external: "#94a3b8",
  diagnostic: "#f59e0b",
};

export const nodeKindIcons: Record<WorkflowNodeKind, LucideIcon> = {
  workflow: GitBranch,
  operation: CirclePlay,
  activity: Cog,
  childWorkflow: Workflow,
  bridge: Cable,
  external: ExternalLink,
  host: Server,
  document: FileText,
  diagnostic: TriangleAlert,
  step: Box,
};

export type WorkflowNodeKind =
  | "document"
  | "host"
  | "workflow"
  | "operation"
  | "activity"
  | "step"
  | "subflow"
  | "childWorkflow"
  | "bridge"
  | "external"
  | "diagnostic";

export type WorkflowEdgeKind =
  | "contains"
  | "starts"
  | "continuesTo"
  | "callsActivity"
  | "startsChildWorkflow"
  | "usesBridge"
  | "dependsOn"
  | "unknown";

export interface WorkflowGraphNode {
  id: string;
  kind: WorkflowNodeKind;
  label: string;
  description?: string;
  sourcePath?: string;
  raw?: unknown;
}

export interface WorkflowGraphEdge {
  id: string;
  kind: WorkflowEdgeKind;
  source: string;
  target: string;
  label?: string;
  raw?: unknown;
}

export interface WorkflowGraph {
  documentId?: string;
  title?: string;
  nodes: WorkflowGraphNode[];
  edges: WorkflowGraphEdge[];
  diagnostics: WorkflowGraphDiagnostic[];
}

export interface WorkflowGraphDiagnostic {
  severity: "info" | "warning" | "error";
  message: string;
  sourcePath?: string;
}

export type TopologyNodeKind =
  | "start"
  | "end"
  | "activity"
  | "bridge"
  | "childWorkflow"
  | "subflow";

export interface WorkflowApiTopologyNode {
  kind: TopologyNodeKind;
  displayName?: string;
  summary?: string;
  activityRef?: string;
  bridgeRef?: string;
  workflowRef?: string;
  subflowRef?: string;
}

export interface WorkflowApiTopologyEdge {
  from: string;
  to: string;
}

export interface WorkflowApiSubflow {
  summary?: string;
  nodes: Record<string, WorkflowApiTopologyNode>;
  edges: WorkflowApiTopologyEdge[];
}

export interface WorkflowApiWorkflow {
  name: string;
  displayName: string;
  summary?: string;
  topology: {
    nodes: Record<string, WorkflowApiTopologyNode>;
    edges: WorkflowApiTopologyEdge[];
  };
}

export interface WorkflowApiDocument {
  info: {
    title: string;
    version: string;
  };
  workflows: Record<string, WorkflowApiWorkflow>;
  components?: {
    subflows?: Record<string, WorkflowApiSubflow>;
  };
}

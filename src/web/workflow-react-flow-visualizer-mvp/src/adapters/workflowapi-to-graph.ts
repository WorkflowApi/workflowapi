import type { WorkflowApiDocument, WorkflowApiSubflow } from "../types/workflowapi-model";
import type {
  WorkflowGraph,
  WorkflowGraphEdge,
  WorkflowGraphNode,
  WorkflowNodeKind,
} from "../types/workflow-graph-model";

function mapNodeKind(kind: string): WorkflowNodeKind {
  switch (kind) {
    case "activity":
      return "activity";
    case "bridge":
      return "bridge";
    case "childWorkflow":
      return "childWorkflow";
    case "subflow":
      return "subflow";
    case "start":
    case "end":
      return "step";
    default:
      return "step";
  }
}

function resolveSubflowRef(
  ref: string | undefined,
  document: WorkflowApiDocument,
): WorkflowApiSubflow | undefined {
  if (!ref) return undefined;
  // Handle refs like '#/components/subflows/SignalCollectionSubflow'
  const match = ref.match(/^#\/components\/subflows\/(.+)$/);
  if (!match) return undefined;
  return document.components?.subflows?.[match[1]];
}

interface ChildWorkflowData {
  displayName: string;
  summary?: string;
  nodes: Record<string, { kind: string; displayName?: string; summary?: string; workflowRef?: string; subflowRef?: string }>;
  edges: Array<{ from: string; to: string }>;
}

function resolveChildWorkflow(
  workflowRef: string,
  document: WorkflowApiDocument,
): ChildWorkflowData | undefined {
  const workflow = document.workflows[workflowRef];
  if (!workflow) return undefined;

  // Recursively resolve nested child workflows and subflows within the child
  const nodes: ChildWorkflowData["nodes"] = {};
  for (const [nodeId, node] of Object.entries(workflow.topology.nodes)) {
    const entry: ChildWorkflowData["nodes"][string] = {
      kind: node.kind,
      displayName: node.displayName,
      summary: node.summary,
      workflowRef: node.workflowRef,
      subflowRef: node.subflowRef,
    };

    // Resolve nested subflows
    if (node.kind === "subflow" && node.subflowRef) {
      const resolvedSubflow = resolveSubflowRef(node.subflowRef, document);
      if (resolvedSubflow) {
        (entry as Record<string, unknown>).subflow = resolvedSubflow;
      }
    }

    // Resolve nested child workflows
    if (node.kind === "childWorkflow" && node.workflowRef) {
      const resolvedChild = resolveChildWorkflow(node.workflowRef, document);
      if (resolvedChild) {
        (entry as Record<string, unknown>).childWorkflow = resolvedChild;
      }
    }

    nodes[nodeId] = entry;
  }

  return {
    displayName: workflow.displayName,
    summary: workflow.summary,
    nodes,
    edges: workflow.topology.edges,
  };
}

function nodeToGraphNode(
  workflowName: string,
  localNodeId: string,
  node: WorkflowApiDocument["workflows"][string]["topology"]["nodes"][string],
  resolvedSubflow?: WorkflowApiSubflow,
  resolvedChildWorkflow?: ChildWorkflowData,
): WorkflowGraphNode {
  const id = `${workflowName}::${localNodeId}`;

  return {
    id,
    kind: mapNodeKind(node.kind),
    label: node.displayName ?? localNodeId,
    description: node.summary,
    raw: {
      ...node,
      workflowName,
      localNodeId,
      ...(resolvedSubflow ? { subflow: resolvedSubflow } : {}),
      ...(resolvedChildWorkflow ? { childWorkflow: resolvedChildWorkflow } : {}),
    },
  };
}

function edgeToGraphEdge(
  workflowName: string,
  source: string,
  target: string,
): WorkflowGraphEdge {
  const sourceId = `${workflowName}::${source}`;
  const targetId = `${workflowName}::${target}`;

  return {
    id: `${sourceId}->${targetId}`,
    kind: "continuesTo",
    source: sourceId,
    target: targetId,
    raw: { from: source, to: target },
  };
}

export function workflowApiToGraph(document: WorkflowApiDocument): WorkflowGraph {
  const nodes: WorkflowGraphNode[] = [];
  const edges: WorkflowGraphEdge[] = [];

  // Collect which workflows are referenced as children so we can exclude them from top-level rendering
  const referencedChildWorkflows = new Set<string>();
  for (const workflow of Object.values(document.workflows)) {
    for (const node of Object.values(workflow.topology.nodes)) {
      if (node.kind === "childWorkflow" && node.workflowRef) {
        referencedChildWorkflows.add(node.workflowRef);
      }
    }
  }

  for (const [workflowName, workflow] of Object.entries(document.workflows)) {
    // Skip workflows that are embedded as children
    if (referencedChildWorkflows.has(workflowName)) continue;

    for (const [localNodeId, node] of Object.entries(workflow.topology.nodes)) {
      const resolvedSubflow = node.kind === "subflow"
        ? resolveSubflowRef(node.subflowRef, document)
        : undefined;

      const resolvedChildWorkflow = node.kind === "childWorkflow" && node.workflowRef
        ? resolveChildWorkflow(node.workflowRef, document)
        : undefined;

      nodes.push(nodeToGraphNode(workflowName, localNodeId, node, resolvedSubflow, resolvedChildWorkflow));
    }

    edges.push(
      ...workflow.topology.edges.map((edge) =>
        edgeToGraphEdge(workflowName, edge.from, edge.to),
      ),
    );
  }

  return {
    documentId: `${document.info.title}@${document.info.version}`,
    title: document.info.title,
    nodes,
    edges,
    diagnostics: [],
  };
}

import dagre from "dagre";
import { MarkerType, type Edge, type Node } from "@xyflow/react";
import type { WorkflowGraph, WorkflowGraphNode } from "../types/workflow-graph-model";

export interface WorkflowNodeData {
  label: string;
  description?: string;
  kind: string;
  id: string;
  localId?: string;
  activityType?: string;
  workflowRef?: string;
  [key: string]: unknown;
}

interface SubflowData {
  nodes: Record<string, { kind: string; displayName?: string; summary?: string; activityRef?: string }>;
  edges: Array<{ from: string; to: string }>;
}

interface ChildWorkflowNodeData {
  kind: string;
  displayName?: string;
  summary?: string;
  activityRef?: string;
  workflowRef?: string;
  subflowRef?: string;
  subflow?: SubflowData;
  childWorkflow?: ChildWorkflowExpandedData;
}

interface ChildWorkflowExpandedData {
  displayName: string;
  summary?: string;
  nodes: Record<string, ChildWorkflowNodeData>;
  edges: Array<{ from: string; to: string }>;
}

const NODE_SIZES: Record<string, { width: number; height: number }> = {
  activity: { width: 240, height: 120 },
  bridge: { width: 240, height: 120 },
  childWorkflow: { width: 260, height: 130 },
  step: { width: 280, height: 100 },
  terminal: { width: 64, height: 76 },
};

const SUBFLOW_CHILD_SIZE = { width: 200, height: 80 };
const SUBFLOW_PADDING = 40;
const SUBFLOW_HEADER_HEIGHT = 60;

const FALLBACK_NODE_SIZE = { width: 240, height: 120 };

function isTerminalNode(node: WorkflowGraphNode): boolean {
  if (!node.raw || typeof node.raw !== "object") return false;
  const raw = node.raw as Record<string, unknown>;
  return raw.kind === "start" || raw.kind === "end";
}

function getNodeSize(nodeType: string): { width: number; height: number } {
  return NODE_SIZES[nodeType] ?? FALLBACK_NODE_SIZE;
}

function getSubflowSize(subflow: SubflowData): { width: number; height: number } {
  // Layout the subflow's children internally to determine total size
  const childCount = Object.keys(subflow.nodes).length;
  if (childCount === 0) return { width: 300, height: 150 };

  const g = new dagre.graphlib.Graph();
  g.setDefaultEdgeLabel(() => ({}));
  g.setGraph({ rankdir: "LR", nodesep: 30, ranksep: 60 });

  for (const id of Object.keys(subflow.nodes)) {
    g.setNode(id, { width: SUBFLOW_CHILD_SIZE.width, height: SUBFLOW_CHILD_SIZE.height });
  }
  for (const edge of subflow.edges) {
    g.setEdge(edge.from, edge.to);
  }
  dagre.layout(g);

  let maxX = 0;
  let maxY = 0;
  for (const id of Object.keys(subflow.nodes)) {
    const pos = g.node(id);
    maxX = Math.max(maxX, pos.x + pos.width / 2);
    maxY = Math.max(maxY, pos.y + pos.height / 2);
  }

  return {
    width: maxX + SUBFLOW_PADDING * 2,
    height: maxY + SUBFLOW_HEADER_HEIGHT + SUBFLOW_PADDING * 2,
  };
}

function getSubflowData(node: WorkflowGraphNode): SubflowData | undefined {
  if (
    node.raw &&
    typeof node.raw === "object" &&
    "subflow" in node.raw &&
    node.raw.subflow &&
    typeof node.raw.subflow === "object"
  ) {
    return node.raw.subflow as SubflowData;
  }
  return undefined;
}

function getChildWorkflowData(node: WorkflowGraphNode): ChildWorkflowExpandedData | undefined {
  if (
    node.raw &&
    typeof node.raw === "object" &&
    "childWorkflow" in node.raw &&
    node.raw.childWorkflow &&
    typeof node.raw.childWorkflow === "object"
  ) {
    return node.raw.childWorkflow as ChildWorkflowExpandedData;
  }
  return undefined;
}

function getGroupChildSize(kind: string): { width: number; height: number } {
  if (kind === "subflow") return { width: 280, height: 160 };
  return SUBFLOW_CHILD_SIZE;
}

function getChildWorkflowGroupSize(cwData: ChildWorkflowExpandedData): { width: number; height: number } {
  const childIds = Object.keys(cwData.nodes);
  if (childIds.length === 0) return { width: 300, height: 180 };

  const g = new dagre.graphlib.Graph();
  g.setDefaultEdgeLabel(() => ({}));
  g.setGraph({ rankdir: "LR", nodesep: 30, ranksep: 60 });

  for (const [id, child] of Object.entries(cwData.nodes)) {
    if (child.kind === "subflow" && child.subflow) {
      const subSize = getSubflowSize(child.subflow);
      g.setNode(id, { width: subSize.width, height: subSize.height });
    } else if (child.kind === "start" || child.kind === "end") {
      const ts = NODE_SIZES.terminal;
      g.setNode(id, { width: ts.width, height: ts.height });
    } else {
      g.setNode(id, { width: SUBFLOW_CHILD_SIZE.width, height: SUBFLOW_CHILD_SIZE.height });
    }
  }
  for (const edge of cwData.edges) {
    g.setEdge(edge.from, edge.to);
  }
  dagre.layout(g);

  let maxX = 0;
  let maxY = 0;
  for (const id of childIds) {
    const pos = g.node(id);
    maxX = Math.max(maxX, pos.x + pos.width / 2);
    maxY = Math.max(maxY, pos.y + pos.height / 2);
  }

  return {
    width: maxX + SUBFLOW_PADDING * 2,
    height: maxY + SUBFLOW_HEADER_HEIGHT + SUBFLOW_PADDING * 2,
  };
}

interface DeferredChildWorkflowGroup {
  triggerId: string;
  cwData: ChildWorkflowExpandedData;
  /** workflowRef of the child workflow (used as metric key for nested bridge/subflow nodes) */
  workflowRef?: string;
}

/** Expand a child workflow group's children into nodes & edges.
 *  Nested child workflows are NOT embedded — they are returned as deferred groups for top-level rendering. */
function expandChildWorkflowGroup(
  parentGroupId: string,
  cwData: ChildWorkflowExpandedData,
  nodes: Node<WorkflowNodeData>[],
  edges: Edge[],
  /** workflowRef of the child workflow — propagated to bridge/subflow nodes as their metric key */
  parentWorkflowRef?: string,
): DeferredChildWorkflowGroup[] {
  const deferred: DeferredChildWorkflowGroup[] = [];

  const childG = new dagre.graphlib.Graph();
  childG.setDefaultEdgeLabel(() => ({}));
  childG.setGraph({ rankdir: "LR", nodesep: 30, ranksep: 60 });

  for (const [id, child] of Object.entries(cwData.nodes)) {
    if (child.kind === "subflow" && child.subflow) {
      const subSize = getSubflowSize(child.subflow);
      childG.setNode(id, { width: subSize.width, height: subSize.height });
    } else if (child.kind === "start" || child.kind === "end") {
      const ts = NODE_SIZES.terminal;
      childG.setNode(id, { width: ts.width, height: ts.height });
    } else {
      childG.setNode(id, { width: SUBFLOW_CHILD_SIZE.width, height: SUBFLOW_CHILD_SIZE.height });
    }
  }
  for (const edge of cwData.edges) {
    childG.setEdge(edge.from, edge.to);
  }
  dagre.layout(childG);

  // Vertically center all children on the same line
  let maxChildCenterY = 0;
  for (const id of Object.keys(cwData.nodes)) {
    const pos = childG.node(id);
    if (pos.y > maxChildCenterY) maxChildCenterY = pos.y;
  }
  for (const id of Object.keys(cwData.nodes)) {
    const pos = childG.node(id);
    pos.y = maxChildCenterY;
  }

  for (const [childId, childNode] of Object.entries(cwData.nodes)) {
    const childPos = childG.node(childId);
    const globalChildId = `${parentGroupId}/${childId}`;

    if (childNode.kind === "subflow" && childNode.subflow) {
      // Nested subflow group inside child workflow
      const subSize = getSubflowSize(childNode.subflow);
      nodes.push({
        id: globalChildId,
        type: "subflow",
        parentId: parentGroupId,
        data: {
          label: childNode.displayName ?? childId,
          description: childNode.summary,
          kind: "subflow",
          id: globalChildId,
          // Subflows execute once per parent workflow run — use parent workflow ref as metric key
          workflowRef: parentWorkflowRef,
        },
        position: {
          x: childPos.x - subSize.width / 2 + SUBFLOW_PADDING,
          y: childPos.y - subSize.height / 2 + SUBFLOW_HEADER_HEIGHT + SUBFLOW_PADDING,
        },
        style: { width: subSize.width, height: subSize.height },
      } as Node<WorkflowNodeData>);

      expandSubflowChildren(globalChildId, childNode.subflow, nodes, edges);
    } else if (childNode.kind === "childWorkflow" && childNode.childWorkflow) {
      // Render only the trigger node inside the group; defer the group itself
      nodes.push({
        id: globalChildId,
        type: "childWorkflow",
        parentId: parentGroupId,
        data: {
          label: childNode.displayName ?? childId,
          description: childNode.summary,
          kind: "childWorkflow",
          id: globalChildId,
          workflowRef: typeof childNode.workflowRef === "string" ? childNode.workflowRef : undefined,
        },
        position: {
          x: childPos.x - SUBFLOW_CHILD_SIZE.width / 2 + SUBFLOW_PADDING,
          y: childPos.y - SUBFLOW_CHILD_SIZE.height / 2 + SUBFLOW_HEADER_HEIGHT + SUBFLOW_PADDING,
        },
      } as Node<WorkflowNodeData>);

      deferred.push({
        triggerId: globalChildId,
        cwData: childNode.childWorkflow,
        workflowRef: typeof childNode.workflowRef === "string" ? childNode.workflowRef : undefined,
      });
    } else {
      // Regular node inside child workflow group
      const nodeType = mapChildNodeType(childNode.kind);
      const isTerminal = childNode.kind === "start" || childNode.kind === "end";
      const nodeW = isTerminal ? NODE_SIZES.terminal.width : SUBFLOW_CHILD_SIZE.width;
      const nodeH = isTerminal ? NODE_SIZES.terminal.height : SUBFLOW_CHILD_SIZE.height;
      const activityType =
        nodeType === "activity"
          ? childNode.activityRef ?? (childNode.displayName ?? childId)
          : undefined;
      // Bridge nodes inside a child workflow group share the parent workflow's execution count
      const workflowRef = nodeType === "bridge" ? parentWorkflowRef : undefined;
      nodes.push({
        id: globalChildId,
        type: nodeType,
        parentId: parentGroupId,
        data: {
          label: childNode.displayName ?? childId,
          description: childNode.summary,
          kind: nodeType,
          id: globalChildId,
          activityType,
          workflowRef,
        },
        position: {
          x: childPos.x - nodeW / 2 + SUBFLOW_PADDING,
          y: childPos.y - nodeH / 2 + SUBFLOW_HEADER_HEIGHT + SUBFLOW_PADDING,
        },
      } as Node<WorkflowNodeData>);
    }
  }

  // Build subflow first/last child lookup for edge redirection within the group
  const internalFirstChild = new Map<string, string>();
  const internalLastChild = new Map<string, string>();

  for (const [childId, childNode] of Object.entries(cwData.nodes)) {
    const globalChildId = `${parentGroupId}/${childId}`;
    if (childNode.kind === "subflow" && childNode.subflow) {
      const first = findFirstChild(childNode.subflow);
      const last = findLastChild(childNode.subflow);
      if (first) internalFirstChild.set(globalChildId, `${globalChildId}/${first}`);
      if (last) internalLastChild.set(globalChildId, `${globalChildId}/${last}`);
    }
  }

  // Internal edges (redirect to subflow children where applicable)
  for (const edge of cwData.edges) {
    const rawSourceId = `${parentGroupId}/${edge.from}`;
    const rawTargetId = `${parentGroupId}/${edge.to}`;
    const sourceId = internalLastChild.get(rawSourceId) ?? rawSourceId;
    const targetId = internalFirstChild.get(rawTargetId) ?? rawTargetId;
    edges.push({
      id: `${rawSourceId}->${rawTargetId}`,
      source: sourceId,
      target: targetId,
      type: "smoothstep",
      animated: true,
      style: { stroke: "#a855f7", strokeWidth: 1.5, strokeDasharray: "6 4" },
      markerEnd: { type: MarkerType.ArrowClosed, width: 16, height: 16, color: "#a855f7" },
    });
  }

  return deferred;
}

/** Expand subflow children into a parent group (reused for nested subflows) */
function expandSubflowChildren(
  parentId: string,
  subflow: SubflowData,
  nodes: Node<WorkflowNodeData>[],
  edges: Edge[],
): void {
  const childG = new dagre.graphlib.Graph();
  childG.setDefaultEdgeLabel(() => ({}));
  childG.setGraph({ rankdir: "LR", nodesep: 30, ranksep: 60 });

  for (const id of Object.keys(subflow.nodes)) {
    childG.setNode(id, { width: SUBFLOW_CHILD_SIZE.width, height: SUBFLOW_CHILD_SIZE.height });
  }
  for (const edge of subflow.edges) {
    childG.setEdge(edge.from, edge.to);
  }
  dagre.layout(childG);

  // Vertically center all subflow children
  let maxSubCenterY = 0;
  for (const id of Object.keys(subflow.nodes)) {
    const pos = childG.node(id);
    if (pos.y > maxSubCenterY) maxSubCenterY = pos.y;
  }
  for (const id of Object.keys(subflow.nodes)) {
    const pos = childG.node(id);
    pos.y = maxSubCenterY;
  }

  for (const [childId, childNode] of Object.entries(subflow.nodes)) {
    const childPos = childG.node(childId);
    const globalChildId = `${parentId}/${childId}`;

    nodes.push({
      id: globalChildId,
      type: "activity",
      parentId: parentId,
      data: {
        label: childNode.displayName ?? childId,
        description: childNode.summary,
        kind: "activity",
        id: globalChildId,
        activityType: childNode.activityRef ?? (childNode.displayName ?? childId),
      },
      position: {
        x: childPos.x - SUBFLOW_CHILD_SIZE.width / 2 + SUBFLOW_PADDING,
        y: childPos.y - SUBFLOW_CHILD_SIZE.height / 2 + SUBFLOW_HEADER_HEIGHT + SUBFLOW_PADDING,
      },
    } as Node<WorkflowNodeData>);
  }

  for (const subEdge of subflow.edges) {
    const sourceId = `${parentId}/${subEdge.from}`;
    const targetId = `${parentId}/${subEdge.to}`;
    edges.push({
      id: `${sourceId}->${targetId}`,
      source: sourceId,
      target: targetId,
      type: "smoothstep",
      animated: true,
      style: { stroke: "#14b8a6", strokeWidth: 1.5, strokeDasharray: "6 4" },
      markerEnd: { type: MarkerType.ArrowClosed, width: 16, height: 16, color: "#14b8a6" },
    });
  }
}

function findFirstChild(data: { nodes: Record<string, unknown>; edges: Array<{ from: string; to: string }> }): string | undefined {
  const childIds = Object.keys(data.nodes);
  if (childIds.length === 0) return undefined;
  const targets = new Set(data.edges.map((e) => e.to));
  return childIds.find((id) => !targets.has(id)) ?? childIds[0];
}

function findLastChild(data: { nodes: Record<string, unknown>; edges: Array<{ from: string; to: string }> }): string | undefined {
  const childIds = Object.keys(data.nodes);
  if (childIds.length === 0) return undefined;
  const sources = new Set(data.edges.map((e) => e.from));
  return childIds.find((id) => !sources.has(id)) ?? childIds[childIds.length - 1];
}

function mapChildNodeType(kind: string): string {
  switch (kind) {
    case "activity": return "activity";
    case "bridge": return "bridge";
    case "start":
    case "end": return "step";
    default: return "activity";
  }
}

export function graphToReactFlow(graph: WorkflowGraph): { nodes: Node<WorkflowNodeData>[]; edges: Edge[] } {
  const dagreGraph = new dagre.graphlib.Graph();
  dagreGraph.setDefaultEdgeLabel(() => ({}));
  dagreGraph.setGraph({
    rankdir: "LR",
    nodesep: 80,
    ranksep: 140,
    edgesep: 40,
  });

  // Pre-compute sizes
  // For dagre layout, childWorkflow nodes use only the trigger size (group is placed separately below)
  const nodeSizes = new Map<string, { width: number; height: number }>();
  const childWorkflowGroups = new Map<string, { cwData: ChildWorkflowExpandedData; groupSize: { width: number; height: number } }>();

  for (const node of graph.nodes) {
    if (node.kind === "subflow") {
      const subflow = getSubflowData(node);
      nodeSizes.set(node.id, subflow ? getSubflowSize(subflow) : { width: 300, height: 180 });
    } else if (node.kind === "childWorkflow") {
      const cwData = getChildWorkflowData(node);
      if (cwData) {
        // Dagre gets only the trigger size — no inflation from the group
        nodeSizes.set(node.id, getNodeSize("childWorkflow"));
        childWorkflowGroups.set(node.id, { cwData, groupSize: getChildWorkflowGroupSize(cwData) });
      } else {
        nodeSizes.set(node.id, getNodeSize(node.kind));
      }
    } else {
      const sizeKey = isTerminalNode(node) ? "terminal" : node.kind;
      nodeSizes.set(node.id, getNodeSize(sizeKey));
    }
  }

  for (const node of graph.nodes) {
    const size = nodeSizes.get(node.id)!;
    dagreGraph.setNode(node.id, { width: size.width, height: size.height });
  }

  for (const edge of graph.edges) {
    dagreGraph.setEdge(edge.source, edge.target);
  }

  dagre.layout(dagreGraph);

  // Force all top-level nodes onto the same vertical center line
  let maxCenterY = 0;
  for (const node of graph.nodes) {
    const pos = dagreGraph.node(node.id);
    if (pos.y > maxCenterY) maxCenterY = pos.y;
  }
  for (const node of graph.nodes) {
    const pos = dagreGraph.node(node.id);
    pos.y = maxCenterY;
  }

  const nodes: Node<WorkflowNodeData>[] = [];
  const edges: Edge[] = [];
  const pendingGroups: DeferredChildWorkflowGroup[] = [];

  // Track first/last child IDs for edge redirection
  const groupFirstChild = new Map<string, string>();
  const groupLastChild = new Map<string, string>();

  for (const node of graph.nodes) {
    const size = nodeSizes.get(node.id)!;
    const position = dagreGraph.node(node.id);
    const topLeftX = position.x - size.width / 2;
    const topLeftY = position.y - size.height / 2;

    const localId = node.raw && typeof node.raw === "object" && "localNodeId" in node.raw
      ? (node.raw.localNodeId as string)
      : undefined;
    const rawNode = (node.raw && typeof node.raw === "object") ? node.raw as Record<string, unknown> : {};
    const activityType = node.kind === "activity" &&
      typeof rawNode.activityRef === "string"
      ? rawNode.activityRef
      : undefined;
    // childWorkflow: workflowRef = the target workflow name
    // bridge/subflow: workflowRef = parent workflow name (they execute once per parent run)
    const workflowRef: string | undefined =
      node.kind === "childWorkflow" && typeof rawNode.workflowRef === "string"
        ? rawNode.workflowRef
        : (node.kind === "bridge" || node.kind === "subflow") && typeof rawNode.workflowName === "string"
          ? rawNode.workflowName
          : undefined;

    if (node.kind === "subflow") {
      const subflow = getSubflowData(node);

      nodes.push({
        id: node.id,
        type: "subflow",
        data: { label: node.label, description: node.description, kind: node.kind, id: node.id, localId, activityType, workflowRef },
        position: { x: topLeftX, y: topLeftY },
        style: { width: size.width, height: size.height },
      });

      if (subflow) {
        expandSubflowChildren(node.id, subflow, nodes, edges);

        const first = findFirstChild(subflow);
        const last = findLastChild(subflow);
        if (first) groupFirstChild.set(node.id, `${node.id}/${first}`);
        if (last) groupLastChild.set(node.id, `${node.id}/${last}`);
      }
    } else if (node.kind === "childWorkflow") {
      const cwGroup = childWorkflowGroups.get(node.id);

      if (cwGroup) {
        // Trigger node at dagre position (same size as other nodes, no layout inflation)
        nodes.push({
          id: node.id,
          type: "childWorkflow",
          data: { label: node.label, description: node.description, kind: node.kind, id: node.id, localId, activityType, workflowRef },
          position: { x: topLeftX, y: topLeftY },
        });

        // Defer group to be placed below the entire workflow
        pendingGroups.push({ triggerId: node.id, cwData: cwGroup.cwData, workflowRef });
      } else {
        // No embedded data, render as simple node
        nodes.push({
          id: node.id,
          type: "childWorkflow",
          data: { label: node.label, description: node.description, kind: node.kind, id: node.id, localId, activityType, workflowRef },
          position: { x: topLeftX, y: topLeftY },
        });
      }
    } else {
      // Regular node (includes bridge nodes at top level)
      nodes.push({
        id: node.id,
        type: node.kind,
        data: { label: node.label, description: node.description, kind: node.kind, id: node.id, localId, activityType, workflowRef },
        position: { x: topLeftX, y: topLeftY },
      });
    }
  }

  // Add top-level edges, redirecting to group child nodes where applicable
  for (const edge of graph.edges) {
    const source = groupLastChild.get(edge.source) ?? edge.source;
    const target = groupFirstChild.get(edge.target) ?? edge.target;

    edges.push({
      id: edge.id,
      source,
      target,
      label: edge.label,
      type: "smoothstep",
      animated: true,
      style: { stroke: "#6b7280", strokeWidth: 1.5, strokeDasharray: "6 4" },
      markerEnd: { type: MarkerType.ArrowClosed, width: 20, height: 20, color: "#6b7280" },
    });
  }

  // Process deferred child workflow groups (rendered as separate top-level groups, not nested)
  let groupYOffset = 0;
  // Find the bottom of all existing nodes for vertical placement
  for (const n of nodes) {
    const h = (n.style?.height as number) ?? 130;
    const bottom = (n.position?.y ?? 0) + h;
    if (bottom > groupYOffset) groupYOffset = bottom;
  }
  groupYOffset += 80;

  while (pendingGroups.length > 0) {
    const { triggerId, cwData, workflowRef } = pendingGroups.shift()!;
    const groupId = `${triggerId}__group`;
    const groupSize = getChildWorkflowGroupSize(cwData);

    // Center group horizontally under the trigger node
    const triggerNode = nodes.find((n) => n.id === triggerId);
    const triggerX = triggerNode?.position?.x ?? 0;
    const triggerW = (triggerNode?.style?.width as number) ?? (triggerNode?.measured?.width ?? getNodeSize("childWorkflow").width);
    const triggerCenterX = triggerX + triggerW / 2;
    const groupX = triggerCenterX - groupSize.width / 2;

    nodes.push({
      id: groupId,
      type: "childWorkflowGroup",
      data: {
        label: cwData.displayName,
        description: cwData.summary,
        kind: "childWorkflowGroup",
        id: groupId,
        workflowRef,
      },
      position: { x: groupX, y: groupYOffset },
      style: { width: groupSize.width, height: groupSize.height },
    });

    const nestedDeferred = expandChildWorkflowGroup(groupId, cwData, nodes, edges, workflowRef);

    // Dashed edge from trigger (bottom) to group (top)
    edges.push({
      id: `${triggerId}->${groupId}`,
      source: triggerId,
      sourceHandle: "bottom",
      target: groupId,
      targetHandle: "top",
      type: "smoothstep",
      animated: true,
      style: { stroke: "#a855f7", strokeWidth: 1.5, strokeDasharray: "6 4" },
      markerEnd: { type: MarkerType.ArrowClosed, width: 16, height: 16, color: "#a855f7" },
    });

    groupYOffset += groupSize.height + 80;
    pendingGroups.push(...nestedDeferred);
  }

  return { nodes, edges };
}

import { useCallback, useEffect, useMemo } from "react";
import {
  Background,
  Controls,
  MiniMap,
  ReactFlow,
  useEdgesState,
  useNodesState,
  type Node,
  type NodeTypes,
} from "@xyflow/react";
import type { WorkflowGraph } from "../types/workflow-graph-model";
import { graphToReactFlow, type WorkflowNodeData } from "../adapters/graph-to-reactflow";
import { ActivityNode } from "./nodes/ActivityNode";
import { BridgeNode } from "./nodes/BridgeNode";
import { ChildWorkflowNode } from "./nodes/ChildWorkflowNode";
import { ChildWorkflowGroupNode } from "./nodes/ChildWorkflowGroupNode";
import { StepNode } from "./nodes/StepNode";
import { SubflowNode } from "./nodes/SubflowNode";

interface WorkflowVisualizerProps {
  graph: WorkflowGraph;
  activityCounts: Record<string, number>;
  metricsLoading: boolean;
  metricsUnavailable: boolean;
}

const nodeTypes: NodeTypes = {
  step: StepNode,
  activity: ActivityNode,
  bridge: BridgeNode,
  childWorkflow: ChildWorkflowNode,
  childWorkflowGroup: ChildWorkflowGroupNode,
  subflow: SubflowNode,
};

const GROUP_TYPES = new Set(["subflow", "childWorkflowGroup"]);
const GROUP_PADDING = 40;
const GROUP_HEADER = 60;

type WNode = Node<WorkflowNodeData>;

function resizeGroups(nodes: WNode[]): WNode[] {
  const childrenByParent = new Map<string, string[]>();
  const nodeMap = new Map<string, WNode>();
  for (const n of nodes) {
    nodeMap.set(n.id, n);
    if (n.parentId) {
      let list = childrenByParent.get(n.parentId);
      if (!list) {
        list = [];
        childrenByParent.set(n.parentId, list);
      }
      list.push(n.id);
    }
  }

  const positionShifts = new Map<string, { dx: number; dy: number }>();
  const groupUpdates = new Map<string, { dx: number; dy: number; width: number; height: number }>();

  for (const [parentId, childIds] of childrenByParent) {
    const parent = nodeMap.get(parentId);
    if (!parent || !GROUP_TYPES.has(parent.type ?? "")) continue;

    const children = childIds.map((id) => nodeMap.get(id)!);

    let minX = Infinity;
    let minY = Infinity;
    let maxRight = -Infinity;
    let maxBottom = -Infinity;

    for (const child of children) {
      const cw = (child.style?.width as number) ?? (child.measured?.width ?? 200);
      const ch = (child.style?.height as number) ?? (child.measured?.height ?? 80);
      minX = Math.min(minX, child.position.x);
      minY = Math.min(minY, child.position.y);
      maxRight = Math.max(maxRight, child.position.x + cw);
      maxBottom = Math.max(maxBottom, child.position.y + ch);
    }

    // Always normalize: shift so minX = GROUP_PADDING, minY = GROUP_HEADER
    const shiftX = minX - GROUP_PADDING;
    const shiftY = minY - GROUP_HEADER;

    if (shiftX !== 0 || shiftY !== 0) {
      for (const childId of childIds) {
        positionShifts.set(childId, { dx: -shiftX, dy: -shiftY });
      }
    }

    const newWidth = (maxRight - shiftX) + GROUP_PADDING;
    const newHeight = (maxBottom - shiftY) + GROUP_PADDING;

    const oldWidth = parent.style?.width as number | undefined;
    const oldHeight = parent.style?.height as number | undefined;

    if (shiftX !== 0 || shiftY !== 0 || oldWidth !== newWidth || oldHeight !== newHeight) {
      groupUpdates.set(parentId, { dx: shiftX, dy: shiftY, width: newWidth, height: newHeight });
    }
  }

  if (groupUpdates.size === 0) return nodes;

  return nodes.map((n) => {
    const groupUpdate = groupUpdates.get(n.id);
    if (groupUpdate) {
      return {
        ...n,
        position: {
          x: n.position.x + groupUpdate.dx,
          y: n.position.y + groupUpdate.dy,
        },
        style: { ...n.style, width: groupUpdate.width, height: groupUpdate.height },
      };
    }

    const shift = positionShifts.get(n.id);
    if (shift) {
      return {
        ...n,
        position: {
          x: n.position.x + shift.dx,
          y: n.position.y + shift.dy,
        },
      };
    }

    return n;
  });
}

export function WorkflowVisualizer({
  graph,
  activityCounts,
  metricsLoading,
  metricsUnavailable,
}: WorkflowVisualizerProps) {
  const { nodes: initialNodes, edges: initialEdges } = useMemo(() => {
    const result = graphToReactFlow(graph);
    const mappedNodes = result.nodes.map((node) => {
      if (node.type !== "activity") {
        return node;
      }

      const activityType =
        typeof node.data.activityType === "string" ? node.data.activityType : undefined;
      const executionCount = activityType ? activityCounts[activityType] : undefined;

      return {
        ...node,
        data: {
          ...node.data,
          executionCount,
          metricsLoading,
          metricsUnavailable,
        },
      };
    });

    return { nodes: mappedNodes, edges: result.edges };
  }, [graph, activityCounts, metricsLoading, metricsUnavailable]);
  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

  useEffect(() => {
    setNodes(initialNodes);
    setEdges(initialEdges);
  }, [initialNodes, initialEdges, setNodes, setEdges]);

  const handleNodesChange = useCallback(
    (changes: Parameters<typeof onNodesChange>[0]) => {
      onNodesChange(changes);
      setNodes((currentNodes) => resizeGroups(currentNodes));
    },
    [onNodesChange, setNodes],
  );

  return (
    <ReactFlow
      nodes={nodes}
      edges={edges}
      nodeTypes={nodeTypes}
      onNodesChange={handleNodesChange}
      onEdgesChange={onEdgesChange}
      nodesDraggable
      fitView
    >
      <Background />
      <MiniMap />
      <Controls />
    </ReactFlow>
  );
}

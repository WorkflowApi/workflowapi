// §12 ReactFlow rendering — reactive canvas wrapper.
// Layout is memoised so Dagre only re-runs when the graph reference changes.
import { useMemo } from "react";
import { ReactFlow, Background, Controls, MiniMap } from "@xyflow/react";
import type { WorkflowGraph, WorkflowGraphNode } from "../workflowapi/graphTypes";
import { layoutGraph } from "./layoutGraph";
import WorkflowNode from "./WorkflowNode";
import type { WorkflowNodeData } from "./WorkflowNode";

export interface WorkflowGraphProps {
  graph: WorkflowGraph;
  selectedNodeId?: string;
  onNodeSelected: (node: WorkflowGraphNode | undefined) => void;
}

export default function WorkflowGraph({
  graph,
  selectedNodeId,
  onNodeSelected,
}: WorkflowGraphProps): JSX.Element {
  // Register once — stable reference keeps ReactFlow from re-mounting nodes
  const nodeTypes = useMemo(() => ({ workflow: WorkflowNode }), []);

  // Dagre layout — recomputed only when the graph reference changes
  const { nodes: layoutNodes, edges: rfEdges } = useMemo(
    () => layoutGraph(graph),
    [graph]
  );

  // Remap layout positions to ReactFlow nodes with the custom type + data shape
  const rfNodes = useMemo(
    () =>
      layoutNodes.map((lNode) => {
        const proj = graph.nodes.find((n) => n.id === lNode.id);
        return {
          ...lNode,
          type: "workflow",
          data: {
            label: proj?.label ?? lNode.id,
            kind: proj?.kind ?? "step",
            description: proj?.description,
          } satisfies WorkflowNodeData,
          selected: lNode.id === selectedNodeId,
        };
      }),
    [layoutNodes, graph.nodes, selectedNodeId]
  );

  return (
    <div className="w-full h-full">
      <ReactFlow
        nodes={rfNodes}
        edges={rfEdges}
        nodeTypes={nodeTypes}
        onNodeClick={(_evt, node) => {
          const found = graph.nodes.find((n) => n.id === node.id);
          onNodeSelected(found);
        }}
        onPaneClick={() => onNodeSelected(undefined)}
        fitView
      >
        <Background />
        <Controls />
        <MiniMap />
      </ReactFlow>
    </div>
  );
}

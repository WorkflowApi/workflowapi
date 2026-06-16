/**
 * §11 Layout — pure Dagre LR layout, no JSX, no React hooks.
 *
 * Accepts a `WorkflowGraph` and returns ReactFlow-compatible nodes and edges
 * with absolute positions.  Output is deterministic: same input → same layout.
 *
 * Node dimensions match the WorkflowNode component (220×80) so that Dagre
 * edge routing and ReactFlow handle positions stay aligned.
 */
import * as dagre from "dagre";
import type { Edge, Node } from "@xyflow/react";
import type { WorkflowGraph } from "../workflowapi/graphTypes";

const NODE_WIDTH = 220;
const NODE_HEIGHT = 80;

export function layoutGraph(graph: WorkflowGraph): { nodes: Node[]; edges: Edge[] } {
  const g = new dagre.graphlib.Graph();
  g.setDefaultEdgeLabel(() => ({}));
  g.setGraph({ rankdir: "LR", nodesep: 80, ranksep: 120 });

  // Register every node with its pixel dimensions (Dagre requires them)
  for (const node of graph.nodes) {
    g.setNode(node.id, { width: NODE_WIDTH, height: NODE_HEIGHT });
  }

  // Register edges (Dagre only needs source/target for layout)
  for (const edge of graph.edges) {
    g.setEdge(edge.source, edge.target);
  }

  dagre.layout(g);

  // Dagre returns centre-anchored positions; ReactFlow expects top-left origin
  const nodes: Node[] = graph.nodes.map((node) => {
    const pos = g.node(node.id) ?? { x: 0, y: 0 };
    return {
      id: node.id,
      type: "workflowNode",
      position: {
        x: pos.x - NODE_WIDTH / 2,
        y: pos.y - NODE_HEIGHT / 2,
      },
      data: node as unknown as Record<string, unknown>,
    };
  });

  const edges: Edge[] = graph.edges.map((edge) => ({
    id: edge.id,
    source: edge.source,
    target: edge.target,
    label: edge.label,
    data: { kind: edge.kind } as Record<string, unknown>,
  }));

  return { nodes, edges };
}

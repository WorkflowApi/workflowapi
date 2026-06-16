/**
 * §10 Graph projection — WorkflowAPI document → WorkflowGraph.
 *
 * Decisions applied (from plan.md):
 *   D6  – detect topology under BOTH topology.nodes AND topology.steps
 *   D7  – *Ref field support: activityRef, bridgeRef, workflowRef, subflowRef
 *   D8  – label priority adds displayName before title/name/key
 *   D9  – kind:subflow → step + info diagnostic
 *   D10 – kind:start / kind:end → step, no error
 *   D11 – bridge with unresolved $ref → node from key + warning diagnostic
 *   D12 – node ID format  <kind>:<key>  with ':' separator
 */
import type {
  WorkflowGraph,
  WorkflowGraphDiagnostic,
  WorkflowGraphEdge,
  WorkflowGraphNode,
  WorkflowNodeKind,
} from "./graphTypes";

// ─── tiny helpers ────────────────────────────────────────────────────────────

function isObj(val: unknown): val is Record<string, unknown> {
  return typeof val === "object" && val !== null && !Array.isArray(val);
}

function str(val: unknown): string | undefined {
  return typeof val === "string" ? val : undefined;
}

// ─── step kind inference (§10.5 + D7-D10) ───────────────────────────────────

function inferStepKind(
  step: Record<string, unknown>,
  diagnostics: WorkflowGraphDiagnostic[],
  stepKey: string,
  wfKey: string,
): WorkflowNodeKind {
  const t = str(step["kind"]) ?? str(step["type"]);

  if (t === "activity") return "activity";
  if (t === "childWorkflow") return "childWorkflow";
  if (t === "bridge") return "bridge";
  if (t === "subflow") {
    // D9: render as step, emit info diagnostic
    diagnostics.push({
      severity: "info",
      message: `Subflow step "${stepKey}" in workflow "${wfKey}" is not expanded in this view.`,
      sourcePath: `workflows.${wfKey}.topology.nodes.${stepKey}`,
    });
    return "step";
  }
  // D10: start/end render as generic step — no error
  if (t === "start" || t === "end") return "step";

  // D7: *Ref field support
  if (step["activityRef"] !== undefined) return "activity";
  if (step["bridgeRef"] !== undefined) return "bridge";
  if (step["workflowRef"] !== undefined) return "childWorkflow";
  if (step["subflowRef"] !== undefined) {
    // D9/D7
    diagnostics.push({
      severity: "info",
      message: `Subflow step "${stepKey}" in workflow "${wfKey}" is not expanded in this view.`,
      sourcePath: `workflows.${wfKey}.topology.nodes.${stepKey}`,
    });
    return "step";
  }

  // Spec §10.5 legacy field names
  if (step["activity"] !== undefined) return "activity";
  if (step["childWorkflow"] !== undefined || step["workflow"] !== undefined)
    return "childWorkflow";
  if (step["bridge"] !== undefined) return "bridge";

  return "step";
}

// ─── operations (§10.4) ──────────────────────────────────────────────────────

function projectOperations(
  result: WorkflowGraph,
  wfKey: string,
  workflow: Record<string, unknown>,
): void {
  const wfNodeId = `workflow:${wfKey}`;

  if (workflow["run"] !== undefined) {
    const id = `${wfNodeId}:operation:run`;
    result.nodes.push({
      id,
      kind: "operation",
      label: "run",
      sourcePath: `workflows.${wfKey}.run`,
      raw: workflow["run"],
    });
    result.edges.push({
      id: `${id}-edge`,
      kind: "starts",
      source: wfNodeId,
      target: id,
    });
  }

  if (workflow["start"] !== undefined) {
    const id = `${wfNodeId}:operation:start`;
    result.nodes.push({
      id,
      kind: "operation",
      label: "start",
      sourcePath: `workflows.${wfKey}.start`,
      raw: workflow["start"],
    });
    result.edges.push({
      id: `${id}-edge`,
      kind: "starts",
      source: wfNodeId,
      target: id,
    });
  }

  // signals
  if (isObj(workflow["signals"])) {
    const signals = workflow["signals"] as Record<string, unknown>;
    for (const [key, val] of Object.entries(signals)) {
      const id = `${wfNodeId}:operation:signal:${key}`;
      result.nodes.push({
        id,
        kind: "operation",
        label: `signal: ${key}`,
        description: isObj(val) ? str(val["summary"]) : undefined,
        sourcePath: `workflows.${wfKey}.signals.${key}`,
        raw: val,
      });
      result.edges.push({ id: `${id}-edge`, kind: "contains", source: wfNodeId, target: id });
    }
  }

  // queries
  if (isObj(workflow["queries"])) {
    const queries = workflow["queries"] as Record<string, unknown>;
    for (const [key, val] of Object.entries(queries)) {
      const id = `${wfNodeId}:operation:query:${key}`;
      result.nodes.push({
        id,
        kind: "operation",
        label: `query: ${key}`,
        description: isObj(val) ? str(val["summary"]) : undefined,
        sourcePath: `workflows.${wfKey}.queries.${key}`,
        raw: val,
      });
      result.edges.push({ id: `${id}-edge`, kind: "contains", source: wfNodeId, target: id });
    }
  }

  // updates
  if (isObj(workflow["updates"])) {
    const updates = workflow["updates"] as Record<string, unknown>;
    for (const [key, val] of Object.entries(updates)) {
      const id = `${wfNodeId}:operation:update:${key}`;
      result.nodes.push({
        id,
        kind: "operation",
        label: `update: ${key}`,
        description: isObj(val) ? str(val["summary"]) : undefined,
        sourcePath: `workflows.${wfKey}.updates.${key}`,
        raw: val,
      });
      result.edges.push({ id: `${id}-edge`, kind: "contains", source: wfNodeId, target: id });
    }
  }

  // generic operations map (avoid duplicating run/start/signals/queries/updates)
  if (isObj(workflow["operations"])) {
    const operations = workflow["operations"] as Record<string, unknown>;
    for (const [key, val] of Object.entries(operations)) {
      const id = `${wfNodeId}:operation:${key}`;
      if (result.nodes.some((n) => n.id === id)) continue;
      result.nodes.push({
        id,
        kind: "operation",
        label: (isObj(val) && str(val["operationId"])) ? str(val["operationId"])! : key,
        sourcePath: `workflows.${wfKey}.operations.${key}`,
        raw: val,
      });
      result.edges.push({ id: `${id}-edge`, kind: "contains", source: wfNodeId, target: id });
    }
  }
}

// ─── steps (§10.5 + D6) ──────────────────────────────────────────────────────

/**
 * Projects topology nodes/steps into graph nodes.
 * Returns a Map<stepKey, nodeId> for use by edge projection.
 */
function projectSteps(
  result: WorkflowGraph,
  wfKey: string,
  workflow: Record<string, unknown>,
  bridgeNodeIdMap: Map<string, string>,
): Map<string, string> {
  const wfNodeId = `workflow:${wfKey}`;
  const stepMap = new Map<string, string>();

  const topology = isObj(workflow["topology"])
    ? (workflow["topology"] as Record<string, unknown>)
    : undefined;

  // D6: prefer topology.nodes, fall back to topology.steps, workflow.steps, workflow.activities
  let stepsObj: Record<string, unknown> | undefined;
  let basePath: string;

  if (topology && isObj(topology["nodes"])) {
    stepsObj = topology["nodes"] as Record<string, unknown>;
    basePath = `workflows.${wfKey}.topology.nodes`;
  } else if (topology && isObj(topology["steps"])) {
    stepsObj = topology["steps"] as Record<string, unknown>;
    basePath = `workflows.${wfKey}.topology.steps`;
  } else if (isObj(workflow["steps"])) {
    stepsObj = workflow["steps"] as Record<string, unknown>;
    basePath = `workflows.${wfKey}.steps`;
  } else if (isObj(workflow["activities"])) {
    stepsObj = workflow["activities"] as Record<string, unknown>;
    basePath = `workflows.${wfKey}.activities`;
  } else {
    return stepMap; // no steps found
  }

  for (const [stepKey, stepVal] of Object.entries(stepsObj)) {
    if (!isObj(stepVal)) continue;
    const step = stepVal as Record<string, unknown>;
    const nodeId = `${wfNodeId}:step:${stepKey}`;
    stepMap.set(stepKey, nodeId);

    const kind = inferStepKind(step, result.diagnostics, stepKey, wfKey);
    // D8: label priority — displayName > key
    const label = str(step["displayName"]) ?? stepKey;

    const node: WorkflowGraphNode = {
      id: nodeId,
      kind,
      label,
      description: str(step["summary"]) ?? str(step["description"]),
      sourcePath: `${basePath}.${stepKey}`,
      raw: step,
    };
    result.nodes.push(node);

    // Connect bridge steps to top-level bridge nodes (§10.7)
    if (kind === "bridge") {
      const rawRef = str(step["bridgeRef"]);
      if (rawRef) {
        // D12/plan: match by string equality; also strip leading 'bridges.' prefix
        const cleanKey = rawRef.replace(/^bridges\./, "");
        const bridgeNodeId =
          bridgeNodeIdMap.get(cleanKey) ?? bridgeNodeIdMap.get(rawRef);
        if (bridgeNodeId) {
          result.edges.push({
            id: `${nodeId}->usesBridge->${bridgeNodeId}`,
            kind: "usesBridge",
            source: nodeId,
            target: bridgeNodeId,
          });
        }
      }
    }
  }

  return stepMap;
}

// ─── declared edges (§10.6) ──────────────────────────────────────────────────

function projectEdges(
  result: WorkflowGraph,
  wfKey: string,
  workflow: Record<string, unknown>,
  stepMap: Map<string, string>,
): void {
  const wfNodeId = `workflow:${wfKey}`;
  const topology = isObj(workflow["topology"])
    ? (workflow["topology"] as Record<string, unknown>)
    : undefined;

  // Detect declared edge arrays from known locations
  let edgesArr: unknown[] | undefined;
  let edgesPath = `workflows.${wfKey}.topology.edges`;

  if (topology && Array.isArray(topology["edges"])) {
    edgesArr = topology["edges"] as unknown[];
  } else if (Array.isArray(workflow["edges"])) {
    edgesArr = workflow["edges"] as unknown[];
    edgesPath = `workflows.${wfKey}.edges`;
  } else if (Array.isArray(workflow["transitions"])) {
    edgesArr = workflow["transitions"] as unknown[];
    edgesPath = `workflows.${wfKey}.transitions`;
  }

  if (edgesArr && edgesArr.length > 0) {
    // §10.6 declared edges
    for (const edgeVal of edgesArr) {
      if (!isObj(edgeVal)) continue;
      const e = edgeVal as Record<string, unknown>;
      // Support both from/to and source/target shapes
      const srcKey = str(e["from"]) ?? str(e["source"]);
      const tgtKey = str(e["to"]) ?? str(e["target"]);
      if (!srcKey || !tgtKey) continue;

      const srcId = stepMap.get(srcKey);
      const tgtId = stepMap.get(tgtKey);

      if (!srcId) {
        result.diagnostics.push({
          severity: "warning",
          message: `Edge in workflow "${wfKey}" references unknown source step "${srcKey}".`,
          sourcePath: edgesPath,
        });
        continue;
      }
      if (!tgtId) {
        result.diagnostics.push({
          severity: "warning",
          message: `Edge in workflow "${wfKey}" references unknown target step "${tgtKey}".`,
          sourcePath: edgesPath,
        });
        continue;
      }

      const edge: WorkflowGraphEdge = {
        id: `${srcId}->${tgtId}`,
        kind: "continuesTo",
        source: srcId,
        target: tgtId,
        raw: edgeVal,
      };
      result.edges.push(edge);
    }
  } else if (stepMap.size > 0) {
    // No declared edges → default workflow→step edges
    for (const stepNodeId of stepMap.values()) {
      result.edges.push({
        id: `${wfNodeId}->${stepNodeId}`,
        kind: "contains",
        source: wfNodeId,
        target: stepNodeId,
      });
    }
  }
}

// ─── public entry point ───────────────────────────────────────────────────────

/**
 * Projects a parsed WorkflowAPI document (from `parseWorkflowApiYaml`) into a
 * `WorkflowGraph`.  Never throws — all problems are pushed into `diagnostics`.
 */
export function workflowApiToGraph(doc: unknown): WorkflowGraph {
  const result: WorkflowGraph = { nodes: [], edges: [], diagnostics: [] };

  if (!isObj(doc)) return result;

  // §10.1 Document node
  const docId = str(doc["id"]) ?? "workflowapi-document";
  const docLabel =
    (isObj(doc["info"]) ? str(doc["info"]["title"]) : undefined) ??
    str(doc["title"]) ??
    str(doc["id"]) ??
    "WorkflowAPI Document";
  const docNodeId = `document:${docId}`;
  result.documentId = docNodeId;
  result.title = docLabel;
  result.nodes.push({
    id: docNodeId,
    kind: "document",
    label: docLabel,
    description: isObj(doc["info"])
      ? (str(doc["info"]["summary"]) ?? str(doc["info"]["description"]))
      : undefined,
    sourcePath: "document",
    raw: doc,
  });

  // §10.2 Host node
  let parentNodeId = docNodeId;
  if (isObj(doc["host"])) {
    const host = doc["host"] as Record<string, unknown>;
    const hostId = str(host["id"]) ?? str(host["name"]) ?? "default";
    const hostNodeId = `host:${hostId}`;
    result.nodes.push({
      id: hostNodeId,
      kind: "host",
      label: str(host["name"]) ?? str(host["id"]) ?? "Host",
      sourcePath: "host",
      raw: host,
    });
    result.edges.push({
      id: `${docNodeId}->${hostNodeId}`,
      kind: "contains",
      source: docNodeId,
      target: hostNodeId,
    });
    parentNodeId = hostNodeId;
  }

  // §10.7 Top-level bridges — create nodes before workflow steps so bridge refs can be resolved
  const bridgeNodeIdMap = new Map<string, string>(); // bridgeKey → nodeId
  if (isObj(doc["bridges"])) {
    const bridges = doc["bridges"] as Record<string, unknown>;
    for (const [key, bridge] of Object.entries(bridges)) {
      const nodeId = `bridge:${key}`;
      bridgeNodeIdMap.set(key, nodeId);

      if (isObj(bridge) && bridge["$ref"] !== undefined) {
        // D11: $ref we cannot resolve → create node from key + warning
        result.nodes.push({
          id: nodeId,
          kind: "bridge",
          label: key,
          sourcePath: `bridges.${key}`,
          raw: bridge,
        });
        result.diagnostics.push({
          severity: "warning",
          message: `Bridge "${key}" has an unresolved $ref: ${str(bridge["$ref"]) ?? "(unknown)"}`,
          sourcePath: `bridges.${key}`,
        });
      } else if (isObj(bridge)) {
        result.nodes.push({
          id: nodeId,
          kind: "bridge",
          label: str(bridge["displayName"]) ?? str(bridge["name"]) ?? key,
          description: str(bridge["summary"]) ?? str(bridge["description"]),
          sourcePath: `bridges.${key}`,
          raw: bridge,
        });
      }
    }
  }

  // §10.3 Workflows
  if (isObj(doc["workflows"])) {
    const workflows = doc["workflows"] as Record<string, unknown>;
    for (const [wfKey, wfVal] of Object.entries(workflows)) {
      if (!isObj(wfVal)) continue;
      const workflow = wfVal as Record<string, unknown>;
      const wfNodeId = `workflow:${wfKey}`;

      // D8: label priority — displayName > title > name > key
      const wfLabel =
        str(workflow["displayName"]) ??
        str(workflow["title"]) ??
        str(workflow["name"]) ??
        wfKey;

      result.nodes.push({
        id: wfNodeId,
        kind: "workflow",
        label: wfLabel,
        description: str(workflow["summary"]) ?? str(workflow["description"]),
        sourcePath: `workflows.${wfKey}`,
        raw: workflow,
      });
      result.edges.push({
        id: `${parentNodeId}->${wfNodeId}`,
        kind: "contains",
        source: parentNodeId,
        target: wfNodeId,
      });

      // §10.4 Operations
      projectOperations(result, wfKey, workflow);

      // §10.5 Steps (D6 inside)
      const stepMap = projectSteps(result, wfKey, workflow, bridgeNodeIdMap);

      // §10.6 Declared edges
      projectEdges(result, wfKey, workflow, stepMap);
    }
  }

  return result;
}

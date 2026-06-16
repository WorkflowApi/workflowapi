/**
 * Projection unit tests — 10 tests, no snapshots, no React/Dagre.
 * Discipline: assertions only; inline YAML only for ≤5 lines; fixtures for larger.
 */
import { readFileSync } from "fs";
import { dirname, resolve } from "path";
import { fileURLToPath } from "url";
import { describe, expect, it } from "vitest";
import { parseWorkflowApiYaml } from "../src/workflowapi/parseWorkflowApiYaml";
import { workflowApiToGraph } from "../src/workflowapi/workflowApiToGraph";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

function loadFixture(name: string): unknown {
  const raw = readFileSync(resolve(__dirname, `../tests/fixtures/${name}`), "utf-8");
  return parseWorkflowApiYaml(raw);
}

// Load the real risk-enrichment YAML once for tests 1–7
const riskDoc = parseWorkflowApiYaml(
  readFileSync(
    resolve(__dirname, "../src/examples/risk-enrichment.workflowapi.yaml"),
    "utf-8",
  ),
);
const riskGraph = workflowApiToGraph(riskDoc);

// ─── §10.3 workflow projection ───────────────────────────────────────────────

describe("workflowApiToGraph", () => {
  it("produces a workflow node for each workflows entry", () => {
    // §10.3 workflow projection
    const wfNodes = riskGraph.nodes.filter((n) => n.kind === "workflow");
    expect(wfNodes.length).toBeGreaterThanOrEqual(3);
    const ids = wfNodes.map((n) => n.id);
    expect(ids).toContain("workflow:RiskEnrichmentWorkflow");
    expect(ids).toContain("workflow:CalculateRiskWorkflow");
    expect(ids).toContain("workflow:ExternalChecksWorkflow");
  });

  // ─── §10.4 operation projection ───────────────────────────────────────────

  it("creates a run operation node from workflow.run", () => {
    // §10.4 operation projection
    const runNode = riskGraph.nodes.find(
      (n) => n.id === "workflow:RiskEnrichmentWorkflow:operation:run",
    );
    expect(runNode).toBeDefined();
    expect(runNode?.kind).toBe("operation");
  });

  it("creates signal, query, and update operation nodes", () => {
    // §10.4 operation projection
    const opPrefix = "workflow:RiskEnrichmentWorkflow:operation:";
    const opNodes = riskGraph.nodes.filter(
      (n) => n.id.startsWith(opPrefix) && n.kind === "operation",
    );
    // run + 2 signals + 1 query + 1 update = 5 total
    expect(opNodes.length).toBeGreaterThanOrEqual(4);
    expect(opNodes.some((n) => n.label.startsWith("signal:"))).toBe(true);
    expect(opNodes.some((n) => n.label.startsWith("query:"))).toBe(true);
    expect(opNodes.some((n) => n.label.startsWith("update:"))).toBe(true);
  });

  // ─── §10.5 step projection ────────────────────────────────────────────────

  it("creates step nodes from topology.nodes (D6)", () => {
    // §10.5 step projection — D6 ensures topology.nodes is detected
    const prefix = "workflow:RiskEnrichmentWorkflow:step:";
    const steps = riskGraph.nodes.filter((n) => n.id.startsWith(prefix));
    expect(steps.length).toBe(7);
    const keys = steps.map((n) => n.id.slice(prefix.length));
    expect(keys).toContain("start");
    expect(keys).toContain("identify-company");
    expect(keys).toContain("enrich-dnb-data");
    expect(keys).toContain("calculate-risk");
    expect(keys).toContain("document-pdf");
    expect(keys).toContain("publish-result");
    expect(keys).toContain("end");
  });

  it("infers bridge kind from topology node kind=bridge", () => {
    // §10.5 step projection — D7 kind inference
    const node = riskGraph.nodes.find(
      (n) => n.id === "workflow:RiskEnrichmentWorkflow:step:enrich-dnb-data",
    );
    expect(node).toBeDefined();
    expect(node?.kind).toBe("bridge");
  });

  // ─── §10.6 declared edges ─────────────────────────────────────────────────

  it("creates declared edges from topology.edges", () => {
    // §10.6 declared edges — from/to shape
    const prefix = "workflow:RiskEnrichmentWorkflow:step:";
    const stepEdges = riskGraph.edges.filter(
      (e) => e.source.startsWith(prefix) && e.target.startsWith(prefix),
    );
    expect(stepEdges.length).toBeGreaterThanOrEqual(6);
    const startToIdentify = stepEdges.find(
      (e) => e.source === `${prefix}start` && e.target === `${prefix}identify-company`,
    );
    expect(startToIdentify).toBeDefined();
  });

  it("skips edge and emits diagnostic when target step is missing", () => {
    // §10.6 declared edges — missing target emits warning
    const graph = workflowApiToGraph(loadFixture("bad-edge-ref.yaml"));
    const badEdge = graph.edges.find((e) => e.target.includes("missing-step"));
    expect(badEdge).toBeUndefined();
    const warnings = graph.diagnostics.filter((d) => d.severity === "warning");
    expect(warnings.length).toBeGreaterThan(0);
    expect(warnings.some((d) => d.message.includes("missing-step"))).toBe(true);
  });

  // ─── §10.1 permissive parsing ─────────────────────────────────────────────

  it("does not throw on unknown top-level fields", () => {
    // §10.1 permissive parsing
    const doc = loadFixture("workflow-with-topology.yaml");
    expect(() => workflowApiToGraph(doc)).not.toThrow();
    const graph = workflowApiToGraph(doc);
    // Graph is produced even though unknownTopLevel is present
    expect(graph.nodes.length).toBeGreaterThan(0);
  });

  // ─── §10.1 empty document ─────────────────────────────────────────────────

  it("empty workflows map produces document node and no workflow nodes", () => {
    // §10.1 document node
    const graph = workflowApiToGraph(loadFixture("empty-document.yaml"));
    const docNode = graph.nodes.find((n) => n.kind === "document");
    expect(docNode).toBeDefined();
    const wfNodes = graph.nodes.filter((n) => n.kind === "workflow");
    expect(wfNodes.length).toBe(0);
  });

  // ─── §10.5 no topology ────────────────────────────────────────────────────

  it("no topology produces no step nodes and does not crash", () => {
    // §10.5 step projection — absence of topology is safe
    expect(() => workflowApiToGraph(loadFixture("workflow-only.yaml"))).not.toThrow();
    const graph = workflowApiToGraph(loadFixture("workflow-only.yaml"));
    const stepKinds = new Set(["step", "activity", "bridge", "childWorkflow"]);
    const stepNodes = graph.nodes.filter((n) => stepKinds.has(n.kind));
    expect(stepNodes.length).toBe(0);
  });
});

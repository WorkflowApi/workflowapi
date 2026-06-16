# Agentic Coding Agent Spec: WorkflowAPI Visualiser MVP

## 1. Objective

Build a minimal React-based MVP that visualises a single WorkflowAPI YAML example as an interactive workflow graph.

The MVP should prove that a WorkflowAPI document can be transformed into a useful, navigable graph showing workflows, operations, steps, activities, child workflows, and bridges where present.

This is a Hackweek prototype, not the final WorkflowAPI reference UI.

## 2. Context

WorkflowAPI is intended to describe durable workflow APIs, sitting conceptually beside OpenAPI and AsyncAPI. Existing internal design material separates the WorkflowAPI document model, reference UI, central catalogue, runtime overlays, and conformance tooling.

For this MVP, ignore the full product architecture. The goal is only to take one YAML example and render it as a graph.

The EventCatalog visualiser package is useful inspiration because it is described as a standalone React visualiser component, while EventCatalog itself is broader and models distributed systems with domains, services, events, schemas, and flows. The visualiser is built around ReactFlow, Dagre layout, node and edge rendering, focus/navigation concepts, and diagram export capabilities. [[github.com]](https://github.com/event-catalog/eventcatalog) [[deepwiki.com]](https://deepwiki.com/event-catalog/eventcatalog/3.1-node-graph-visualizer)

For Hackweek, prefer **Vite + React + TypeScript** over Next.js. The MVP is a client-side interactive graph, so avoid framework overhead unless explicitly needed later.

## 3. Non-goals

Do **not** implement:

- Next.js application shell
- full WorkflowAPI schema validation
- multi-document catalogue merge
- runtime overlays
- Temporal API integration
- Temporal Web replacement
- editing or authoring UI
- BPMN-style modelling
- authentication
- persistence
- package publishing
- polished production UI
- support for every possible WorkflowAPI field

Unsupported YAML fields should be ignored gracefully.

## 4. Technology choices

Use:

```text
Vite
React
TypeScript
@xyflow/react
dagre
yaml
lucide-react
```

Optional but acceptable:

```text
tailwindcss
clsx
```

Avoid adding large UI frameworks unless needed.

## 5. Repository structure

Create a standalone MVP app:

```text
workflowapi-visualiser-mvp/
  package.json
  vite.config.ts
  tsconfig.json
  index.html
  src/
    App.tsx
    main.tsx
    examples/
      risk-enrichment.workflowapi.yaml
    workflowapi/
      parseWorkflowApiYaml.ts
      workflowApiToGraph.ts
      graphTypes.ts
    visualiser/
      WorkflowGraph.tsx
      WorkflowNode.tsx
      NodeDetailsPanel.tsx
      layoutGraph.ts
      visualStyles.ts
    test/
      workflowApiToGraph.test.ts
```

If the repository already exists, add this under:

```text
src/web/workflowapi-visualiser-mvp/
```

or:

```text
samples/workflowapi-visualiser-mvp/
```

Choose the location that best fits the current repo layout.

## 6. Example input

Use one example YAML file only.

Preferred first example:

[https://github.com/WorkflowApi/workflowapi/blob/main/examples/risk-enrichment.workflowapi.yaml](https://github.com/WorkflowApi/workflowapi/blob/main/examples/risk-enrichment.workflowapi.yaml)

If bridge rendering is more important for the demo, use:

[https://raw.githubusercontent.com/WorkflowApi/workflowapi/refs/heads/main/examples/document-service-generate-pdf.bridge.workflowapi.yaml](https://raw.githubusercontent.com/WorkflowApi/workflowapi/refs/heads/main/examples/document-service-generate-pdf.bridge.workflowapi.yaml)

For the MVP, copy the selected YAML into:

```text
src/examples/risk-enrichment.workflowapi.yaml
```

Do not fetch from GitHub at runtime. Keep the app deterministic and offline-capable.

## 7. MVP user experience

The app should show:

```text
Header
  "WorkflowAPI Visualiser MVP"

Main area
  Left: workflow graph
  Right: node details panel

Footer or small debug area
  selected YAML file name
  node count
  edge count
```

Behaviour:

1. App loads the bundled YAML file.
2. YAML is parsed in the browser.
3. Parsed document is converted to a graph model.
4. Graph renders using ReactFlow.
5. User can pan, zoom, drag nodes, and fit view.
6. User can click a node.
7. Details panel shows:
    - node kind
    - label
    - description if present
    - source id/path
    - raw source fragment as JSON

## 8. Minimal graph model

Create `src/workflowapi/graphTypes.ts`:

```ts
export type WorkflowNodeKind =
  | "document"
  | "host"
  | "workflow"
  | "operation"
  | "activity"
  | "step"
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
```

## 9. YAML parser

Create `src/workflowapi/parseWorkflowApiYaml.ts`:

```ts
import YAML from "yaml";

export function parseWorkflowApiYaml(source: string): unknown {
  return YAML.parse(source);
}
```

This parser should throw if the YAML is invalid. App-level code should catch errors and render a readable error message.

## 10. Graph projection rules

Create `src/workflowapi/workflowApiToGraph.ts`.

The projection must be permissive. It should support likely shapes without requiring the full schema.

### 10.1 Top-level document

If present, create a document node:

```text
document:<document id or "workflowapi-document">
```

Label priority:

```text
document.info.title
document.title
document.id
"WorkflowAPI Document"
```

### 10.2 Host

If `host` exists, create a host node:

```text
host:<host id or host name or "default">
```

Add an edge:

```text
document -> host
```

### 10.3 Workflows

For each entry in `document.workflows`:

```text
workflow:<workflow key>
```

Label priority:

```text
workflow.title
workflow.name
workflow key
```

Add an edge:

```text
host -> workflow
```

If no host exists:

```text
document -> workflow
```

### 10.4 Operations

For each workflow, detect operations from any of:

```text
workflow.operations
workflow.run
workflow.start
workflow.signals
workflow.queries
workflow.updates
```

Create operation nodes:

```text
workflow:<workflow key>:operation:<operation key>
```

Operation labels should include operation type if available:

```text
run
start
signal: <name>
query: <name>
update: <name>
```

Add edges:

```text
workflow -> operation
```

Use edge kind:

```text
starts
```

for run/start, otherwise:

```text
contains
```

### 10.5 Steps

Detect steps from any of:

```text
workflow.steps
workflow.topology.steps
workflow.activities
```

For each step:

```text
workflow:<workflow key>:step:<step key>
```

Infer node kind:

```ts
if type/kind === "activity" => activity
if type/kind === "childWorkflow" => childWorkflow
if type/kind === "bridge" => bridge
if step.activity exists => activity
if step.childWorkflow or step.workflow exists => childWorkflow
if step.bridge exists => bridge
otherwise => step
```

Add a default edge from workflow to step only if there are no declared edges for that workflow.

### 10.6 Declared edges

Detect declared edges from any of:

```text
workflow.edges
workflow.topology.edges
workflow.transitions
```

Support edge shapes:

```yaml
- source: a
  target: b
```

```yaml
- from: a
  to: b
```

```yaml
- id: a-to-b
  source: a
  target: b
```

Create graph edges:

```text
workflow:<workflow key>:step:<source> -> workflow:<workflow key>:step:<target>
```

If source or target nodes are missing, create a diagnostic warning and skip the edge.

### 10.7 Bridges

Detect top-level bridges from:

```text
document.bridges
```

Create bridge nodes:

```text
bridge:<bridge key>
```

If a step references a bridge by key, connect:

```text
step -> bridge
```

If a bridge declares a target workflow, create an external workflow node if needed:

```text
external:<target workflow id>
```

and connect:

```text
bridge -> external
```

Do not over-model bridge semantics. The visual distinction is enough for the MVP.

## 11. Layout

Create `src/visualiser/layoutGraph.ts`.

Use Dagre to calculate positions.

Default layout:

```text
rankdir: LR
nodesep: 80
ranksep: 120
```

Node dimensions:

```text
width: 220
height: 80
```

The layout function should accept graph nodes and edges and return ReactFlow-compatible nodes and edges.

## 12. ReactFlow rendering

Create `src/visualiser/WorkflowGraph.tsx`.

Requirements:

- render `ReactFlow`
- include `Background`
- include `Controls`
- include `MiniMap`
- use custom node component
- call `fitView`
- allow click selection

Pseudo-interface:

```tsx
export interface WorkflowGraphProps {
  graph: WorkflowGraph;
  selectedNodeId?: string;
  onNodeSelected: (node: WorkflowGraphNode | undefined) => void;
}
```

## 13. Node component

Create `src/visualiser/WorkflowNode.tsx`.

Show:

- icon by kind
- label
- kind
- short description if present
- coloured left border or top badge

Colour mapping:

```ts
document: "#334155"
host: "#475569"
workflow: "#6366f1"
operation: "#22c55e"
activity: "#14b8a6"
step: "#64748b"
childWorkflow: "#8b5cf6"
bridge: "#f43f5e"
external: "#94a3b8"
diagnostic: "#f59e0b"
```

Suggested icons from `lucide-react`:

```text
workflow: GitBranch
operation: PlayCircle
activity: Cog
childWorkflow: Workflow
bridge: Cable
external: ExternalLink
host: Server
document: FileText
diagnostic: TriangleAlert
```

## 14. Details panel

Create `src/visualiser/NodeDetailsPanel.tsx`.

If no node is selected, show:

```text
Select a node to inspect its WorkflowAPI source fragment.
```

If selected, show:

```text
Label
Kind
Description
Source path
Raw JSON
```

Render raw JSON in a `<pre>` block.

## 15. App composition

Create `src/App.tsx`.

Responsibilities:

1. Import YAML as raw text.
2. Parse YAML.
3. Convert to graph.
4. Store selected node state.
5. Render header, graph, details panel, diagnostics.

For Vite raw import:

```ts
import exampleYaml from "./examples/risk-enrichment.workflowapi.yaml?raw";
```

Error handling:

- invalid YAML should show a clear error card
- projection warnings should show in a small diagnostics section
- warnings must not prevent graph rendering

## 16. Tests

Add basic unit tests for the graph projection.

Required tests:

```text
parses document and creates at least one workflow node
creates operation nodes when operations/run/start are present
creates step nodes when steps/topology.steps are present
creates declared edges when topology edges are present
does not throw on unknown fields
adds diagnostics for missing edge source/target
```

Use Vitest if the project has no existing test framework:

```bash
pnpm add -D vitest
```

## 17. Acceptance criteria

The implementation is complete when:

```text
Given the selected WorkflowAPI YAML example
When the app starts
Then a graph is rendered without manual input

Given the graph is rendered
When the user clicks a node
Then the details panel shows that node's kind, label, and raw source fragment

Given the YAML contains workflows
Then at least one workflow node is visible

Given the YAML contains steps, activities, child workflows, or bridges
Then those concepts are rendered as distinguishable node types where detectable

Given unsupported YAML fields
Then the app ignores them without failing

Given a malformed declared edge
Then the app records a warning diagnostic instead of crashing

Given a browser refresh
Then the graph layout is deterministic enough for demo purposes
```

## 18. Definition of done

The coding agent must deliver:

- working Vite React app
- bundled example YAML
- YAML parser
- WorkflowAPI-to-graph projection
- ReactFlow visualisation
- custom node styling
- click-to-inspect details panel
- diagnostics display
- basic projection tests
- README with run instructions

README must include:

```bash
pnpm install
pnpm dev
pnpm test
```

## 19. README text

Use this README skeleton:

````md
# WorkflowAPI Visualiser MVP

Hackweek MVP for rendering a single WorkflowAPI YAML document as an interactive workflow graph.

## Scope

This prototype loads one bundled WorkflowAPI YAML file, projects it into a small graph model, and renders it with ReactFlow.

## Non-goals

This is not the final WorkflowAPI reference UI. It does not implement catalogue merge, runtime overlays, Temporal integration, authentication, editing, or full schema validation.

## Run

~~~bash
pnpm install
pnpm dev
~~~

## Test

```bash
pnpm test
```

## Current example

The app currently visualises:

`src/examples/risk-enrichment.workflowapi.yaml`

## Architecture

```text
WorkflowAPI YAML
  -> YAML parser
  -> permissive graph projection
  -> ReactFlow graph
  -> node details panel
```

```

## 20. Implementation guidance for the coding agent

Prioritise working software over abstraction.

Do:

- keep types small
- keep the graph projection isolated
- tolerate unknown fields
- render partial graphs rather than failing
- use deterministic ids
- keep node styles simple
- make bridges visually obvious
- add tests around the projection, not ReactFlow internals

Do not:

- introduce Next.js
- introduce a backend
- introduce runtime calls
- overfit to Temporal
- create a generic catalogue server
- spend time on perfect visual design
- block rendering because schema validation is incomplete
- refactor into many packages during the MVP

## 21. Preferred first milestone

Implement in this order:

1. Vite app boots.
2. YAML raw import works.
3. YAML parser works.
4. Graph projection returns nodes and edges.
5. ReactFlow renders basic graph.
6. Dagre layout added.
7. Custom node styles added.
8. Details panel added.
9. Diagnostics added.
10. Projection tests added.
11. README added.

## 22. Final note for the agent

The point of this MVP is to answer one question:

> Can a WorkflowAPI YAML document become a useful, navigable workflow graph?

If the answer is visible in the browser, the MVP has succeeded.

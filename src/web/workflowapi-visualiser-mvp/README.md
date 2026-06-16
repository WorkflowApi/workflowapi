# WorkflowAPI Visualiser MVP

This is a hackweek prototype that answers one question: _Can a WorkflowAPI YAML document become a useful, navigable workflow graph?_ It loads a single bundled WorkflowAPI YAML file, projects it into a typed graph model, and renders it with ReactFlow — giving teams a lightweight, zero-backend way to explore workflow topology, signals, queries, updates, and bridges at a glance.

## Scope

Hackweek prototype only. Loads one bundled YAML (`src/examples/risk-enrichment.workflowapi.yaml`), projects it into a small graph model using a React-free projection module, and renders it interactively with ReactFlow.

## Non-goals

This is not the final WorkflowAPI reference UI. It does not implement:

- Multi-document catalogue merge
- Runtime overlays or plugin system
- Temporal API integration or Temporal Web replacement
- Authentication or persistence
- Editing or authoring UI
- BPMN-style modelling
- Full WorkflowAPI schema validation or `$ref` resolution across files
- Package publishing or polished production UI

Unsupported YAML fields are ignored gracefully.

## Run

```bash
pnpm install
pnpm dev
```

Opens at `http://localhost:5173/`

## Test

```bash
pnpm test
```

10 Vitest unit tests cover the YAML → graph projection (`src/workflowapi/`). Tests run against YAML fixtures in `tests/fixtures/`.

## Build

```bash
pnpm build
```

Outputs a static bundle to `dist/` via Vite + `tsc -b`.

## Current example

The app visualises:

```text
src/examples/risk-enrichment.workflowapi.yaml
```

Contains 3 workflows, 2 bridges, and a mix of signals, queries, and updates — enough to exercise the full projection pipeline.

## Architecture

```text
WorkflowAPI YAML
  -> YAML parser          (yaml v2)
  -> permissive graph projection  (src/workflowapi/ — React-free, portable)
  -> ReactFlow graph      (@xyflow/react v12 + dagre layout)
  -> node details panel   (click any node to inspect its fields)
```

`src/workflowapi/` is intentionally React-free so the projection logic can be reused or tested independently of the rendering layer.

## Tech stack

- **Vite** — dev server and bundler
- **React 19** + **TypeScript** — UI layer
- **@xyflow/react v12** — graph canvas and edge routing
- **dagre** — automatic graph layout
- **yaml** — permissive YAML parsing
- **lucide-react** — icons
- **Tailwind CSS v4** — utility styling
- **Vitest** — unit tests

## File map

```text
src/
  workflowapi/        # React-free YAML → graph projection (portable, tested)
  visualiser/         # ReactFlow rendering, custom node component, details panel
  examples/           # Bundled YAML demo input
  App.tsx             # Composition: parse → project → render
test/                 # Vitest unit tests for the projection module
tests/fixtures/       # YAML fixtures used by tests
```

## Trying another YAML

Drop a WorkflowAPI YAML into `src/examples/`, update the import in `App.tsx` to point at the new file, then restart the dev server.

## What's NOT here

- No schema validation — invalid fields are silently ignored
- No `$ref` resolution across multiple files
- No Temporal runtime connection or live workflow data
- No persistence — everything resets on page reload

For the eventual reference UI design (catalogue, runtime overlays, Temporal integration), see `/docs/specs/` in this repository.

# Squad Decisions

## Active Decisions

### 2026-06-17 — WorkflowAPI v1 MVP Team Evaluation (requested by Rebecca Powell)

Seven agents produced independent readiness assessments for v1 MVP (Holden first-pass plan; Naomi spec/schema; Amos .NET implementation; Alex Temporal binding + UI; Miller conformance/validation; Bobbie security; Holden final synthesis was still in flight at archive time and will be merged on a second Scribe pass).

**Headline verdicts (consolidated):**

- **Naomi (spec) — NOT READY (yellow/red).** Conceptual coverage is complete, but normative artifacts are mutually inconsistent: three different `workflowApi` versions across spec/schema/examples, on-disk examples do not validate against the schema, bridge shape is flat-vs-nested inconsistent, doc 09 §20–§28 still describes post-v1 surface. Atomic spec freeze required before .NET work starts.
- **Amos (.NET) — Implementation-ready once spec freezes; ~3–4 weeks for 6 MVP packages.** Zero code exists today. MVP set: `Abstractions`, `AspNetCore`, `Temporal`, `AspNetCore.Temporal`, `Reference`, minimal `Cli` (export+validate). Defer: `MSBuild` (thin wrapper), `Catalog.Server`, `Aspire.Hosting.WorkflowApiCatalog`, Roslyn analyzers, source generator, `diff`/`bundle`/`publish` CLI. Runtime reflection scanner only (no Roslyn generator in v1). Verify.Xunit snapshot harness with 6 fixtures.
- **Alex (Temporal binding + UI) — Drop the standalone catalog from v1.** Ship Temporal static binding metadata + per-service Reference UI only. Preserve runtime overlay *contract surface* (`WorkflowApi.Runtime.Abstractions`) with a no-op `WorkflowApi.Runtime.Temporal` skeleton so post-v1 plugins drop in. Four code-enforced invariants: UI core has no Runtime/Temporal refs; Abstractions has no Temporal ref; `Runtime.Temporal` has zero write API surface; browser bundles ship no Temporal SDK. WCAG 2.1 AA + keyboard nav + text-fallback graph are non-negotiable.
- **Miller (conformance) — ~30 diagnostic codes proposed.** Code prefixes: `WFAPI_PARSE_*`, `WFAPI_SCHEMA_*`, `WFAPI_CORE_*` (15 rules), `WFAPI_TEMPORAL_*` (7 rules), `WFAPI_STYLE_*`. Reserved (not emitted in v1): `WFAPI_CATALOG_*`, `WFAPI_RUNTIME_*`. Frozen diagnostic JSON envelope. CLI `validate` with `--profile`, `--strict`, `--format text|json|sarif`, exit codes 0/1/2/3. Diff CLI with 7 spec-13 categories + ~20 `WFAPI_DIFF_*` codes. Minimum fixtures: 12 positive + 15 negative + ≥7 diff pairs. Ten deterministic-output rules mandatory for every JSON-producing subsystem.
- **Bobbie (security) — v1 is safe by construction IF three load-bearing rules hold:** (1) no runtime overlay code paths exist in v1 binaries (read-only enforced by absence, not `if`); (2) no Temporal client wired into any UI/catalog process; (3) blocking CI no-secrets scan (gitleaks recommended) over specs/examples/generated/tests/`.agents/`. WorkflowAPI v1 itself requires **zero runtime credentials** — defend this. Acceptance: write-op refusal tests, overlay-disabled tests, PII classification diagnostics (`WFAPI-SEC-001`/`WFAPI-SEC-002`), CSP + escape tests on reference UI, SBOM + non-root chiseled containers, parser limits (5MB / depth 64 / `$ref` depth 32), SECURITY.md, agent skill review. Nine must-fix §14 spec gaps catalogued.
- **Holden (first-pass plan) — 5 milestones, ~11 weeks.** M1 spec freeze + skeleton; M2 .NET core; M3 Temporal binding; M4 Reference UI; M5 conformance/diff/RC. Hard order: spec freeze → schema+fixtures+diagnostic registry → Abstractions → AspNetCore → Temporal → UI. Holden rulings: OMIT `WorkflowApi.Catalog` from v1 (reserve name only); CLI = `validate`+`export`+`diff` only (`publish` slips to v1.1). Spec 10 §14 items 11–14 (catalog, Aspire, overlays, publish tooling) explicitly out of v1 — must not be allowed to creep.

**Convergent v1 scope (all six independent assessments agree):**

1. Six .NET packages: `Abstractions`, `AspNetCore`, `Temporal`, `AspNetCore.Temporal`, `Reference`, minimal `Cli`.
2. Temporal binding is static metadata only — no client writes anywhere in v1 binaries.
3. Reference UI is per-service, static, requires zero runtime credentials.
4. Catalog server is out of v1 (Alex prefers drop entirely; Holden agrees; Bobbie agrees on attack-surface grounds).
5. Conformance suite, validator, and diff land alongside packages — not after.

**Spec-freeze blockers Naomi owns before Amos starts coding in earnest:**

1. Pin one `workflowApi` format version across spec/schema/examples.
2. Make `run` cardinality unambiguous on concrete workflows.
3. Resolve `schemaRef` canonical form (`input.schema.$ref` vs bare `$ref`).
4. Resolve bridge shape (flat vs nested `bridges→services→operations`).
5. Resolve workflow key naming (kebab-case vs PascalCase in examples).
6. State `activities` top-level vs inline precedence + `ref.activity` resolution rule.
7. Scope-fence doc 09 §20–§28 as post-v1.

**Cross-agent contracts (frozen at v1, breaking changes thereafter):**

- Diagnostic code identifiers + JSON envelope (Miller).
- CLI `--strict` / `--fail-on` semantics + exit codes 0/1/2/3 (Miller).
- Diff category names (Miller, per spec 13).
- Runtime overlay contract surface in `WorkflowApi.Runtime.Abstractions` (Alex).
- Secret-name denylist + PII classification enum `public|internal|business-identifier|pii|sensitive|restricted` with default `internal` (Bobbie, owned by Naomi normatively).
- `WFAPI-SEC-001` (error) on secret-shaped fields; `WFAPI-SEC-002` (warning) on PII-named search attributes without classification.

**Holden's open lead-decisions still in flight (final-plan pass will resolve):**

- Final M3/M4 parallelism shape.
- `publish` CLI command inclusion (current ruling: defer to v1.1).
- Bridge grammar consolidation point.
- Catalog disposition (current convergent recommendation: drop from v1).

**Status:** Holden's final synthesis (`holden-mvp-final-plan.md`) was not yet present at archive time. Coordinator will trigger a second Scribe pass to merge it.

---

## Inbox Archive — 2026-06-17 (full agent submissions, verbatim)


<!-- merged from alex-mvp-temporal-ui-readiness.md on 2026-06-17 -->
# Alex — MVP Readiness: Temporal Binding, Reference UI, Catalog

**Author:** Alex (Temporal & UI Dev)
**Date:** 2026-06-17
**Status:** Proposal for team consensus
**References:** docs/specs/03, 04, 08, 11, 16, 17, 18; docs/assets/screenshots/*

---

## 0. Governing constraint (spec 18)

WorkflowAPI v1 is **tightened** to:

- Compact durable-execution workflow API document model.
- .NET generation from attributes / fluent definitions.
- Temporal **static binding metadata**.
- **Static** workflow display (reference UI).

Explicitly **out of v1**: runtime overlays, catalogue collation, source polling,
workflow control-plane actions, BPMN-style modelling, runtime observability,
observed topology, history parsing.

Specs 03 §11, 04 §6, 08, 11 (phases 3–5), 16, and 17 carry "v1 tightened scope
note" headers and are retained as **post-v1 design** only. My MVP scope mirrors
this exactly; nothing in the contracts/packages should be deleted, but the
implementation slice for v1 is far smaller than the documents alone suggest.

---

## 1. Temporal binding MVP scope (`WorkflowAPI.Temporal`)

### In v1 — static binding metadata only

- Attribute → spec inference from Temporal SDK attributes (spec 03 §3):
  - `[Workflow]` → workflow type, dynamic flag.
  - `[WorkflowRun]` → run method, input/output types.
  - `[WorkflowSignal]`, `[WorkflowQuery]`, `[WorkflowUpdate]` → name + payload types.
  - `[Activity]` → activity type, input/output, dynamic flag.
- Host-level binding object (spec 03 §5.1): `targetHost`, `namespace`, `taskQueues`,
  `webUrl`, `environment`. `targetHost` optional in exported docs.
- Workflow / activity / signal / query / update binding shapes (§5.2–5.4).
- **Static** Nexus binding (§5.5, §10.1, §10.2): implemented and called
  Nexus services/operations as declared metadata. Endpoint routing treated as
  deployment metadata (§10.3) — not inferred from a running cluster.
- Topology declaration (§8.1, §8.2): `[WorkflowApiEdge]` attribute and the
  `WorkflowApiDefinition<T>` fluent builder. **Declared only.**
- Search attribute declaration (§9) with PII warning analyzer.
- Validation rules from §13 wired into the .NET analyzer/diagnostic surface.
- `WithTemporal(...)` extension on the WorkflowAPI builder (§6.2/§6.3) so a
  service can opt into Temporal binding inference without taking a runtime
  dependency at spec-generation time.

### Deferred (post-v1, design only)

- Runtime overlay providers, Visibility / Count / GetWorkflowExecutionHistory
  queries (spec 03 §11) — keep the `WorkflowApi.Runtime.Abstractions` contract
  surface and a `WorkflowApi.Runtime.Temporal` package skeleton with a no-op
  provider only.
- Observed-topology / runtime-derived graph (§8.3).
- Drift detection (spec 11 phase 5).
- Endpoint resolution against a live Temporal cluster.
- **Hard rule: no Temporal client writes ever** (start, signal, update,
  cancel, terminate, schema register). Enforced in `WorkflowAPI.Temporal`
  by not taking a dependency on `Temporalio.Client` write APIs in v1 and
  by Bobbie's security review gate.

---

## 2. Reference UI MVP (`WorkflowApi.Ui.Core`, `WorkflowApi.Reference`)

### In v1 — static rendering, zero Temporal creds

Per spec 11 §"Phase 1" and spec 04 §12 MVP recommendation:

- Single endpoint: `app.MapWorkflowApiReference()` reading one
  `workflow-api.json` from `app.MapWorkflowApi()`.
- Document overview (title, version, host, bindings summary, counts).
- Workflow list and workflow detail pages: run, signals, queries, updates,
  steps, edges, schemas, examples, Temporal binding metadata, dependencies,
  bridge references.
- Schema viewer (JSON Schema + examples).
- Declared topology graph via `WorkflowApiGraphAdapter` →
  `@eventcatalog/visualiser` (per spec 04 §5, spec 11 "EventCatalog
  visualiser usage"). React Flow fallback path documented but not required
  in v1.
- Raw document download.
- Diagnostics view (validation errors/warnings only — no runtime errors yet).
- UI containers: Shadcn UI primitives + Tailwind (per reference-ui SKILL).
- **No** runtime overlay slots wired to live data, **no** time-range picker,
  **no** `/api/runtime/*` calls. The overlay slot vocabulary from spec 11
  ("nodeBadge", "edgeBadge", "sidePanelSection", …) should be reserved in the
  component API but unused in v1 so a post-v1 plugin can hydrate them.

### Deferred

- All three modes from spec 11 §"UI modes" beyond mode 1 ("Static reference").
- Live overlay hydration, deep links to Temporal Web, drift views, observed
  graph views, time-range selector.

### UI mockup alignment (docs/assets/screenshots)

| Mockup | v1 status |
|---|---|
| `workflowapi-reference-specification-view.png` | **In v1.** This is the target for `MapWorkflowApiReference()`. |
| `workflowapi-reference-runtime-overlays.png` | Post-v1. Reference for plugin slot layout only. |
| `workflowapi-catalog-merged-runtime.png` | Post-v1. Mocks merged graph + runtime — both out of v1. |
| `workflowapi-instance-drilldown.png` | Post-v1. Drill-down is runtime overlay + Temporal Web deep linking. |

Naomi/Rebecca should confirm we are happy to ship v1 with only the first
mockup realised.

---

## 3. Catalog server MVP (`WorkflowApi.Catalog.*`, `Aspire.Hosting.WorkflowApiCatalog`)

Catalog collation and source polling are **out of v1** (spec 18). I recommend
v1 ships **no standalone catalog server**. The per-service reference UI in
§2 covers the v1 use case ("WorkflowAPI document + Scalar-like reference").

If the team disagrees and wants a minimum catalog footprint in v1, the
smallest acceptable slice is:

- ASP.NET Core host project published as a Docker image (spec 08 §11).
- Configuration model from spec 08 §5.1 (HTTP, file, directory sources) —
  **no polling**, **single fetch at startup only**, no conditional requests,
  no ETag handling.
- Memory cache only; no persistent store (spec 08 §4.2).
- Partial-failure semantics from collation rules (spec 17): per-source status
  (`healthy`, `invalid`, `unreachable`, `stale`, `disabled`), partial graph
  rendering, never blank the UI if at least one source is good.
- Canonical identities from spec 17 §"Canonical identities" and merge rules
  for hosts / workflows / steps / bridges / schemas (§"Merge rules").
- Conflict diagnostics (`WFAPI_CATALOG_DUPLICATE_HOST_ID`, etc.).
- Aspire extension surface limited to `AddWorkflowApiCatalog`,
  `WithCatalogSource(project)`, `WithCatalogSourceFile`,
  `WithCatalogSourceDirectory` (spec 04 §7.3).
- **Explicitly excluded even in this minimum slice:** `WithTemporal`,
  `WithRuntimeOverlay`, polling, `/api/runtime/*` endpoints, Temporal
  connections, drift, Temporal Web deep links.

My preference: drop the catalog from v1 entirely and ship it as the first
post-v1 deliverable. Reasoning in §6 risks.

---

## 4. Runtime overlay plugin architecture — v1 contract surface

Even though no overlay implementation ships in v1, the v1 **package layout
and contract surface** should already exist so post-v1 plugins drop in.

### v1 deliverable

- `WorkflowApi.Runtime.Abstractions` package containing:
  - `IWorkflowRuntimeOverlayProvider` (spec 11 §"Backend interface concept").
  - DTOs: `WorkflowRuntimeCapabilities`, `WorkflowMetricsRequest/Result`,
    `StepMetricsRequest/Result`, `BridgeMetricsRequest/Result`,
    `RuntimeDeepLinkRequest`, `RuntimeDeepLink`, time-range model.
  - Standard error code `WFAPI_RUNTIME_*` shape (spec 16 §"Error model").
- `WorkflowApi.Runtime.Temporal` package skeleton containing **only** the
  no-op / disabled provider and capabilities reporting `enabled: false`.
- Plugin loading via static DI (spec 11 §"Catalog server plugin loading"):
  `AddRuntimeOverlay<TProvider>()` registration only. **No dynamic plugin
  discovery / assembly scanning / load context isolation in v1** (security
  scope reduction).
- Frontend: reserve overlay slot component API (`nodeBadge`, `edgeBadge`,
  `sidePanelSection`, `runtimeLinks`) returning nulls when no provider is
  registered. Documented so a post-v1 Temporal overlay package can ship
  without changing UI core.

### v1 contract invariants (must be true in code, not just docs)

1. UI core has zero compile-time references to `WorkflowApi.Runtime.*` and
   zero references to `Temporalio.*`.
2. `WorkflowApi.Abstractions` has zero references to `Temporalio.*` or
   `WorkflowApi.Temporal*`. (Standing rule from charter; restating because
   this is the easiest thing to break when wiring binding inference.)
3. `WorkflowApi.Runtime.Temporal` has zero **write** API surface against
   Temporal — no `StartWorkflowAsync`, `SignalAsync`, `UpdateAsync`,
   `CancelAsync`, `TerminateAsync`, schema register, namespace mutate.
4. Browser bundles ship no Temporal client SDK.

These four invariants should be enforced by a Miller-owned conformance test
(banned-symbol / ArchUnit-style assertions) so they cannot regress.

---

## 5. UI accessibility, keyboard, and graph fallback (v1 requirements)

Non-negotiable per my charter ("accessibility is not optional"):

- WCAG 2.1 AA for all reference UI surfaces in v1.
- Full keyboard navigation: tab order through workflow list, workflow detail
  sections, schema viewer, and graph nodes/edges. No mouse-only interactions.
- Visible focus states on every interactive element (Shadcn defaults retained,
  not stripped by custom theming).
- Graph view must have a **readable text fallback**: a deterministic
  workflow → steps → edges outline rendered as semantic HTML
  (`<section>`/`<ol>`/`<dl>`) that is keyboard-traversable and screen-reader
  navigable, available without JS-driven layout. This is the canonical
  fallback for: no-JS environments, screen readers, print, and any failure of
  the visualiser dependency.
- Graph nodes must expose `role`, accessible name, and a textual description
  of incoming/outgoing edges via ARIA.
- Color is never the sole signal: status (healthy / warning / invalid),
  binding badges, and validation severity must each have a text/icon
  affordance in addition to color.
- Prefers-reduced-motion respected for any graph layout animations.
- Schema viewer must be readable at 200% zoom and reflow at 320 CSS px.
- Diagnostics list is a proper landmark with a heading and a count
  announced via `aria-live="polite"`.

Acceptance gate: Miller adds an axe-core / pa11y check to the reference UI
test pass, plus a snapshot test for the text fallback graph.

---

## 6. Risks

### R1 — Runtime read-only enforcement (HIGH)

The largest temptation is to "just add a `StartWorkflowAsync` for the
re-run button" or wire a control-plane endpoint into the catalog. Mitigation:

- §4 invariant #3 enforced by conformance test (Miller).
- `WorkflowApi.Runtime.Abstractions` exposes **no** mutating verbs in its
  contract — there is nowhere legitimate to put a write.
- Bobbie's security review must sign off any change to the Temporal package
  that adds a new public method.
- Documented in spec 18 already; restate in package README.

### R2 — Secret leakage from runtime / binding (HIGH)

Even though runtime overlays are post-v1, Temporal binding metadata can
inadvertently leak secrets if `targetHost`, auth headers, or env-resolved
config bleed into the generated `workflow-api.json`. Mitigation:

- `targetHost` is optional in exported docs (spec 03 §5.1) and **redacted by
  default** in the exporter for non-`Development` environments.
- No `Tls`/`Auth`/`ApiKey`/`ClientCertPath` fields are ever serialised into
  WorkflowAPI documents; binding shape v1 has no field for them.
- Document generator strips any `Configuration` interpolation that resolves
  to a value tagged `Secret` (works with .NET's secret-manager / `KeyVault`
  bindings — Amos owns the generator hook).
- Search attribute PII analyzer (spec 03 §9) emits a warning when an
  attribute name matches common PII heuristics.
- Examples must not contain real payloads (charter rule).
- Snapshot tests on generated documents from the sample app assert that no
  banned tokens (`Bearer `, `BEGIN PRIVATE KEY`, `tmprl.cloud`, `ApiKey=`)
  appear in output.

### R3 — UI core picking up a Temporal dependency by accident (MEDIUM)

Easy to do when wiring overlay slots. Mitigation: §4 invariant #1
enforced by package-reference lint and the conformance banned-symbol test.

### R4 — Visualiser dependency coupling (MEDIUM)

If we leak `@eventcatalog/visualiser` node payload shape into the
WorkflowAPI document model, we cannot swap the renderer later. Mitigation:
strict `WorkflowApiGraphAdapter` boundary (spec 04 §5.2, spec 11 §"EventCatalog
visualiser usage"). UI core exports only `WorkflowApiGraphModel`; the adapter
package is separate.

### R5 — Catalog scope creep in v1 (MEDIUM)

If we ship even a minimum catalog (§3), there is strong pressure to add
polling, Temporal overlay, and Aspire `.WithTemporal()`. Mitigation: prefer
to defer the catalog entirely to post-v1. If the team wants the minimum
slice, lock it behind the explicit list in §3 and gate any addition on a
team decision in `.squad/decisions.md`.

### R6 — Accessibility regressions when adding the visualiser (MEDIUM)

Canvas/SVG-only graph renderers routinely fail screen-reader testing.
Mitigation: the text fallback in §5 is the canonical accessible
representation; the visualiser is an enhancement.

### R7 — Nexus endpoint routing ambiguity (LOW for v1)

Endpoint → namespace/task-queue resolution is deployment-specific
(spec 03 §10.3). v1 treats endpoint metadata as declared only. Catalog
resolution post-v1.

---

## 7. Sequencing — dependencies on Amos's core .NET packages

My v1 work is layered on Amos's deliverables. Critical path:

```
Amos: WorkflowApi.Abstractions
    └─ document model, attributes, validation primitives, JSON Schema
       └─ blocks ALL my work
Amos: WorkflowApi.AspNetCore
    └─ AddWorkflowApi(), MapWorkflowApi(), document generator,
       attribute scanning, fluent definition support
       └─ blocks WorkflowApi.Temporal (needs the builder extension point)
       └─ blocks WorkflowApi.Reference (needs the document endpoint)
```

### Parallelisable

Once Amos exposes:
- the document model types,
- the spec-generation pipeline hook (`IWorkflowApiTransformer` or
  equivalent), and
- the builder extension point (`IWorkflowApiBuilder.WithBindings(...)`),

I can work in parallel on:

- `WorkflowApi.Temporal` static binding inference (depends on document model
  + builder hook; does **not** need MapWorkflowApi to be finished).
- `WorkflowApi.Runtime.Abstractions` contract surface (depends only on
  basic types in Abstractions).
- `WorkflowApi.Ui.Core` frontend (depends only on the JSON document shape /
  schema; can use fixture documents until the .NET generator is wired).
- `WorkflowApi.Reference` host package (small wrapper; can stub the document
  endpoint until `MapWorkflowApi` lands).

### My order (assuming the contract handshake with Amos is settled)

1. `WorkflowApi.Runtime.Abstractions` contract + DTOs (small, unblocks UI
   slot API and gives Miller a stable surface to test invariants against).
2. `WorkflowApi.Temporal` static binding: attribute inference, host/workflow/
   activity/signal/query/update binding shapes, fluent topology, search
   attributes, validation rules.
3. `WorkflowApi.Ui.Core` against fixture documents: document overview,
   workflow list, workflow detail, schema viewer, diagnostics, text-fallback
   graph.
4. Visualiser adapter + graph view on top of (3).
5. `WorkflowApi.Reference` integration: `MapWorkflowApiReference()` serving
   the UI from a real `MapWorkflowApi()` document.
6. `WorkflowApi.Runtime.Temporal` no-op skeleton + capabilities endpoint
   reporting `enabled: false`.
7. **(Defer if possible)** Minimum catalog slice per §3.

### Handshake items I need from Amos

- Final names + shapes of: document root type, host metadata, workflow,
  step, edge, bridge, signal/query/update/activity, schema reference.
- Extension point for binding inference (`WithBindings` /
  `IWorkflowApiBindingProvider`) so `WithTemporal(...)` is a clean plug-in.
- Document-generation snapshot test infrastructure I can reuse for the
  Temporal binding snapshots.
- Confirmation that `WorkflowApi.Abstractions` will not take a Temporal
  dependency under any circumstance (charter rule, restated).

### Handshake items I need from Naomi

- Confirmation that Nexus binding shape v1 is the static `implements` /
  `calls` model in spec 03 §5.5/§10 and nothing more.
- Confirmation that declared topology (`[WorkflowApiEdge]` + fluent
  `builder.Edge(...)`) is the only v1 topology source.

### Handshake items I need from Miller

- Conformance tests enforcing the four invariants in §4.
- Axe/pa11y check for reference UI; snapshot for text-fallback graph.
- Banned-token snapshot test on generated documents (§R2).

### Handshake items I need from Bobbie

- Security review on the Temporal binding package public surface before
  any v1 tag.
- Sign-off on the redaction rules for `targetHost` / config-resolved values.

---

## 8. Summary recommendation

- **Ship v1 with:** Temporal static binding + per-service Reference UI +
  Runtime overlay contract surface (no implementation).
- **Defer to post-v1:** Standalone catalog server, runtime overlays
  (including Temporal implementation), source polling, drift, observed
  topology, deep links, time-range UI. Keep design docs (specs 16, 17, 08,
  parts of 11, 03 §11) as the post-v1 plan; do not delete.
- **Enforce in code** (not just docs) the four invariants in §4 and the
  accessibility requirements in §5 — these are the contract gates that
  protect us from the scope-creep risks in §6.


<!-- merged from amos-mvp-dotnet-readiness.md on 2026-06-17 -->
# Amos — .NET Implementation Readiness for WorkflowAPI v1 MVP

**Author:** Amos (.NET Dev)
**Date:** 2026-06-17
**Status:** Proposal — for team review
**Requested by:** Rebecca Powell

## TL;DR

Specs are detailed and largely implementation-ready for an MVP. **Zero .NET code exists in the repo today** (no `.csproj`, no `.sln`). MVP needs 5 core packages + 1 test project to deliver Path A (fluent) and Path B (Temporal) authoring with a static export endpoint. CLI/MSBuild can be deferred to a thin export wrapper if needed; the rest is post-MVP.

Two gating dependencies on Naomi: (1) frozen v1 core document model (§01/§09), (2) frozen Temporal binding shape (§03). Without these locked, snapshot tests churn and the generator pipeline can't stabilise.

---

## 1. Package architecture — MVP set

| # | Package | MVP? | Purpose |
|---|---|---|---|
| 1 | `WorkflowApi.Abstractions` | **MVP** | POCO document model + WorkflowAPI attributes. No deps. |
| 2 | `WorkflowApi.AspNetCore` | **MVP** | `AddWorkflowApi`, `MapWorkflowApi`, scanner provider model, fluent `IWorkflowApiBuilder`, transformers, JSON writer (deterministic). Scrutor allowed here. |
| 3 | `WorkflowApi.Temporal` | **MVP** | Reads `Temporalio` attributes (`[Workflow]`, `[WorkflowRun]`, signals/queries/updates, `[Activity]`), produces descriptors + Temporal binding object. Worker-safe (no ASP.NET). |
| 4 | `WorkflowApi.AspNetCore.Temporal` | **MVP** | `WithTemporal(...)` extension; wires Temporal provider into the AspNetCore pipeline. |
| 5 | `WorkflowApi.Reference` | **MVP (thin)** | `MapWorkflowApiReference()` serving Alex's static UI bundle as embedded resources. No generation logic. |
| 6 | `WorkflowApi.Cli` | **Defer / minimal** | MVP-minimum: `export` (load assembly, run pipeline, write JSON) + `validate` (JSON Schema). `diff`/`bundle`/`publish` post-MVP. |
| 7 | `WorkflowApi.MSBuild` | **Defer** | Wrapper that invokes the CLI in a post-build target. Single `.targets` file, no source generator yet. |
| 8 | `WorkflowApi.Catalog.Server` | **Out of MVP** | Per §18 non-goals (catalogue collation). |
| 9 | `Aspire.Hosting.WorkflowApiCatalog` | **Out of MVP** | Depends on #8. |

**Dependency direction (must hold):**
```
Abstractions  ← AspNetCore  ← AspNetCore.Temporal → Temporal → Abstractions
                       ↖ Reference                ↗
                       Cli → AspNetCore (+ Temporal optionally)
```
**Hard rules:** Abstractions has no Scrutor, no ASP.NET, no Temporal. Temporal has no ASP.NET.

**Test projects (one per shipping package):**
`WorkflowApi.Abstractions.Tests`, `WorkflowApi.AspNetCore.Tests`, `WorkflowApi.Temporal.Tests`, `WorkflowApi.AspNetCore.Temporal.Tests`, plus a single `WorkflowApi.Snapshots.Tests` for golden documents.

---

## 2. Attribute + fluent API surface

### Specced and ready to implement (§02 §4)

Attributes: `[WorkflowApi]`, `[WorkflowApiRun]`, `[WorkflowApiSignal]`, `[WorkflowApiQuery]`, `[WorkflowApiUpdate]`, `[WorkflowApiActivity]`, `[WorkflowApiStep]`, `[WorkflowApiEdge]`, `[WorkflowApiSearchDimension]`. Properties are fully defined.

Fluent (§02 §3/§5): `AddWorkflowApi(name)`, `.ScanFromAssemblyOf<T>()`, `.WithTemporal(...)`, `.AddWorkflow(...)`, `.AddDefinition<T>()`, `.Configure(doc => ...)`. `WorkflowApiDefinition<TWorkflow>` base class. `IWorkflowApiBuilder` with `.Workflow().Step().Activity().Edge().Sla().Title()` chains.

### Needs detail from Naomi before coding

- **`WorkflowApiDocument` C# shape** — exact property names, nullability, collection types (list vs dictionary), enum casing. §09 is normative but I need a frozen C# type map.
- **`StepKind` enum values** — spec lists `activity`, `bridge`, `childWorkflow`, `signal`, `query`, `update`. Need confirmation this is closed for v1.
- **`WorkflowApiCriticality` enum values** — referenced in §02 §4.5 but not enumerated.
- **`Lifecycle` / `Visibility` strings vs enums** — currently typed as `string?`; spec needs to say if these are open or closed sets.
- **Search dimension type model** — generic name + `ContainsPersonalData`, but how runtime-specific index types (Temporal Keyword/Text/Int/DateTime) attach. §02 §4.8 says "mapped through the Temporal binding"; needs a concrete shape.
- **Schema reference model** — `IWorkflowApiSchemaGenerator` is abstracted (§02 §10), but for MVP we must pick a default. Recommend: thin wrapper over `System.Text.Json.Schema.JsonSchemaExporter` (available in .NET 10).

### Needs detail (mine, not Naomi's)

- Discovery options merge precedence when multiple `AddWorkflowApi(name)` calls share an assembly scan.
- XML doc file discovery on disk (alongside-dll convention).
- Deterministic JSON writer settings (property ordering, indentation, line endings).

---

## 3. Generator strategy

**MVP: runtime reflection scanner only.** §02 §17 explicitly recommends this.

- `IWorkflowApiDescriptorProvider` is the abstraction. Two providers ship for MVP:
  1. `CoreDescriptorProvider` — reads WorkflowAPI attributes + `WorkflowApiDefinition<T>` subclasses via Scrutor.
  2. `TemporalDescriptorProvider` — reads `Temporalio` attributes; registered only when `.WithTemporal(...)` is called.
- Aggregation pipeline order is specced (§02 §7) — implement straight.
- Output goes through transformer chain (§02 §9) — ship the interfaces and an empty default chain; sample transformer in tests.

**Not MVP:** Roslyn source generator, Roslyn analyzers (WFA0xx diagnostics in §02 §14), MSBuild incremental generation. These are post-MVP per §02 §17 recommendation. WFA002/WFA003/WFA007/WFA008 should ship as **runtime** diagnostics surfaced by the pipeline; the analyzer versions come later.

**AOT/trimming:** out of scope for MVP. Add `[RequiresUnreferencedCode]` annotations on scanner entry points so we don't silently break trimmed apps.

---

## 4. Snapshot / golden test approach

**Tooling:** Verify (`Verify.Xunit`) — already idiomatic in .NET, no new infra needed beyond the NuGet ref. Snapshots live next to tests; CI fails on diff.

**Fixtures:**
- `tests/dotnet/WorkflowApi.Snapshots.Tests/Fixtures/` — small workflow programs covering each authoring path:
  - `FluentMinimal` — Path A, one workflow, one step.
  - `FluentFullTopology` — Path A, multi-step + edges + signals/queries/updates.
  - `TemporalAttributesOnly` — Path B with no `[WorkflowApi*]` (API surface only).
  - `TemporalWithDeclaredTopology` — Path B + `[WorkflowApiStep]` / `[WorkflowApiEdge]`.
  - `TemporalWithFluentDefinition` — Path B + `WorkflowApiDefinition<T>` override.
  - `MultiDocument` — `public` vs `internal` view of the same assembly.
- Each fixture compiles, runs the pipeline in-process, serialises, and compares against `*.verified.json`.

**Determinism guards** (run as a separate test pass):
- No timestamps unless explicit.
- No absolute paths.
- Sorted keys: workflows by name, steps by name, edges by `(from, to)`, schemas by ref.
- Same input → byte-identical output across two consecutive runs in the same test.

**Cross-cut with Miller:** snapshot fixtures double as positive conformance inputs. Coordinate fixture set so we don't duplicate.

---

## 5. Build / test / lint commands (use existing only)

Per spec §10 (.NET 10 LTS) and project rules — no new tooling:

```bash
dotnet restore
dotnet build -c Release --nologo
dotnet test -c Release --nologo --no-build
dotnet format --verify-no-changes        # lint/style — built into SDK
```

CI minimum: build + test + format check + run `workflowapi validate` over `tests/schema/valid-documents/` once the CLI ships.

**Project-wide config (one-time setup, MVP):**
- `Directory.Build.props` at `src/dotnet/` enforcing `TargetFramework=net10.0`, `Nullable=enable`, `TreatWarningsAsErrors=true`, `LangVersion=latest`.
- `Directory.Packages.props` for central package management (`Scrutor`, `Temporalio`, `Verify.Xunit`, `xunit`, `Microsoft.AspNetCore.App` framework ref).
- Root `WorkflowApi.sln` covering MVP projects.
- `.editorconfig` already implied by `dotnet format`; ship a minimal one.

---

## 6. Dependencies on Naomi (blocking, ordered)

1. **§01 + §09 frozen** — C# type names + JSON property names for the document model. Without this, Abstractions can't ship. **Blocker for everything.**
2. **JSON Schema in `schemas/workflowapi.schema.json` matches §09** — needed for `workflowapi validate` and for snapshot tests to use as guard.
3. **§03 Temporal binding shape frozen** — `taskQueue`, `namespace`, `workflowType`, `signalName`, `queryName`, `updateName`, search-dimension-type mapping, Nexus bridge attributes. Blocks `WorkflowApi.Temporal`.
4. **Closed enums identified** — `StepKind`, `Criticality`, `Lifecycle`, `Visibility`. Blocks attribute compile.
5. **Examples in §06 confirmed canonical** — they become snapshot inputs.

**Non-blocking but wanted:** spec text for diagnostics WFA002/003/007/008 (so my runtime messages match the docs).

---

## 7. Effort buckets

Sized in t-shirts assuming 1 dev, specs frozen, no rework:

| Package / workstream | Size | Notes |
|---|---|---|
| Repo scaffolding (`src/dotnet/`, `tests/dotnet/`, sln, Directory.*.props, .editorconfig) | **S** | Mechanical, half-day. |
| `WorkflowApi.Abstractions` (POCOs + attributes + enums) | **S** | Mostly typing once §09 is frozen. |
| `WorkflowApi.AspNetCore` (DI, scanner abstraction, fluent builder, transformer pipeline, JSON writer, MapWorkflowApi, multi-doc) | **L** | Biggest single package. The pipeline + builder is the heart of the system. |
| `WorkflowApi.Temporal` (attribute reader + binding emitter) | **M** | Bounded by Temporal SDK surface; mechanical but needs Nexus handling. |
| `WorkflowApi.AspNetCore.Temporal` (`WithTemporal` + provider wiring) | **S** | Thin glue. |
| `WorkflowApi.Reference` (host static UI assets behind `MapWorkflowApiReference`) | **S** | Trivial on .NET side; depends on Alex shipping a bundle. |
| `WorkflowApi.Cli` (minimal: `export` + `validate`) | **M** | Assembly load + isolation needs care; use `System.CommandLine`. |
| `WorkflowApi.MSBuild` (single `.targets` invoking CLI) | **S** | Defer or ship as thin wrapper. |
| Snapshot test harness + 6 fixtures | **M** | Per §4 above. |
| Schema validator wiring (`workflowapi validate`) | **S** | Reuse Naomi's schema via `JsonSchema.Net`. |
| **MVP total** | **~3–4 weeks** | Sequential critical path: Abstractions → AspNetCore → Temporal → snapshots. |

**Explicitly out of MVP:** Catalog server, Aspire integration, Roslyn analyzers, source generator, `diff`/`bundle`/`publish` CLI commands, runtime overlay packages.

---

## Open questions / asks

- **Holden:** confirm `WorkflowApi.AspNetCore.Temporal` as a separate package vs folded into `WorkflowApi.Temporal` with optional ASP.NET ref. Spec says separate; I agree but want a sign-off because it adds a package to maintain.
- **Naomi:** see §6 above.
- **Alex:** what's the delivery format for the reference UI bundle (single HTML + assets folder? embedded `.zip`? loose files in `wwwroot`?). Affects `WorkflowApi.Reference` build.
- **Miller:** are your conformance fixtures the same set as my snapshot fixtures, or do you need orthogonal coverage? Want to avoid duplication.
- **Bobbie:** does shipping `[WorkflowApiSearchDimension(ContainsPersonalData=true)]` without redaction at MVP need a security gate, or is a runtime warning (WFA006) enough for now?

## Recommendation

Proceed with MVP scope as above the moment §01/§09/§03 are frozen. Start scaffolding (S task) in parallel with Naomi finishing the model — it's safe work that doesn't bind us to type shapes.


<!-- merged from bobbie-mvp-security-readiness.md on 2026-06-17 -->
# Bobbie — v1 MVP Security Readiness Assessment

**Author:** Bobbie (Security)
**Date:** 2026-06-17
**Requested by:** Rebecca Powell
**Status:** Proposed — for team review
**Scope:** WorkflowAPI v1 MVP (compact DSL, .NET generation, Temporal static binding, static reference UI)

---

## TL;DR

v1 is **small enough to be safe by construction**, IF we hold the line on three things:

1. **No runtime overlay code paths exist in v1 binaries.** Read-only is enforced by *absence*, not by `if` checks.
2. **No Temporal client is wired into any UI or catalog process in v1.** The static UI ships zero Temporal SDK dependencies.
3. **A CI no-secrets scan is mandatory and blocking** on specs, examples, generated outputs, tests, and `.agents/` skill files.

The spec is broadly in good shape but has gaps (listed in §7) that I want closed before tagging `v1.0`.

---

## 1. v1 Write-Op Enforcement — How We Guarantee Read-Only

Spec §14 forbids start/signal/update/terminate/cancel/reset/schedule/Nexus-modify in v1. The principle is right; the enforcement model needs to be **structural, not behavioural**.

### Rules (proposed)

1. **No Temporal client in v1 reference UI / catalog binaries.** The `WorkflowApi.Ui.Core`, `WorkflowApi.AspNetCore`, and CLI projects must not have a `PackageReference` to `Temporalio` or `Temporalio.Client`. Verified by a build-time assertion test in CI.
2. **Runtime overlay assemblies are not produced in v1.** `WorkflowApi.Ui.Runtime`, `WorkflowApi.Ui.TemporalOverlay`, and any `RuntimeOverlay*` projects are either:
   - excluded from the v1 release manifest, OR
   - present only as compile-time stubs that throw `NotSupportedException("Runtime overlays are out of scope for WorkflowAPI v1")` and are tagged `[Experimental]`.
3. **No HTTP endpoints under `/api/runtime/*`** are registered by any v1 host. Asserted by an integration test that boots the reference UI and confirms `404` for `GET /api/runtime/capabilities`.
4. **The Temporal binding package (`WorkflowApi.Temporal`)** is metadata-only in v1. It must not reference `Temporalio.Client` or `Temporalio.Worker.Client`. Only `Temporalio` types needed for *attribute* / *static metadata* extraction are allowed. A whitelist of allowed `Temporalio.*` namespaces is enforced by an architecture test (e.g. NetArchTest or a simple Roslyn-based fixture).
5. **CLI commands**: the v1 CLI exposes `validate`, `generate`, `diff`, `bundle`. It **must not** expose `start`, `signal`, `terminate`, `query`, `update`, or any verb that implies execution. A naming-policy test forbids those verb names.
6. **Per-service `MapWorkflowApiReference()`** must not accept a Temporal client/connection in its options surface. Reviewed by Bobbie before merge.

### Enforcement = code + test, not docs

The "Forbidden in v1" list in §14 stays, but every item must map to either:
- a missing dependency (preferred), or
- a failing test if the symbol appears.

---

## 2. Secret Handling

### Non-negotiable rules

- **Never in:** WorkflowAPI documents (JSON/YAML), examples in `docs/`, example projects, snapshot/golden outputs, conformance fixtures, test fixtures, generated `workflow-api.json`, `.agents/` skill files, agent prompts, decision inbox files, schema files.
- **Allowed locations:** ASP.NET Core configuration providers — `appsettings.*.json` user-secrets (dev only), environment variables, Azure Key Vault / AWS Secrets Manager / GCP Secret Manager, Kubernetes secrets mounted as files, workload identity / managed identity. Never committed.

### Configuration shape (already in §14, restated for v1)

Connection settings (host, namespace, TLS server name) live in config:

```json
{
  "WorkflowApi": {
    "Temporal": {
      "TargetHost": "temporal.company.internal:7233",
      "Namespace": "Commerce.OrderService"
    }
  }
}
```

Secrets (API key, mTLS key, OAuth client secret) are **referenced by name**, not by value, in config — actual material comes from the host secret provider.

> **v1 note:** Since v1 does not run a Temporal client from the UI, the only place that legitimately holds Temporal credentials in v1 is **user code** (the workflow worker process), which is out of WorkflowAPI's scope. WorkflowAPI v1 itself should require **zero runtime credentials** to function. This is a v1 superpower — defend it.

### Document-level redaction

- The generator must omit secret-looking fields even if reflected. A field is "secret-looking" if its name matches a denylist (regex): `(?i)(secret|password|passwd|pwd|api[_-]?key|token|bearer|authorization|credential|connection.?string|priv(ate)?[_-]?key|cert(ificate)?)`.
- If detected, generator emits diagnostic `WFAPI-SEC-001` (error) and refuses to write the document. Override requires explicit `[WorkflowApiAllowSensitiveName]` attribute and a comment justifying it. The override list is reviewed in CI by Bobbie.

---

## 3. PII / Search Attribute Redaction Requirements

Temporal Search Attributes are the highest-risk surface because they are routinely named after business identifiers.

### Required in v1

1. **Classification metadata on search attributes** (§14 already proposes this — make it normative):
   ```yaml
   searchAttributes:
     CustomerEmail:
       type: Text
       classification: pii
       visibility: restricted
   ```
2. **Classifications (closed enum for v1):** `public`, `internal`, `business-identifier`, `pii`, `sensitive`, `restricted`.
3. **Generator behaviour:**
   - Default classification when omitted is `internal` (NOT `public`).
   - Search attributes named to obvious PII patterns (`email`, `phone`, `ssn`, `dob`, `passport`, `nationalid`, `creditcard`, `iban`) without an explicit classification raise diagnostic `WFAPI-SEC-002` (warning) — recommend `pii` classification.
4. **Public-view emission:** any search attribute with classification ∈ {`pii`, `sensitive`, `restricted`} is **stripped** from the public document view. Tested via snapshot.
5. **DTO schema redaction:** properties annotated `[Pii]` or `[Sensitive]` (or the WorkflowAPI equivalent) are emitted as `"x-classification": "pii"` in the schema and stripped from the public view.
6. **No example payloads with realistic-looking PII** in shipped examples. Use obviously-fake values (`alice@example.com`, `+1-555-0100`, etc.). Bobbie reviews all example payloads.
7. **Workflow IDs and run IDs** must not appear in the static spec (only types/structure do), which v1 naturally satisfies — but call this out so post-v1 overlay work doesn't accidentally regress it.

---

## 4. Threat Model Summary for v1

Trust boundaries in v1 are narrow because most of WorkflowAPI is offline tooling.

### 4.1 Component inventory and trust posture

| Component | Trust zone | Runs creds? | Network reachable? | Primary threats |
|---|---|---|---|---|
| `WorkflowApi.Abstractions` (NuGet) | library, in-process | no | n/a | supply chain (typo-squat, dep confusion) |
| `WorkflowApi.Generator` (build-time / CLI) | dev workstation, CI | no | no (build-time) | malicious source assemblies, generator RCE via crafted attribute, supply chain |
| `WorkflowApi.AspNetCore` (static reference UI middleware) | in-process with host | no (v1) | yes (HTTP) | info disclosure, XSS in rendered spec, auth bypass to `/workflow-api` endpoints in prod |
| `WorkflowApi.Cli` | dev workstation, CI | no | no | path traversal in `--out`, malicious WorkflowAPI doc input → parser DoS, supply chain |
| Catalog server (if shipped in v1; spec §18 lists collation OOS — treat as **post-v1**) | server, multi-tenant | no | yes | AuthN/Z, IDOR across documents, info disclosure, ingestion of untrusted specs |
| Runtime overlay providers (Temporal) | **out of scope v1** | yes | yes | covered post-v1 |

### 4.2 Top threats and mitigations (v1)

| # | Threat | Likelihood | Impact | Mitigation in v1 |
|---|---|---|---|---|
| T1 | Secret leaks into committed WorkflowAPI doc / example / generated output | Med | High | No-secrets scan in CI (blocking); generator denylist `WFAPI-SEC-001`; Bobbie review on examples |
| T2 | PII leaks via Search Attributes / DTO schemas in public view | High | High | Classification metadata, default `internal`, public-view stripping, `WFAPI-SEC-002` warning |
| T3 | Reference UI exposed unauthenticated in Production | High | Med-High | Default = disabled outside Development; `RequireAuthorization` shown in all docs as the prod pattern; analyzer/template warning |
| T4 | XSS via workflow names/descriptions/error messages rendered in UI | Med | Med | All rendered text escaped; documentation/markdown rendered through allowlist sanitiser; CSP `default-src 'self'` on reference UI host |
| T5 | Supply-chain attack on `Temporalio` / dependencies | Low | High | SBOM generation, dependency scanning (Dependabot + CodeQL), pinned versions, signed container images |
| T6 | Malicious WorkflowAPI doc → parser DoS (deep nesting, schema bombs) | Low | Med | Hard limits: max document size, max nesting depth, max schema refs; reject with diagnostic |
| T7 | Build-time generator RCE via crafted attributes/types | Low | High | Generator runs in source-only analysis; no `Activator.CreateInstance` on user types; no reflection-invoke of constructors |
| T8 | Accidental write op against Temporal in v1 (scope creep) | Med | High | Structural enforcement (§1): no client dep, no `/api/runtime/*`, CLI verb policy test |
| T9 | Catalog (post-v1) collates untrusted specs from cross-team sources | n/a v1 | High | Defer; document risk now |
| T10 | Agent skill files contain prompt-injection payloads or pre-approved shell capabilities | Med | Med | All `.agents/skills/**/*.md` reviewed by Bobbie; no skill grants shell auto-approval; review checklist published |

### 4.3 Data flows (v1)

```
[Source code w/ workflow attrs] → [Generator / CLI] → [workflow-api.json on disk]
                                                          │
                                                          ▼
[workflow-api.json] → [Reference UI middleware] → [Browser: static rendering]

No Temporal client. No live data. No write paths. No catalog ingestion.
```

If a diagram requires more arrows than that, it isn't v1.

---

## 5. Required Security Checks / Tests to Ship MVP

These are **blocking** for v1.0. Every item produces a test or CI check.

### 5.1 No-secrets scan (BLOCKING)

- Tool: `gitleaks` (or `trufflehog`) run in CI on every PR and on `main`.
- Scope: entire repo including `docs/`, `examples/`, `tests/fixtures/`, `**/snapshots/`, `.agents/`, `**/*.generated.*`.
- Custom rules: detect Temporal-shaped artifacts (PEM blocks, `temporal_api_key_`, mTLS cert bodies).
- A baseline allowlist (if needed) is a separate file, reviewed by Bobbie, with an expiry date per entry.

### 5.2 Write-op refusal tests (BLOCKING)

- **Architecture test:** assert no project in the v1 release set has a reference to `Temporalio.Client.*` (allowlist enforced).
- **Integration test:** boot the reference UI host with default options; `GET /api/runtime/*` returns 404; `POST` to any `/api/workflow/*/start|signal|terminate|cancel|reset|update` route returns 404 (route does not exist).
- **CLI test:** invoking `workflowapi start|signal|terminate|cancel|reset|update` returns exit code ≠ 0 with message "not supported in v1".
- **Symbol test:** no public type in the v1 surface area named `*Client`, `*Worker`, `Start*`, `Signal*`, `Terminate*` related to runtime operations.

### 5.3 Overlay-disabled tests (BLOCKING)

- Reference UI renders correctly when no overlay provider is registered (snapshot match).
- Reference UI renders correctly when an overlay provider *throws* on every call (graceful degradation; spec graph visible; no error blocks page load).
- Capabilities endpoint, if present at all, returns `{ "providers": [] }` and never leaks provider config.
- Snapshot test of the public-view emission: confirms overlay-related metadata is absent.

### 5.4 PII / classification tests

- Snapshot test: a workflow with PII-classified search attributes produces a public document that strips them.
- Diagnostic test: a workflow with a `CustomerEmail` search attribute and no classification raises `WFAPI-SEC-002`.
- Diagnostic test: a property named `ApiKey` raises `WFAPI-SEC-001` (error) unless explicitly overridden.

### 5.5 UI / web security

- CSP header present on reference UI responses: `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'`.
- All rendered fields (names, descriptions, error messages) escape HTML; snapshot test of a workflow with `<script>` in description renders escaped.
- Reference UI middleware default behaviour: enabled in `Development`, requires authorisation outside `Development`; integration test confirms 401/403 in `Production` env without auth.

### 5.6 Supply chain

- SBOM published per NuGet package and per container image (CycloneDX).
- Container images: non-root user, read-only root FS, distroless or `mcr.microsoft.com/dotnet/aspnet:10.0-*-chiseled` base, no shell.
- `dotnet list package --vulnerable` clean (or documented waivers).
- CodeQL + Dependabot enabled.

### 5.7 Document parser limits

- Max document size: 5 MB (configurable, default enforced).
- Max nesting depth: 64.
- Max schema `$ref` resolution depth: 32; cycle detection mandatory.
- Tests for each limit with a fuzz-style fixture.

### 5.8 Agent skill review

- All `.agents/skills/**/*.md` reviewed by Bobbie before merge.
- No skill auto-approves shell, network, or write operations.
- A test enumerates skill files and fails if any contain pre-approval directives.

---

## 6. UI: No Direct Temporal Calls From Browser — Enforcement Plan

Spec §11 says this; we make it real:

1. **Frontend dependency policy:** the UI core npm package (`@workflowapi/ui-core`) declares no dependency on `@temporalio/*` or any Temporal SDK. Enforced by a `package.json` audit test in CI.
2. **No Temporal hostnames in frontend code:** lint rule (regex) forbids `temporal.io`, `:7233`, `temporal-frontend` URLs in `*.ts(x)` / `*.js(x)` files under UI core.
3. **No outbound `fetch`/`XMLHttpRequest` to non-same-origin hosts** from UI core. Enforced by CSP `connect-src 'self'` and a runtime assertion in dev builds.
4. **Build-time check** that the shipped UI bundle does not contain the strings `temporal-frontend`, `WorkflowService/StartWorkflowExecution`, `grpc-web`, or `@temporalio` (post-bundle grep step in CI).
5. **Backend-only Temporal credentials:** documented in §11.7, restated in onboarding docs. Any code review introducing browser-side credential handling is auto-rejected.
6. **Runtime overlay browser contract** (when it ships post-v1): browser calls only `/api/runtime/*` on the same origin. Browser never sees Temporal namespace credentials, only display strings.

For v1 specifically: since there is no `/api/runtime/*` in v1, the browser has *no* path to Temporal. This is the cleanest possible posture and we must not regress it.

---

## 7. Gaps in the Spec — Must Be Added Before Shipping v1

Listed by severity. Each is small; none should block timeline.

### Must-fix before v1.0

1. **§14 doesn't define the secret-name denylist** that generators must enforce. Add the regex and the `WFAPI-SEC-001` diagnostic ID to §14 and to §12 (Conformance).
2. **§14 doesn't normatively require the classification enum.** Make `public | internal | business-identifier | pii | sensitive | restricted` normative for v1.
3. **§14 doesn't specify default classification** when omitted. Specify default = `internal` and public-view stripping rules.
4. **§18 (v1 scope) doesn't explicitly call out that the v1 reference UI requires zero runtime credentials.** Add it — this is a *feature* and a defensive posture.
5. **No spec text yet on parser limits** (size, depth, ref depth). Add a short "Robustness" subsection to §01 or §12 with the limits in §5.7 above.
6. **No spec text on CSP / web headers** for the reference UI host. Add to §04 §10 or §11 §10.
7. **No spec text on agent skill safety.** Add a short subsection to §14 referencing §19, stating that skills are reviewed assets and no skill may pre-approve destructive capabilities.
8. **§14 lists forbidden v1 write ops as prose only.** Add the **enforcement model** (structural absence + tests) as a normative requirement.
9. **CLI verb policy** is implicit. Add an allowed-verbs list to §15 and forbid execution verbs in v1.

### Should-fix before v1.0

10. **Document size / depth diagnostics** need codes (`WFAPI-LIM-001`, etc.) and conformance fixtures.
11. **Public/internal/debug view emission rules** are described qualitatively; need a normative table mapping every WorkflowAPI field to its inclusion rule per view.
12. **Container baseline** in §14 is a bulleted list; needs a published reference Dockerfile so adopters don't roll their own incorrectly. (Alex/Amos to own; Bobbie reviews.)
13. **SBOM + image signing**: spec mentions, doesn't pick tools. Recommend CycloneDX + cosign; pin in §14.

### Nice-to-have (can land in v1.x)

14. RBAC scope strings in §14 are good — add a worked example mapping them to ASP.NET Core authorisation policies.
15. Add a "responsible disclosure" / SECURITY.md template under `repo-template/`.

---

## 8. Acceptance Criteria for "Security-ready for v1"

I will sign off v1.0 when:

- [ ] CI no-secrets scan is enabled and green on `main`.
- [ ] Write-op refusal tests (§5.2) exist and pass.
- [ ] Overlay-disabled tests (§5.3) exist and pass.
- [ ] PII classification diagnostics (§5.4) emit and are tested.
- [ ] Reference UI CSP + escape tests (§5.5) pass.
- [ ] SBOM published; container images non-root + minimal base; `dotnet list package --vulnerable` clean.
- [ ] Parser limits enforced and tested.
- [ ] Spec gaps §7.1–§7.9 closed (Naomi).
- [ ] SECURITY.md present at repo root.
- [ ] No `Temporalio.Client.*` reference in v1 release set (architecture test passes).
- [ ] No `/api/runtime/*` route registered in v1 reference UI (integration test passes).

---

## 9. Open Questions for the Team

1. **Catalog server in v1?** §18 lists catalogue collation as OOS, but §08 exists and §04 mentions it. If catalog is OOS for v1, I want the standalone catalog binary excluded from v1 release artifacts to keep the attack surface small. **Decision needed from Holden.**
2. **Authentication baseline for the reference UI in shared dev environments**: do we ship a default OIDC handler or leave it entirely to the host? My recommendation: leave it to the host, but provide a sample. (Alex.)
3. **Do we run gitleaks or trufflehog?** Recommend gitleaks for speed + good defaults. (Holden.)
4. **Container base image:** chiseled vs distroless. Recommend chiseled `aspnet:10.0`. (Amos.)

---

## References

- `docs/specs/14-security-and-privacy.md`
- `docs/specs/16-runtime-overlay-api.md`
- `docs/specs/18-v1-scope-and-non-goals.md`
- `docs/specs/03-temporal-dotnet-binding.md`
- `docs/specs/04-reference-ui-and-catalog.md`
- `docs/specs/11-ui-core-and-runtime-plugin-architecture.md`
- Charter: `.squad/agents/bobbie/charter.md`
- Skill: `.agents/skills/security-review/SKILL.md`

— Bobbie


<!-- merged from holden-mvp-plan.md on 2026-06-17 -->
# Decision: WorkflowAPI v1 MVP Readiness & Delivery Plan (Independent Pre-Synthesis)

- **Author:** Holden (Lead)
- **Date:** 2026-06-17
- **Status:** Draft — awaiting Naomi/Amos/Alex/Miller/Bobbie input before final synthesis
- **References:** docs/specs/01, 02, 03, 04, 09, 10, 12, 13, 14, 15, 18; team.md; routing.md

## Summary

WorkflowAPI v1 has a strong specification corpus (19 spec docs, 3 JSON schemas, 2 example YAMLs) but **almost no implementation code yet** (`src/` contains only `src/web/` placeholders, no `.csproj`, no schemas under repo root, no monorepo skeleton from spec 10). MVP is achievable in ~5 milestones if we (a) freeze spec scope hard against the v1 non-goals, (b) build the .NET core before any UI/Temporal work, and (c) ship a real conformance harness on day one so every later workstream lands against fixtures.

## 1. MVP Readiness Assessment

### What is specified (✅ ready to build against)
| Area | Status | Source |
|---|---|---|
| v1 scope & non-goals | Frozen and explicit | spec 18 |
| Top-level document shape (`workflowApi`, `info`, `host`, `bindings`, `workflows`, `activities`, `bridges`, `components`) | Specified with canonical example | spec 01 §4–5, 09 |
| `run`/signals/queries/updates/activities semantics | Specified | spec 01 §3.2–3.3 |
| Topology (entry/steps/edges) — declared display only | Specified | spec 01 §3.4 |
| Temporal binding fields (workflowType, namespace, taskQueue, signal/query/update names) | Specified | spec 01, 03 |
| .NET package architecture & dependency graph | Specified — 9 packages, clean layering | spec 02 |
| Attribute + fluent authoring DX | Specified with code samples | spec 02 §3–4 |
| Validation layers, diagnostic codes, profiles | Specified | spec 12 |
| Compatibility/diff rules | Specified | spec 13 |
| Security non-negotiables (no secrets, read-only overlays) | Specified | spec 14 |
| CLI surface (`validate`, `export`, `diff`, `publish`) | Specified | spec 15 |
| Monorepo layout, CI workflows, ADR list | Specified | spec 10 |

### What is missing or unverified (❌ MVP blockers)
1. **Monorepo skeleton does not exist.** `src/` has only `src/web/` placeholders. No `WorkflowApi.*.csproj`, no solution file, no `tests/`, no `.github/workflows/`, no root `schemas/` directory (schemas live under `docs/specs/schemas/` only). Spec 10 §14 is unstarted.
2. **JSON Schema present but not load-tested.** `docs/specs/schemas/workflowapi.schema.json` exists — we have not validated it parses as Draft 2020-12 nor that the canonical example in spec 01 §5 validates against it. Naomi must confirm.
3. **No ADRs exist.** Spec 10 §12 lists 8 mandatory ADRs (`0001-use-workflowapi-name` … `0008-standalone-catalog-server`). None of these is in `docs/adr/` (directory does not exist). These are gating because they encode irreversibles (Temporal first, monorepo first, Scrutor scanning, no UI dep on overlays).
4. **No conformance fixtures.** Spec 12 §"Canonical example set" requires ≥10 valid + ≥10 invalid fixtures with `.diagnostics.json` peers. We have 2 example YAMLs (`risk-enrichment`, `document-service-generate-pdf.bridge`) — no negative fixtures, no diagnostics expectations.
5. **No validator / diagnostic registry.** Diagnostic codes are named (`WFAPI_CORE_*`, `WFAPI_TEMPORAL_*`) but not enumerated or implemented. This blocks Miller's CLI and every CI gate.
6. **Bridges and `bindings.temporal` field-by-field grammar.** Spec 01 lists "optional bridge references" but the bridge sub-schema and the Temporal Nexus binding fields are scattered across specs 03/09 — need a single normative reference before Amos generates types.
7. **`workflowApi` version string semantics.** Spec 13 says use SemVer for the format but the canonical example uses `workflowApi: 1.0.0` while spec 10 §8.1 uses `workflowApi: 0.1.0`. We must pick one and freeze before any generator emits documents (otherwise the snapshot tests will churn).
8. **Reference UI document contract.** Spec 04/11 describe the UI but the **minimal data contract** (what JSON shape the renderer actually consumes — full document? a flattened view model?) is not pinned. Alex needs this before scaffolding React work.
9. **`info.version` vs `workflow.version` reconciliation in generator.** Spec 13 §"Version concepts" defines 6 versions; spec 02 examples show only `info.version`. Amos needs a per-workflow versioning attribute or this falls out of spec on first emit.
10. **Examples directory placement is inconsistent.** Spec 10 puts canonical examples under `samples/` and `tests/schema/`; spec 12 puts them under `examples/specs/`; today they live under `examples/`. Pick once.

### What is in scope per spec 18 but at risk of slipping
- Bridges (`optional bridge references`) — only one example exists; under-specified semantically.
- Fluent definition API surface — spec 02 §3.1 shows it but no precise interface signatures.
- MSBuild `dotnet build /p:GenerateWorkflowApi=true` task — spec 02 §2.6 names it; no design for the task itself.

## 2. Critical Path / Sequencing

**Hard order (cannot parallelize):**
```
Spec freeze (ADRs 0001-0008, version pick, bridge grammar)
    ↓
JSON Schema + canonical fixture set + diagnostic code registry
    ↓
WorkflowApi.Abstractions (model types only, no behaviour)
    ↓
WorkflowApi.AspNetCore (document builder + fluent + serialization)
    ↓
WorkflowApi.Temporal + WorkflowApi.AspNetCore.Temporal (attribute readers + binding)
    ↓
End-to-end: Temporal sample → emits document → validates → renders in UI
```

**Parallelizable once Abstractions is shape-stable:**
- Validator library + CLI (Miller) — needs schema + diagnostic codes, not the generator
- Reference UI scaffold + static-render against fixture documents (Alex) — needs fixtures, not the generator
- Security review + CI no-secrets gate (Bobbie) — needs repo skeleton only
- Diff engine (Miller, can defer to M5) — needs Abstractions model types

## 3. Workstream Breakdown by Squad Member

### Naomi (📐 Spec Dev) — owns spec freeze and schema
- Author ADRs 0001, 0003, 0004 (name, Temporal-first, separate UI from generation), and a new ADR-009 picking the v1 `workflowApi` format version string.
- Reconcile and consolidate the bridge sub-schema across specs 01/03/09 into a single normative section.
- Verify `workflowapi.schema.json` against canonical example from spec 01 §5; add a minimal example fixture if it doesn't validate.
- Author the per-workflow `version` field rules referenced in spec 13.
- Deliverable: spec frozen at SHA, manifest.json regenerated, all v1 examples validate.

### Amos (🔧 .NET Dev) — owns Abstractions + AspNetCore + MSBuild + CLI host
- Scaffold the monorepo per spec 10 §3 (.sln, src/dotnet/WorkflowApi.*, tests/dotnet/*, eng/, root `schemas/` symlink or copy).
- Implement `WorkflowApi.Abstractions` model types from spec 09.
- Implement `WorkflowApi.AspNetCore`: `AddWorkflowApi`, `MapWorkflowApi`, fluent builder, document provider, deterministic JSON serializer.
- Implement Scrutor-based scanner for `[WorkflowApi*]` attributes.
- Implement `WorkflowApi.MSBuild` build-time export.
- Implement `WorkflowApi.Cli` shell (commands stubbed; logic delegated to libraries).
- Snapshot tests for all four authoring paths (Path A fluent, Path B attribute, Document-only, UI-only).

### Alex (🌀 Temporal & UI Dev) — owns Temporal binding + Reference UI
- Implement `WorkflowApi.Temporal`: Temporal SDK attribute readers (no AspNetCore dep).
- Implement `WorkflowApi.AspNetCore.Temporal`: `WithTemporal(...)`, Temporal-aware scanner, binding metadata injection.
- Build the Temporal single-service sample (order-fulfilment) that emits a real document validating against the schema.
- Scaffold `WorkflowApi.Reference` UI (React + embedded assets) — render info, workflows, operations, topology, schemas from a static document. **No** runtime overlay, **no** Temporal client calls.
- Owns: pinning the UI document data contract (deliverable to Naomi for spec inclusion if needed).

### Miller (🧪 Conformance) — owns validator, fixtures, diagnostics, diff
- Enumerate the v1 diagnostic code registry (`WFAPI_CORE_*`, `WFAPI_TEMPORAL_*`, `WFAPI_STYLE_*`) with stable codes, messages, severities — publish to spec 12 as appendix.
- Build the validator library (layers: schema → core semantic → Temporal binding) consumable from `WorkflowApi.Cli`.
- Author ≥10 valid and ≥10 invalid fixtures with `.diagnostics.json` peers, covering: missing run, duplicate workflow id, unknown step ref, missing Temporal task queue, unsupported search attribute type, secret-in-document, deprecated without guidance.
- Wire `validate-spec.yml` CI to run validator against every fixture and every example.
- Diff engine for compatibility checking (spec 13) — can land in M5.

### Bobbie (🔒 Security) — owns security gates and write-op enforcement
- Author `SECURITY.md` and the no-secrets CI gate (regex + entropy scan over examples, fixtures, generated outputs).
- Static check enforcing **no write/control-plane calls** in `WorkflowApi.Temporal` or `WorkflowApi.AspNetCore.Temporal` (block on `IWorkflowClient.StartWorkflowAsync` etc. — only attribute reading is permitted in v1).
- Review of `WorkflowApi.Reference` to confirm zero Temporal credentials reach the browser.
- Threat model for the document endpoint and CLI publish path.
- Dockerfile review for any container we ship (deferred — catalog server is **out of v1**, so v1 has no container).

## 4. Risks and Open Questions Blocking MVP

| # | Risk / Question | Severity | Owner to resolve |
|---|---|---|---|
| R1 | `workflowApi` format version inconsistency (1.0.0 vs 0.1.0 across specs) | High — blocks Amos | Naomi (ADR-009) |
| R2 | No canonical bridge grammar; example exists but schema fields are not enumerated | High — blocks Amos/Miller | Naomi |
| R3 | UI document data contract unpinned | Medium — blocks Alex M4 | Alex + Naomi |
| R4 | Spec scope creep risk: spec 10 §14 lists 14 steps but #11–14 (catalog, Aspire, overlays, publish tooling) are **explicitly out of v1 per spec 18**. We must not let drift pull catalog work in. | High — process | Holden enforcement |
| R5 | Scrutor-based discovery interaction with NativeAOT/trimming unknown | Medium — defer | Amos to spike in M2 |
| R6 | Determinism of generated JSON across .NET 10 versions / cultures | Medium | Amos to lock with snapshot test infra |
| R7 | No tests/integration framework yet for Temporal samples — do we boot a real Temporal dev server in CI, or only attribute-read in tests? v1 only needs attribute reading. | Low | Alex/Miller (attribute-only in v1) |
| Q1 | Do we ship `WorkflowApi.Catalog` package stub in v1 (as named in spec 02) or omit per spec 18? | Lead decision | **Holden ruling: OMIT from v1. Reserve name only.** |
| Q2 | Does v1 include `WorkflowApi.Cli publish`? Spec 15 says yes; spec 18 doesn't list it. | Lead decision | **Holden ruling: validate+export+diff only; publish slips to v1.1.** |
| Q3 | NuGet pre-release versioning scheme | Low | Holden/Amos at M5 |
| Q4 | Do fluent and attribute-driven paths produce byte-identical documents for equivalent inputs? | Medium — affects snapshot strategy | Amos + Miller |

## 5. Suggested Milestone Structure

### M1 — Spec Freeze & Skeleton (Week 1–2)
**Definition of done:**
- ADRs 0001–0008 + ADR-009 (version pick) merged
- `workflowapi.schema.json` validates the canonical example
- Monorepo skeleton per spec 10 §3 exists (.sln, projects, tests, schemas/, .github/workflows/, eng/)
- `validate-spec.yml`, `build-dotnet.yml` workflows green on empty projects
- Diagnostic code registry published as spec 12 appendix
- Bridge grammar consolidated into a single normative section
**Owners:** Naomi (lead), Amos (skeleton), Miller (diagnostic registry), Holden (ADR review), Bobbie (no-secrets gate)

### M2 — .NET Core (Week 3–5)
**Definition of done:**
- `WorkflowApi.Abstractions` + `WorkflowApi.AspNetCore` shipped with fluent + attribute authoring
- Document generator emits deterministic JSON
- `AddWorkflowApi` / `MapWorkflowApi` working end-to-end with fluent sample
- Snapshot tests for Path A authoring
- `WorkflowApi.MSBuild` task emits document at build time
**Owners:** Amos (lead), Miller (snapshot infra), Holden (review)

### M3 — Temporal Binding (Week 5–7, partial overlap with M2)
**Definition of done:**
- `WorkflowApi.Temporal` reads Temporal SDK attributes
- `WorkflowApi.AspNetCore.Temporal` produces documents from a Temporal worker assembly
- Order-fulfilment sample emits a document that validates against schema and Temporal-binding profile
- No write/control-plane API surface area (Bobbie-gated)
**Owners:** Alex (lead), Amos (Abstractions support), Bobbie (write-op gate)

### M4 — Reference UI (Week 7–9, parallel with M3 tail)
**Definition of done:**
- `WorkflowApi.Reference` renders info, workflows, operations (signals/queries/updates), activities, declared topology, schemas from a static document
- `MapWorkflowApiReference` works in the Temporal sample
- UI fetches no runtime data; passes Bobbie's credential-free review
- Renders all canonical valid fixtures without errors
- Document data contract pinned in spec
**Owners:** Alex (lead), Naomi (contract pinning), Bobbie (review)

### M5 — Conformance, Diff & Release Candidate (Week 9–11)
**Definition of done:**
- Validator + CLI (`validate`, `export`, `diff`) shipped with `WorkflowApi.Cli`
- ≥10 valid + ≥10 invalid fixtures with diagnostics peers
- Compatibility diff engine implements spec 13 rules
- All examples validate in CI; invalid examples fail with correct diagnostics
- NuGet packages built at `0.1.0-beta.1` (pre-release)
- README, CONTRIBUTING, GOVERNANCE, SECURITY in place
- Holden's full build/test sign-off (`dotnet build`, `dotnet test`, `workflowapi validate` on all examples)
**Owners:** Miller (lead), Amos (CLI host), Bobbie (release security gate), Holden (integration sign-off)

### Explicit non-deliverables for v1 (per spec 18)
- Standalone catalog server
- Aspire hosting integration
- Runtime overlays / metrics / history
- Catalog collation
- `publish` CLI command
- Source polling
- BPMN-style modelling
- Workflow control-plane actions
- UI editor / code generation from diagrams

These are reserved for v1.1+ and should not be allowed to creep into milestone DoDs.

## 6. Pending Synthesis

This is my **independent** assessment. I will reconcile with:
- **Naomi:** spec-side gaps and schema state
- **Amos:** .NET package feasibility, NativeAOT, fluent API shape
- **Alex:** Temporal SDK attribute coverage gaps, UI scope realism
- **Miller:** diagnostic registry and fixture set completeness
- **Bobbie:** security non-negotiables and write-op enforcement mechanism

The final synthesis will resolve any disagreements (especially around M3/M4 parallelism, the `publish` command, and the bridge grammar) and produce the version that ships to Rebecca.

— Holden


<!-- merged from miller-mvp-conformance-readiness.md on 2026-06-17 -->
# Miller — Conformance & Validation MVP Readiness

**Author:** Miller (Conformance)
**Date:** 2026-06-17
**Requested by:** Rebecca Powell
**Scope:** WorkflowAPI v1 (per `docs/specs/18-v1-scope-and-non-goals.md`)
**Status:** Proposal for team consensus

> v1 scope reminder: in scope = core DSL, .NET attribute/fluent definitions, Temporal static binding, static reference UI. **Out of scope** = runtime overlays, catalog collation, source polling, control-plane actions, BPMN, observability. The conformance MVP must therefore exclude `WFAPI_CATALOG_*` and `WFAPI_RUNTIME_*` diagnostics from v1, and exclude `catalog-source` and `runtime-overlay` profiles. They remain reserved namespaces for post-v1.

---

## 1. Conformance test framework — MVP shape

**Repository layout:**

```text
src/dotnet/WorkflowAPI.Validation/        # validator library (Miller)
src/dotnet/WorkflowAPI.Validation.Temporal/ # Temporal binding rules
src/dotnet/WorkflowAPI.Diff/              # diff engine
src/dotnet/WorkflowAPI.Cli/               # `workflowapi` tool (Amos owns host)
tests/conformance/
  fixtures/
    valid/          # positive fixtures (>=10)
    invalid/        # negative fixtures (>=10), each with .diagnostics.json
    diff/           # pairs: before/after + expected-diff.json
  harness/          # xUnit test project: WorkflowAPI.Conformance.Tests
  snapshots/        # Verify-style approved outputs
```

**Test categories (xUnit + Verify.Xunit) for v1:**

1. **Parse tests** — JSON/YAML, duplicate keys, encoding.
2. **Schema tests** — every fixture validated against `workflowapi.schema.json` (Draft 2020-12).
3. **Core semantic tests** — `WFAPI_CORE_*` rules below.
4. **Temporal binding tests** — `WFAPI_TEMPORAL_*` rules.
5. **Style tests** — `WFAPI_STYLE_*` rules (must produce hint, never error).
6. **Diff classification tests** — before/after pairs assert category + per-change codes.
7. **CLI exit-code tests** — `validate` / `diff` exit codes under `default`, `strict`.
8. **Determinism tests** — running validate/export twice on same input produces byte-identical JSON diagnostics (after path normalisation).

**Rule:** every new validation rule MUST land with one positive fixture and one negative fixture in the same PR. The negative fixture MUST pin the exact diagnostic code.

---

## 2. Validator diagnostics — required codes for v1

Stable code prefixes used in v1: `WFAPI_PARSE_*`, `WFAPI_SCHEMA_*`, `WFAPI_CORE_*`, `WFAPI_TEMPORAL_*`, `WFAPI_STYLE_*`. Reserved (not emitted in v1): `WFAPI_CATALOG_*`, `WFAPI_RUNTIME_*`.

### `WFAPI_PARSE_*` (error)
| Code | Trigger |
|---|---|
| `WFAPI_PARSE_INVALID_JSON` | JSON parser failed |
| `WFAPI_PARSE_INVALID_YAML` | YAML parser failed |
| `WFAPI_PARSE_DUPLICATE_KEY` | duplicate object key (when parser supports detection) |
| `WFAPI_PARSE_UNSUPPORTED_ENCODING` | non-UTF8 input |

### `WFAPI_SCHEMA_*` (error)
| Code | Trigger |
|---|---|
| `WFAPI_SCHEMA_MISSING_REQUIRED` | required property absent (generic shim over JSON Schema error) |
| `WFAPI_SCHEMA_INVALID_TYPE` | property has wrong JSON type |
| `WFAPI_SCHEMA_INVALID_ENUM` | enum value not allowed (e.g. `step.kind`) |
| `WFAPI_SCHEMA_ADDITIONAL_PROPERTY` | unexpected property in strict object |
| `WFAPI_SCHEMA_INVALID_FORMAT` | format/regex mismatch (e.g. id, semver) |

> Each schema error MUST carry the JSON Pointer `path` and the original JSON Schema keyword in `data.keyword`.

### `WFAPI_CORE_*` — semantic rules (v1 minimum)
| Code | Severity | Rule |
|---|---|---|
| `WFAPI_CORE_DUPLICATE_WORKFLOW_ID` | error | two workflows share `id` in one document |
| `WFAPI_CORE_DUPLICATE_STEP_ID` | error | duplicate step id within a workflow |
| `WFAPI_CORE_UNKNOWN_STEP_REFERENCE` | error | edge source/target missing |
| `WFAPI_CORE_UNKNOWN_WORKFLOW_REFERENCE` | error | child workflow / subflow ref unresolved |
| `WFAPI_CORE_UNKNOWN_ACTIVITY_REFERENCE` | error | activity ref unresolved |
| `WFAPI_CORE_UNKNOWN_SCHEMA_REFERENCE` | error | `$ref` into components/schemas missing |
| `WFAPI_CORE_AMBIGUOUS_STEP_IMPLEMENTATION` | error | step declares both `activityRef` and `workflowRef` |
| `WFAPI_CORE_MISSING_RUN_OPERATION` | error | concrete workflow without `run` and not `abstract: true` |
| `WFAPI_CORE_BRIDGE_MISSING_TARGET` | error | bridge declares operation, omits target |
| `WFAPI_CORE_DUPLICATE_OPERATION_NAME` | error | duplicate signal/query/update name within a workflow |
| `WFAPI_CORE_QUERY_NOT_READONLY` | error | query has mutating semantics flag |
| `WFAPI_CORE_INVALID_VERSION` | error | `workflowApi`/`info.version`/`workflow.version` not semver |
| `WFAPI_CORE_DEPRECATION_WITHOUT_GUIDANCE` | warning | `deprecated: true` without `deprecation.reason` |
| `WFAPI_CORE_EXAMPLE_SCHEMA_MISMATCH` | warning | example does not validate against referenced schema |
| `WFAPI_CORE_UNUSED_SCHEMA` | hint | schema in `components` is unreferenced |

### `WFAPI_TEMPORAL_*` — binding rules (v1)
| Code | Severity | Rule |
|---|---|---|
| `WFAPI_TEMPORAL_MISSING_WORKFLOW_TYPE` | error | concrete workflow missing `bindings.temporal.workflowType` |
| `WFAPI_TEMPORAL_MISSING_TASK_QUEUE` | error | host doc has Temporal binding but no `taskQueue`/`taskQueues` |
| `WFAPI_TEMPORAL_MISSING_NAMESPACE` | warning | namespace omitted (allowed for portable docs, warn otherwise) |
| `WFAPI_TEMPORAL_UNSUPPORTED_SEARCH_ATTRIBUTE_TYPE` | error | type not in Temporal-supported set |
| `WFAPI_TEMPORAL_NEXUS_INCOMPLETE` | error | Nexus bridge binding missing service/operation/endpoint |
| `WFAPI_TEMPORAL_CREDENTIAL_LEAK` | error | document contains apiKey/clientCert/secret-shaped fields (coordinate with Bobbie) |
| `WFAPI_TEMPORAL_DUPLICATE_WORKFLOW_TYPE` | error | two workflows share `workflowType` in same task queue |

### `WFAPI_STYLE_*` — quality hints (v1, never error even in strict)
| Code | Severity |
|---|---|
| `WFAPI_STYLE_MISSING_SUMMARY` | hint |
| `WFAPI_STYLE_NONBUSINESS_STEP_NAME` | hint |
| `WFAPI_STYLE_SCHEMA_NAMING_CONVENTION` | hint |
| `WFAPI_STYLE_POSSIBLE_PII_IN_SEARCH_ATTRIBUTE` | warning (Bobbie owns the heuristic list) |

**Diagnostic JSON contract (frozen for v1):**

```json
{
  "code": "WFAPI_CORE_UNKNOWN_STEP_REFERENCE",
  "severity": "error",
  "message": "Edge 'a->b' references unknown target step 'b'.",
  "path": "$.workflows.order-fulfilment.edges[2].target",
  "source": "path/to/file.json",
  "data": { "keyword": "ref", "expected": "step id" }
}
```

Top-level envelope:

```json
{
  "workflowApiValidator": "0.1.0",
  "documents": [
    { "source": "...", "profile": "core-document", "diagnostics": [...] }
  ],
  "summary": { "errors": 0, "warnings": 0, "infos": 0, "hints": 0 }
}
```

Field order is stable. Diagnostics sorted by `(source, path, code)`. No timestamps. No machine paths unless `--debug`.

---

## 3. CLI validation surface — `workflowapi validate` MVP

Owned by Miller (logic) on top of Amos's CLI host project.

```text
workflowapi validate <path>... [options]
```

**MVP options (v1):**

```text
--profile core-document|temporal-binding|reference-ui   (default: core-document; reference-ui and temporal-binding are additive)
--recursive
--strict                       # warnings become non-zero exit
--format text|json|sarif       # sarif optional-but-targeted for v1
--output <path>
--fail-on error|warning        # overrides default exit policy
--no-color                     # CI ergonomics
```

**Exit codes (frozen):**

| Code | Meaning |
|---|---|
| 0 | success (per active fail-on policy) |
| 1 | validation failure (errors, or warnings under strict/--fail-on warning) |
| 2 | usage error (bad args, file not found) |
| 3 | internal validator error (bug) |

**Out of MVP / v1:** `--profile catalog-source`, `--profile runtime-overlay`, `advisory` mode, `catalog` mode. Reserve the flag values; reject with a clear "post-v1" message.

**Input forms accepted:** single file, directory (with `--recursive`), glob, `-` for stdin (JSON only).

**Output rules:**

- text format: GCC-style `path:line:col: severity code message`, plus terminal summary.
- json format: matches the envelope above; byte-stable.
- sarif: SARIF 2.1.0, rules registry populated from the diagnostic code table.

---

## 4. Fixture coverage plan — minimum set to ship v1

Spec demands ≥10 positive and ≥10 negative. v1 minimum I will ship:

**Positive (`tests/conformance/fixtures/valid/`):** 12
1. `minimal-valid.workflowapi.json` — one workflow, run only.
2. `signals-queries-updates.workflowapi.json` — all operation kinds.
3. `activities-only.workflowapi.json` — workflow with activity steps + edges.
4. `child-workflow.workflowapi.json` — nested subflow reference.
5. `bridge-basic.workflowapi.json` — bridge with target, no Temporal.
6. `temporal-basic.workflowapi.json` — Temporal binding (workflowType, taskQueue, namespace).
7. `temporal-search-attributes.workflowapi.json` — all supported SA types.
8. `temporal-nexus-bridge.workflowapi.json` — full Nexus binding.
9. `deprecated-workflow.workflowapi.json` — deprecation block populated.
10. `abstract-workflow.workflowapi.json` — `abstract: true`, no run.
11. `reference-ui-display-topology.workflowapi.json` — explicit display topology.
12. `multi-workflow-document.workflowapi.json` — three workflows, shared schemas.

**Negative (`tests/conformance/fixtures/invalid/`):** 15 — one per error code, each paired with `<name>.diagnostics.json`:

- `parse-invalid-json` → `WFAPI_PARSE_INVALID_JSON`
- `schema-missing-info` → `WFAPI_SCHEMA_MISSING_REQUIRED`
- `schema-invalid-step-kind` → `WFAPI_SCHEMA_INVALID_ENUM`
- `core-duplicate-workflow-id` → `WFAPI_CORE_DUPLICATE_WORKFLOW_ID`
- `core-duplicate-step-id` → `WFAPI_CORE_DUPLICATE_STEP_ID`
- `core-unknown-step-reference` → `WFAPI_CORE_UNKNOWN_STEP_REFERENCE`
- `core-unknown-workflow-reference` → `WFAPI_CORE_UNKNOWN_WORKFLOW_REFERENCE`
- `core-unknown-schema-reference` → `WFAPI_CORE_UNKNOWN_SCHEMA_REFERENCE`
- `core-ambiguous-step-implementation` → `WFAPI_CORE_AMBIGUOUS_STEP_IMPLEMENTATION`
- `core-missing-run-operation` → `WFAPI_CORE_MISSING_RUN_OPERATION`
- `core-bridge-missing-target` → `WFAPI_CORE_BRIDGE_MISSING_TARGET`
- `core-duplicate-operation-name` → `WFAPI_CORE_DUPLICATE_OPERATION_NAME`
- `temporal-missing-task-queue` → `WFAPI_TEMPORAL_MISSING_TASK_QUEUE`
- `temporal-unsupported-search-attribute-type` → `WFAPI_TEMPORAL_UNSUPPORTED_SEARCH_ATTRIBUTE_TYPE`
- `temporal-credential-leak` → `WFAPI_TEMPORAL_CREDENTIAL_LEAK`

Each invalid fixture's `.diagnostics.json` file is a sorted array of the exact expected diagnostics (code + path), used as the assertion oracle.

---

## 5. Diff / compatibility tooling — MVP

`WorkflowAPI.Diff` library + `workflowapi diff <old> <new>` CLI command.

**v1 categories (per spec 13):** `contract-breaking`, `contract-non-breaking`, `runtime-binding-change`, `documentation-change`, `metadata-change`, `quality-change`, `unknown-risk`.

**MVP rule coverage:**

- Workflow add/remove/rename (id).
- Run input/output: add optional, add required, remove, type-change.
- Signal/query/update: add, remove, rename, payload property add (optional/required), remove, type-change.
- Bridge: add/remove operation, schema change, target change.
- Steps/edges: add/remove visible business step (classified `contract-non-breaking` or `contract-breaking` with note).
- Temporal binding: namespace/taskQueue/workflowType changes classified as `runtime-binding-change`.
- Deprecation: `deprecated` flip → `metadata-change`; removal of previously-deprecated past `removalAfter` → `contract-breaking` with note.

**Diff output formats:** `text` (default), `json` (machine), `markdown` (PR comment). `--fail-on contract-breaking|warning|any-change`. Same exit code table as `validate`.

**Diff JSON contract (frozen for v1):**

```json
{
  "from": "old.json",
  "to": "new.json",
  "summary": { "contract-breaking": 1, "contract-non-breaking": 3, "runtime-binding-change": 1, "documentation-change": 0, "metadata-change": 0, "quality-change": 0, "unknown-risk": 0 },
  "changes": [
    { "category": "contract-breaking", "code": "WFAPI_DIFF_REQUIRED_INPUT_ADDED",
      "path": "$.workflows.order-fulfilment.run.input.properties.customerId",
      "before": null, "after": { "type": "string" }, "note": "..." }
  ]
}
```

Diff codes use `WFAPI_DIFF_*` prefix. Frozen subset for v1 (≈20 codes, one per cell in the spec-13 tables). Full enumeration to land alongside the diff library PR.

**Diff fixtures:** at minimum one pair per category (≥7 pairs).

---

## 6. Snapshot test discipline — deterministic output rules

These rules are MANDATORY for any subsystem that writes JSON consumed by tests or snapshots (validator diagnostics, diff output, exported documents). I will block PRs that violate them.

1. **No timestamps** in output unless `--debug`.
2. **No absolute or machine-local paths.** Sources are recorded as repo-relative or as the literal user-provided argument.
3. **Stable ordering everywhere:**
   - workflows by `id` (ordinal, invariant culture);
   - operations by kind then name;
   - steps by `id`;
   - edges by `(source, target, label)`;
   - schemas by name;
   - diagnostics by `(source, path, code)`;
   - diff changes by `(category-order, path, code)` where category order is the spec-13 list order.
4. **Stable generated ids.** Hashes use SHA-256 over canonical JSON (sorted keys, no insignificant whitespace, UTF-8). No GUIDs in output.
5. **No culture-sensitive formatting.** `CultureInfo.InvariantCulture` everywhere; ordinal comparisons.
6. **No environment dependency.** No `Environment.MachineName`, `Environment.UserName`, working directory, process id.
7. **Newline policy:** `\n` only in JSON output; `Environment.NewLine` only in text mode.
8. **Snapshots use Verify.Xunit** (`*.verified.json`, `*.received.json`) checked into git. Updates require a snapshot diff in the PR description.
9. **Round-trip test:** parse → serialise → parse must be byte-identical for every positive fixture.
10. **Cross-runner equivalence test (post-Amos integration):** runtime-exported document and build-time-exported document for the same project produce byte-identical JSON.

---

## 7. Dependencies on Naomi (schemas) and Amos (CLI host)

### From Naomi (Spec):
1. **Frozen `workflowapi.schema.json`** for v1 — including `step.kind` enum, `bindings` shape, `components.schemas` shape, semver pattern for versions, and `abstract` field. Without freeze, schema-layer fixtures churn.
2. **Authoritative semver pattern** to reuse in `WFAPI_CORE_INVALID_VERSION`.
3. **Authoritative list of supported Temporal search-attribute types** (or its location in `workflowapi-temporal-binding.schema.json`) for `WFAPI_TEMPORAL_UNSUPPORTED_SEARCH_ATTRIBUTE_TYPE`.
4. **Decision on `abstract` keyword** spelling and placement — `WFAPI_CORE_MISSING_RUN_OPERATION` keys off this.
5. **Confirmation that `WFAPI_CATALOG_*` and `WFAPI_RUNTIME_*` codes are reserved but unused in v1**, so I can document them as "reserved" without ambiguity.
6. **Sign-off on the diagnostic JSON envelope** (above). Once shipped it is a public contract.

Without 1–4, I will write fixtures against a moving target and the negative fixture set will rot weekly.

### From Amos (.NET / CLI host):
1. **`WorkflowApi.Cli` project skeleton** (host, command parser, DI, logging) with stub `validate` and `diff` subcommands that call into `WorkflowAPI.Validation` / `WorkflowAPI.Diff`.
2. **Exit-code contract** wired through the host exactly as specified above; no `Environment.Exit` short-circuits from inside commands.
3. **Deterministic stdout** — no host banner, no version line, no progress output on stdout (stderr only, suppressed under `--format json`).
4. **Process-level newline + culture pinning** (`CultureInfo.InvariantCulture` set in `Main`).
5. **Confirmation he will NOT add a `Temporal` reference to `WorkflowAPI.Validation`**; Temporal rules live in `WorkflowAPI.Validation.Temporal` per repo charter.
6. **MSBuild target placeholder** so I can land a build-time `validate` task in the CI fixture without blocking on full MSBuild work.

Cross-cutting: confirm with Bobbie (security) the secret/credential-shape heuristic list before I freeze `WFAPI_TEMPORAL_CREDENTIAL_LEAK`.

---

## Compatibility & security impact

- **Compatibility:** the diagnostic JSON envelope, the diagnostic code identifiers, the `--fail-on`/`--strict` semantics, and the diff category names become a public CLI contract at v1. Changing any of them post-v1 is a breaking change for CI consumers. I am proposing to freeze them now.
- **Security:** `WFAPI_TEMPORAL_CREDENTIAL_LEAK` and `WFAPI_STYLE_POSSIBLE_PII_IN_SEARCH_ATTRIBUTE` overlap with Bobbie's domain; heuristics will be owned by Bobbie, executed by my validator.
- **v1 scope discipline:** I will NOT ship `WFAPI_CATALOG_*`, `WFAPI_RUNTIME_*`, the `catalog-source`/`runtime-overlay` profiles, or `catalog`/`advisory` modes. They are documented as reserved.

## Open questions for the team

1. Are we shipping SARIF in v1, or deferring? (Recommend yes for v1 — GitHub code-scanning integration is high-leverage at low cost.)
2. YAML input support in `validate` for v1, or JSON-only? (Recommend JSON-only for v1; YAML accepted but normalised through a YAML→JSON shim with `WFAPI_PARSE_INVALID_YAML` errors.)
3. Should `WFAPI_CORE_EXAMPLE_SCHEMA_MISMATCH` be `error` or `warning`? (Recommend warning until JSON Schema example validation is universally trusted.)

## Asks (action items)

- **Naomi:** confirm/freeze items 1–6 in §7 by next sync.
- **Amos:** confirm/scaffold items 1–6 in §7; create the CLI skeleton repo path.
- **Bobbie:** own credential-shape + PII heuristic lists; deliver as data (regex/keyword pack) the validator can consume.
- **Rebecca / coordinator:** approve the frozen diagnostic envelope and diff category list as v1 public contract.

— Miller


<!-- merged from naomi-mvp-spec-readiness.md on 2026-06-17 -->
# Naomi — MVP Spec Readiness Assessment

- **Author:** Naomi (Spec Dev)
- **Date:** 2026-06-17
- **Requested by:** Rebecca Powell
- **Scope:** Is the WorkflowAPI v1 spec + JSON Schema + examples complete enough to start .NET implementation in earnest?

**TL;DR — NOT READY. Yellow/red.** The conceptual surface is well thought out, but the
normative artifacts that an implementer actually consumes (JSON Schema, examples,
version string, validation rules) are mutually inconsistent. Three different
`workflowApi` versions appear across spec, schema and examples, and the two
example documents in `/examples/` do not validate against the published schema.
Amos cannot start a generator/validator against this corpus without us picking
one shape and aligning everything to it.

---

## 1. DSL completeness vs v1 scope (doc 18)

Doc 18 declares the in-scope v1 surface. Coverage in the normative model (doc 09)
and the schema (`schemas/workflowapi.schema.json`):

| v1 in-scope element | Prose (01/09) | JSON Schema | Examples | Status |
|---|---|---|---|---|
| Document model (`workflowApi`, `info`, `host`, `workflows`, `activities`, `bridges`, `components`, `bindings`, `extensions`) | ✅ | ✅ | ⚠️ shape drift | Mostly covered |
| Workflow host metadata | ✅ | ✅ (host obj) | ❌ uses `id`/`owner.team` not in schema | Drift |
| Workflow identity & metadata (`id`, `title`, `version`, `owner`, `domain`, `lifecycle`, `visibility`, `deprecated`) | ✅ | ✅ | ❌ examples use `name`, `displayName` | Drift |
| `run` durable entry point | ✅ | ✅ (via operation) | ⚠️ examples use `input.$ref` instead of `input.schema.$ref` | Drift |
| Signals | ✅ | ✅ | ✅ | OK |
| Queries | ✅ | ✅ | ✅ | OK |
| Updates | ✅ | ✅ | ✅ | OK |
| Activities (top-level registry) | ✅ | ✅ | ❌ examples inline only | Drift |
| Child workflow references (`kind: childWorkflow`, `ref.workflow`, `invocation.*`) | ✅ | ✅ | partial | OK |
| Optional bridge references (`kind: bridge`, `ref.bridge/service/operation`) | ✅ | ✅ (bridge→services→operations nesting) | ❌ flat bridge object, no `services` map | Drift |
| Optional declared display topology (`entry`, `steps`, `edges`) | ✅ | ✅ | ✅ | OK |
| Runtime bindings (Temporal first) | ✅ | ✅ (`additionalProperties:true`) | ✅ | OK |
| .NET attributes / fluent definitions | covered in 06, 02 | n/a | ✅ | OK at design level |
| Static reference UI consumability | ✅ | ✅ | ⚠️ depends on example/schema alignment | OK once aligned |

The conceptual coverage of in-scope elements is **complete**. The execution
coverage (schema + fixtures matching the prose) is **not**.

### Out-of-scope leakage to flag

Doc 09 still contains material the v1-tightened scope (doc 18) explicitly
excludes. It carries a scope-note banner, but several sections continue to
describe v1 artifacts:

- §20 Policies, §21 Search attributes, §22 Events / AsyncAPI refs, §23 Security
  schemes, §26 Catalog collation, §27 Runtime overlay, §28 Validation levels
  (`catalog`, `runtime`).

For MVP we need to either (a) clearly fence these as post-v1, or (b) decide which
slice is in. As written, an implementer reading doc 09 in isolation will build
more than v1 asks for.

## 2. JSON Schema status

`schemas/workflowapi.schema.json` exists, is well-structured (2020-12, uses
`$defs`), and covers the in-scope shapes. Issues blocking MVP:

1. **Version pattern mismatch.** `workflowApi` is `^0\.1(\.\d+)?$`. Doc 01 §4
   uses `workflowApi: 1.0.0`. Doc 09 §6/§29/§30 and both `/examples/*.yaml`
   use `0.1.0`. Doc 13 talks about "v0.1". **Three sources, three versions.** Pick one
   for v1 and propagate.
2. **`run` not required.** Doc 01 §6 validation rule says "each concrete
   workflow has exactly one `run`". Schema marks `run` optional on `workflow`.
   Either tighten the schema (`required: ["run"]` on concrete workflows) or
   loosen the prose (which is what doc 09 §6.1 actually says — "documents may
   publish only bridges/components"). Implementers need one answer.
3. **`additionalProperties: true` everywhere.** Almost every object permits
   arbitrary properties. That's fine as a forward-compat hatch but it means the
   schema cannot detect typos like `runs:` vs `run:` or `singals:` vs
   `signals:`. For MVP I recommend `additionalProperties: false` on the strict
   nodes (`workflow`, `operation`, `step`, `edge`, `topology`) with an
   `x-*`/`extensions` escape hatch, plus `unevaluatedProperties: false` on
   the document root.
4. **Schema-vs-prose drift, concrete items:**
   - Operation: prose lists `errors`, `examples`, `policies`, `extensions`;
     schema has none of these.
   - Step: prose lists `errors`, `policies`, `group`; schema has none.
   - Edge: prose lists `probability`, `bindings`, `extensions`; schema has none.
   - Activity: prose lists `policies`; schema has none (has `expectedDuration`,
     `sla`, `criticality` which the prose mentions inline only).
   - Workflow: prose lists `emits`, `consumes`, `policies`, `security`,
     `extensions`, `operations` (custom kinds); schema has none of these.
     `searchAttributes` exists in schema but is generic-only; binding-typed
     variant (Temporal `Keyword`) is in the Temporal schema — good.
   - `dependsOn` in schema is shape-free (`additionalProperties: true` on
     each list item). Prose §17 gives concrete fields. Tighten.
5. **`schemaRef` is ambiguous.** Schema allows both `{schema: …}` and bare
   `{$ref: …}`. Prose only uses `schema.$ref`. Examples use bare `$ref`. Pick
   one canonical form and reject the other (or accept both and document
   normalization).
6. **No `$id` referenced by `manifest.json`.** Schemas are not listed in the
   manifest. Add them so consumers know which schema version pairs with which
   spec revision.
7. **Temporal binding schema is incomplete.** It lists `nexus.endpoint` etc.,
   but doc 25 / examples also reference `childWorkflowType`, `webUrl`,
   `workerHost`, `sdk`, `workflowRunMethod`. Either add these or document them
   as `x-temporal-*`. Right now `additionalProperties: true` hides the gap.
8. **No negative-test schema metadata.** No `$comment` markers identifying the
   diagnostic codes the validator should emit. Doc 12 (conformance) presumably
   defines codes — wire them in via `errorMessage` / out-of-band table so
   Amos's validator and Miller's conformance suite stay aligned.

## 3. Examples coverage

Two files in `/examples/`:

- `risk-enrichment.workflowapi.yaml`
- `document-service-generate-pdf.bridge.workflowapi.yaml`

Doc 06 also embeds the canonical order-fulfilment example as Markdown.

**Problems:**

1. **The on-disk examples do not validate against the schema.** Concrete
   instances of drift, in order of impact:
   - `host.id`, `host.owner.team`, `host.baseUrl` — none defined in schema.
   - Workflow keys are PascalCase (`RiskEnrichmentWorkflow`); doc 09 §7 mandates
     kebab-case. (Schema does not enforce, but UI/catalog keys will be wrong.)
   - `workflows.*.name` and `displayName` — schema has `title`, no `name`.
   - Operation payloads use `input: { $ref: ... }` instead of
     `input: { schema: { $ref: ... } }`.
   - `bridges.*` is a flat object with `direction`, `target.workflowRef`,
     `binding` — schema requires `services` map with `operations` map.
   - Temporal binding uses `webUrl`, `workerHost`, `sdk` — not in temporal
     binding schema.
2. **Order-fulfilment example exists only in Markdown.** It is the canonical
   demo per doc 06 and doc 01 §5. It must exist as a stand-alone YAML/JSON
   file in `/examples/` so the validator, generator snapshot tests, and UI
   smoke tests can consume it.
3. **No negative fixtures.** Per my charter every construct gets a positive
   AND a negative fixture. We currently have zero negative fixtures. Without
   them Miller cannot build a conformance suite and Amos cannot TDD the
   validator.
4. **No minimal-document fixture.** Doc 09 §29 specifies a minimal valid
   document but it is not on disk.
5. **No edge-case coverage** for child workflow refs, bridge-as-step,
   activities-only documents, bindings inheritance, or deprecation metadata.

## 4. Gaps and ambiguities blocking MVP

Hard blockers (must resolve before Amos starts the .NET generator/validator):

1. **Version identifier.** Pin one `workflowApi` value for MVP (`1.0.0` is
   what doc 18 implies by saying "v1"; doc 13 still says "v0.1"). Update
   schema pattern, all prose, both examples, and the canonical Markdown
   example in doc 06 atomically.
2. **`run` cardinality.** State explicitly that every concrete workflow must
   have exactly one `run`, and that documents containing only bridges/
   activities/components are valid with `workflows: {}` empty or omitted.
3. **`schemaRef` canonical form.** Choose `input.schema.$ref` (matches prose)
   OR allow bare `$ref` (matches examples). Update everything to match.
4. **Bridge shape canonical form.** The flat shape in
   `document-service-generate-pdf.bridge.workflowapi.yaml` and the nested
   `bridges→services→operations` shape in the schema and doc 09 §18 are
   incompatible. Pick one.
5. **Workflow key naming.** Kebab-case is mandated by prose; examples use
   PascalCase. Either change the rule (and update collation key guidance) or
   change the example.
6. **`activities` vs inline activities.** Doc 09 §18a allows both. State the
   precedence rule (top-level wins / required-when-reused / etc.) and add
   the `ref.activity` resolution rule the validator must implement.
7. **Doc 09 scope fencing.** Sections §20–§28 must either be marked "post-v1"
   per section, or pulled into a separate post-v1 design doc, so the implementer
   has an unambiguous list of v1 features.

Soft (should resolve, can be tracked):

8. Whether `components.schemas` is JSON Schema 2020-12, the OpenAPI 3.1
   subset, or both (doc 09 §32 open question 2). Validator behavior depends.
9. Diagnostic-code table for the validator (Naomi + Miller).
10. Deprecation metadata shape — doc 13 specifies it; schema has only a
    boolean `deprecated`. Add the structured `deprecation` object.
11. `extensions` vs `x-*` convention — pick one as canonical for v1.

## 5. Recommended spec work before .NET implementation starts

Suggested ordering, each item completed atomically (prose + schema + examples
+ negative fixture as per the spec skill):

1. **Pin v1 versions.** ADR draft + global s/0.1.0/1.0.0/ (or vice versa).
   Update `manifest.json` and add schema entries to it.
2. **Tighten and align the schema.**
   - Add missing operation/step/edge/workflow/activity fields listed in §2 above.
   - Decide `additionalProperties` policy and apply it.
   - Make `run` required on concrete workflows.
   - Resolve the `schemaRef` ambiguity at the schema level.
3. **Rebuild `/examples/` from scratch:**
   - `minimal.workflowapi.yaml` (doc 09 §29 verbatim).
   - `order-fulfilment.workflowapi.yaml` (canonical demo — promoted from
     Markdown in doc 06; the example the UI MVP renders).
   - `order-fulfilment-with-bridge.workflowapi.yaml` (childWorkflow + bridge).
   - `bridge-only.workflowapi.yaml` (document with only `bridges`).
   - `negative/` directory with at least one fixture per validation rule
     (missing `run`, duplicate workflow key, unknown step ref in edge,
     unresolved activity ref, secret-shaped value, unknown `kind`, etc.).
   - All examples must validate (positive) / fail with a known diagnostic
     code (negative) under the canonical schema.
4. **Canonical bridge shape.** Resolve flat-vs-nested and rewrite the bridge
   example.
5. **Validation rules table.** Promote doc 01 §6 into a numbered diagnostic
   table (`WAPI-001` … `WAPI-NNN`) so Amos's validator and Miller's
   conformance fixtures share one source of truth.
6. **Scope fence doc 09.** Either inline "post-v1" tags per section, or
   move §20–§28 into a new `09a-post-v1-design.md`.
7. **Temporal binding schema completion.** Add `childWorkflowType`,
   `workflowRunMethod`, and flatten the `nexus.*` object to match what the
   examples and doc 25 actually use.
8. **Versioning/deprecation metadata.** Promote doc 13's `deprecation` object
   into the schema.

Once items 1–5 are done, Amos has a stable target. Items 6–8 can run in
parallel with early .NET work but should land before the v1 freeze.

## Compatibility and security impact

- **Compatibility:** Pinning a v1 version string and tightening
  `additionalProperties` are both breaking changes against any current
  consumer. Since nothing is shipping yet, this is the right moment.
- **Security:** No new surface introduced. Reinforce doc 14's "no secrets"
  rule by adding a negative fixture that fails validation when a field looks
  like a credential (e.g. `*Token`, `*Secret`, `password`). Cheap, high signal.

## References

- `docs/specs/01-workflowapi-core-spec.md`
- `docs/specs/06-examples.md`
- `docs/specs/09-workflowapi-normative-document-model.md`
- `docs/specs/13-versioning-and-compatibility.md`
- `docs/specs/18-v1-scope-and-non-goals.md`
- `docs/specs/schemas/workflowapi.schema.json`
- `docs/specs/schemas/workflowapi-temporal-binding.schema.json`
- `docs/specs/schemas/workflowapi-runtime-overlay.schema.json`
- `docs/specs/manifest.json`
- `examples/risk-enrichment.workflowapi.yaml`
- `examples/document-service-generate-pdf.bridge.workflowapi.yaml`


## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

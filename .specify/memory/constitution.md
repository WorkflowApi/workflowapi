<!--
  Sync Impact Report
  ==================
  Version change: 0.0.0 → 1.0.0
  Modified principles: N/A (initial creation)
  Added sections:
    - Core Principles (4): Code Quality, Testing Standards, UX Consistency, Performance
    - Quality Gates
    - Development Workflow
    - Governance
  Removed sections: N/A
  Templates requiring updates:
    - .specify/templates/plan-template.md ✅ (Constitution Check section is generic; no update needed)
    - .specify/templates/spec-template.md ✅ (Success Criteria aligns with performance principle; no update needed)
    - .specify/templates/tasks-template.md ✅ (Polish phase covers performance/testing; no update needed)
  Follow-up TODOs: None
-->

# WorkflowAPI Constitution

## Core Principles

### I. Code Quality

All production code MUST meet the following non-negotiable standards:

- Nullable reference types MUST remain enabled across all .NET projects.
- Public APIs MUST have XML documentation comments.
- No compiler warnings allowed in CI builds; treat warnings as errors.
- Code MUST follow established project layering: Abstractions → Core → Runtime bindings → Host packages.
- Every package MUST have a single, clearly defined responsibility. Cross-cutting concerns (e.g., Temporal) MUST NOT leak into core abstractions.
- Generated output MUST be deterministic and produce stable IDs across runs.
- Dead code, commented-out code, and TODO comments without linked issues MUST NOT be merged.

**Rationale**: WorkflowAPI is a specification ecosystem consumed by other teams. Inconsistent or undocumented public surfaces erode trust and create integration friction.

### II. Testing Standards

All changes MUST be validated through an appropriate testing strategy:

- Unit tests MUST cover all public API surface and edge cases.
- Integration tests MUST cover cross-package interactions (e.g., document model → JSON Schema validation → serialization round-trip).
- Conformance tests MUST validate spec examples against the JSON Schema and validator.
- Snapshot/golden tests MUST protect generated output (UI renders, schema output, document serialization). Snapshot updates require explicit justification in PR description.
- Tests MUST be deterministic: no flaky tests, no dependency on external services, no ordering assumptions.
- New features MUST include both positive (happy path) and negative (error/edge) test fixtures.
- Test names MUST clearly describe the scenario under test using the pattern: `MethodName_Scenario_ExpectedResult`.

**Rationale**: WorkflowAPI's correctness guarantee is its primary value proposition. A spec ecosystem that produces inconsistent validation or output is worse than no spec at all.

### III. User Experience Consistency

All user-facing surfaces (Reference UI, Catalog UI, CLI output, error messages) MUST provide a coherent, predictable experience:

- UI components MUST be accessible (WCAG 2.1 AA) with full keyboard navigation support.
- Error messages MUST be actionable: state what went wrong, why, and how to fix it.
- Graph/visualization fallbacks MUST render meaningful content when JavaScript is disabled or overlays are unavailable.
- The UI core MUST render WorkflowAPI documents without requiring runtime credentials or connections.
- Runtime overlay decorations MUST degrade gracefully when the plugin is unavailable or disconnected, showing clear "unavailable" states rather than broken UI.
- Visual and interaction patterns MUST be consistent across Reference UI and Catalog UI modes.
- CLI output MUST support both human-readable and JSON machine-readable formats.

**Rationale**: WorkflowAPI serves both engineering teams and business stakeholders. A confusing or inaccessible UI undermines adoption regardless of specification quality.

### IV. Performance Requirements

All components MUST meet defined performance budgets:

- JSON Schema validation of a typical WorkflowAPI document (≤50 workflows) MUST complete in <100ms on reference hardware.
- Reference UI initial page load MUST complete in <2 seconds on a 4G connection (Lighthouse performance score ≥90).
- Catalog Server MUST handle ≥100 concurrent document queries with p95 latency <500ms.
- Document serialization/deserialization round-trips MUST not allocate excessively; benchmark critical paths and track regressions.
- Generated .NET code MUST not introduce measurable startup latency beyond the underlying Temporal SDK overhead.
- Performance-sensitive changes MUST include before/after benchmark data in the PR description.

**Rationale**: WorkflowAPI tooling runs in developer inner loops and production dashboards. Sluggish validation or UI renders break flow and discourage adoption.

## Quality Gates

All pull requests MUST pass these gates before merge:

- All CI checks green (build, test, lint, schema validation).
- No reduction in test coverage for modified files.
- Snapshot/golden tests updated with justification if changed.
- Performance benchmarks show no regression beyond 10% tolerance.
- Documentation updated for any public API or DSL change (spec prose, JSON Schema, examples, validator tests).
- Security: no secrets, credentials, or PII in code, specs, examples, or generated output.

## Development Workflow

Development follows these mandatory practices:

- Changes to the DSL MUST synchronize: prose spec, JSON Schema, examples, validator tests, and docs.
- Changes to generated output MUST update snapshot/golden tests.
- Runtime-specific logic (Temporal) MUST reside in binding/overlay packages, never in core.
- PRs MUST be small, focused, and include clear acceptance criteria.
- Breaking changes MUST follow semantic versioning and include migration guidance.
- Complexity MUST be justified; prefer the simplest solution that satisfies requirements (YAGNI).

## Governance

This constitution is the authoritative source of engineering standards for WorkflowAPI. It supersedes informal practices and ad-hoc decisions.

- **Amendment process**: Propose changes via PR to this file. Changes require review and explicit approval. Each amendment MUST include a migration plan for existing code that violates new rules.
- **Versioning**: Constitution follows semantic versioning (MAJOR.MINOR.PATCH). MAJOR for principle removals/redefinitions, MINOR for new principles or material expansions, PATCH for clarifications.
- **Compliance**: All PRs and code reviews MUST verify compliance with these principles. Violations MUST be flagged and resolved before merge.
- **Exceptions**: Temporary exceptions MUST be documented with a linked issue and a timeline for resolution.

**Version**: 1.0.0 | **Ratified**: 2026-06-17 | **Last Amended**: 2026-06-17

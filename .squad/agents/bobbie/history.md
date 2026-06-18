# Project Context

- **Owner:** Rebecca Powell
- **Project:** WorkflowAPI — specification and tooling ecosystem for durable workflow APIs, analogous to OpenAPI and AsyncAPI
- **Stack:** .NET 10 LTS, C#, JSON Schema, TypeScript/React (Reference UI), Temporal .NET SDK
- **Created:** 2026-06-13

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

## 2026-06-17 — v1 MVP security readiness assessment

Requested by Rebecca Powell. Produced full v1 security posture review covering:
- read-only enforcement via structural absence (no Temporal client in v1 UI/catalog, no /api/runtime/* routes, CLI verb policy)
- secrets handling (denylist regex + WFAPI-SEC-001 diagnostic, config provider patterns; v1 needs zero runtime credentials)
- PII / search attribute classification (closed enum, default=internal, public-view stripping, WFAPI-SEC-002 warning)
- threat model for v1 (component inventory, top 10 threats T1–T10, data flow)
- required blocking checks: gitleaks no-secrets scan, write-op refusal tests, overlay-disabled tests, PII tests, CSP + escape tests, supply chain SBOM/non-root containers, parser limits, agent skill review
- browser-no-Temporal enforcement: npm dep policy, CSP connect-src 'self', post-bundle grep
- 9 spec gaps to close before v1.0 (denylist regex, classification enum normative, default classification, zero-credentials feature, parser limits, CSP, agent skill safety, structural enforcement model, CLI verb allowlist)

Open questions flagged for Holden (catalog in v1?, tool choice), Alex (OIDC sample), Amos (container base).

Persisted: decisions/inbox/bobbie-mvp-security-readiness.md

## 2026-06-17 — Team MVP evaluation (requested by Rebecca Powell)

Produced independent v1 MVP security posture assessment. Three load-bearing rules: no runtime overlay paths in v1 binaries, no Temporal client in any UI/catalog process, blocking CI no-secrets scan. WorkflowAPI v1 itself requires zero runtime credentials — defended as a feature. Nine §14 spec gaps catalogued; diagnostics WFAPI-SEC-001/002 specified. Submission archived under decisions.md; see orchestration-log/2026-06-17T12-37-19Z-bobbie.md.

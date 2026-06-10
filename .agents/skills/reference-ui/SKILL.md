---
name: reference-ui
description: WorkflowAPI Reference UI Skill
---

# WorkflowAPI Reference UI Skill

Use this skill for generic UI core, graph rendering, EventCatalog visualiser adapter, runtime overlay decoration, and single-service reference UI. UI core must work without Temporal.

Before finishing:

- Check affected docs.
- Check affected schemas.
- Check examples and tests.
- State compatibility and security impact.
- Do not broaden scope beyond the requested slice.
- For UI containers use Shadcn UI primitives and Tailwind. For graph rendering use D3 or similar. For visualiser adapter use the EventCatalog schema and docs as reference. For runtime overlay decoration use plugin-provided metadata and docs as reference.

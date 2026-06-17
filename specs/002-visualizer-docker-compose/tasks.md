# Tasks: Visualizer Docker Compose Integration

**Input**: Design documents from `/specs/002-visualizer-docker-compose/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: No automated test tasks are included (spec/plan request manual acceptance validation).

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare shared environment/config files used by all stories.

- [X] T001 Create canonical environment template in src/.env.example by merging values from src/temporal-local/.env and adding VISUALIZER_PORT=3000
- [X] T002 [P] Ensure runtime env file is ignored by updating/confirming src/.env handling in .gitignore
- [X] T003 Add usage comments for src/.env creation and VISUALIZER_PORT override directly in src/.env.example

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the new compose root and keep existing Temporal stack behavior intact before story work.

- [X] T004 Create new root orchestration file at src/docker-compose.yml from src/temporal-local/docker-compose.yml
- [X] T005 Update moved-compose relative paths in src/docker-compose.yml for temporal volume (./temporal-local/dynamicconfig) and worker build context (./temporal-local/worker)
- [X] T006 Preserve existing dependency/healthcheck startup chain for postgresql, temporal, temporal-ui, and risk-worker in src/docker-compose.yml

**Checkpoint**: Foundation ready — user story work can begin.

---

## Phase 3: User Story 1 - Start full local stack with one command (Priority: P1) 🎯 MVP

**Goal**: `docker compose up --build` from `src/` starts all required services together and `docker compose down` stops all of them.  
**Independent Test**: From `src/`, run `docker compose up --build`; verify Temporal UI (`:8233`) and Visualizer (`:${VISUALIZER_PORT}`) are reachable, then run `docker compose down`.

### Implementation for User Story 1

- [X] T007 [US1] Add workflow-visualizer service definition to src/docker-compose.yml with build context ./web/workflow-react-flow-visualizer-mvp and network temporal-local
- [X] T008 [US1] Configure workflow-visualizer port mapping in src/docker-compose.yml as ${VISUALIZER_PORT:-3000}:80
- [X] T009 [US1] Add workflow-visualizer healthcheck in src/docker-compose.yml using wget --spider -q http://localhost:80 without depends_on temporal
- [X] T010 [US1] Update stack run/stop validation steps for five services in specs/002-visualizer-docker-compose/quickstart.md

**Checkpoint**: US1 is independently functional and demoable.

---

## Phase 4: User Story 2 - Use compose file at src/docker-compose.yml (Priority: P2)

**Goal**: The repository uses `src/docker-compose.yml` as the single authoritative compose entrypoint.  
**Independent Test**: Confirm `src/docker-compose.yml` exists, `src/temporal-local/docker-compose.yml` is removed, and existing Temporal services still start from `src/`.

### Implementation for User Story 2

- [X] T011 [US2] Remove legacy compose file at src/temporal-local/docker-compose.yml after root compose migration is complete
- [X] T012 [US2] Update quick start path and compose location notes in src/temporal-local/README.md from `cd src/temporal-local` to `cd src`
- [X] T013 [US2] Update compose command examples in src/temporal-local/README.md to match root compose usage in src/docker-compose.yml

**Checkpoint**: US2 path migration is complete and documentation matches reality.

---

## Phase 5: User Story 3 - Build and start visualizer automatically (Priority: P3)

**Goal**: Visualizer is container-built and served as static production assets (no host npm/dev-server steps).  
**Independent Test**: Run `docker compose up --build` from `src/`; verify workflow-visualizer image is built, container is healthy, and app responds on configured host port.

### Implementation for User Story 3

- [X] T014 [US3] Create multi-stage Dockerfile in src/web/workflow-react-flow-visualizer-mvp/Dockerfile using node:22-alpine build stage and nginx:1.27-alpine serve stage
- [X] T015 [US3] Add nginx SPA serving configuration in src/web/workflow-react-flow-visualizer-mvp/Dockerfile with try_files fallback and no proxy_pass
- [X] T016 [US3] Wire visualizer build details in src/docker-compose.yml (dockerfile: Dockerfile, container port 80, build context ./web/workflow-react-flow-visualizer-mvp)
- [X] T017 [P] [US3] Document VISUALIZER_PORT constraints and conflict guidance in src/.env.example (must not collide with 7233/8233)

**Checkpoint**: US3 visualizer containerization is complete and independently testable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final consistency, docs alignment, and acceptance polish across stories.

- [X] T018 [P] Re-run and refine end-to-end validation instructions in specs/002-visualizer-docker-compose/quickstart.md for current final behavior
- [X] T019 Add troubleshooting notes (port conflict, visualizer build failure) to src/temporal-local/README.md
- [X] T020 [P] Align environment contract details in specs/002-visualizer-docker-compose/contracts/env-variables.md with final src/.env.example values

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 → Phase 2 → User Story phases (3–5) → Phase 6

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2
- **US2 (P2)**: Starts after Phase 2 (recommended after US1 migration baseline is stable)
- **US3 (P3)**: Starts after Phase 2; depends on US1 visualizer service entry in src/docker-compose.yml

### Task Dependency Highlights

- T004 blocks T005–T009, T011–T013, T016
- T007 blocks T008/T009/T016
- T014 and T015 block final visualizer runtime validation (T018)

### Suggested Story Completion Order

1. US1 (MVP)
2. US2
3. US3

---

## Parallel Opportunities

- **Setup**: T001 and T002 can run in parallel
- **US3**: T014 and T017 can run in parallel (different files)
- **Polish**: T018 and T020 can run in parallel (different files)

### Parallel Example: US3

```bash
# In parallel:
Task T014: Create src/web/workflow-react-flow-visualizer-mvp/Dockerfile
Task T017: Update VISUALIZER_PORT guidance in src/.env.example
```

---

## Implementation Strategy

### MVP First (US1 only)

1. Complete Phase 1 and Phase 2
2. Complete US1 tasks (T007–T010)
3. Validate full-stack one-command startup/shutdown from `src/`
4. Demo MVP

### Incremental Delivery

1. Deliver US1 (single-command stack startup)
2. Deliver US2 (path migration cleanup + docs accuracy)
3. Deliver US3 (production-grade visualizer container build/serve)
4. Finish Polish phase for final consistency

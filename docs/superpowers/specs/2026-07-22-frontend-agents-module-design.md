# Design: Frontend Agents Module (list, search, detail)

**Date:** 2026-07-22  
**Status:** Approved for implementation planning  
**Scope:** Frontend — `frontend/src/modules/agents/` list + search + detail against existing `/api/agents` endpoints

## Goal

Give users a UI to search agents and open a detail view, using the existing agents HTTP API. All feature code lives under `frontend/src/modules/agents/`; the app shell stays thin and registry-driven.

## Decisions

| Topic | Choice |
|-------|--------|
| Scope | List + search + detail (no chat / create / approvals) |
| Routes | `/module/agents` (list), `/module/agents/:id` (detail) |
| Code location | `frontend/src/modules/agents/` |
| Registry | Hand-written `frontend/src/modules/index.ts` exports agents routes |
| Routing library | `react-router-dom` (`BrowserRouter`) |
| Search UX | Three filters: name, department, job title; Search submit |
| Empty filters | Empty state; **no** API call |
| Search API | `GET /api/agents/search?name=&department=&jobTitle=` (exact match, as today) |
| Detail API | `GET /api/agents/{id}` |
| List-all API | Not used in v1 (`GET /api/agents` unused by this UI) |
| Default entry | `/` redirects to `/module/agents` |
| Data fetching | Thin `fetch` helpers in `api.ts` (no React Query) |
| Styling | Simple module CSS; functional, not a design-system overhaul |
| Tests | Vitest + Testing Library under `src/__tests__/` |

## Architecture & layout

```
frontend/src/
  modules/
    index.ts                 # registry: exports agents module routes
    agents/
      routes.tsx             # Route objects for /module/agents and /module/agents/:id
      api.ts                 # searchAgents, getAgent
      types.ts               # AgentDto, page/detail response types
      AgentsListPage.tsx
      AgentDetailPage.tsx
      agents.css             # module styles
  App.tsx                    # BrowserRouter + registry routes + / → /module/agents
  main.tsx
```

**Boundaries**

- Agent UI and API client code must not leak into shared shell beyond route registration.
- Relative `/api/...` URLs only (Vite proxy / same-origin host already configured).
- Backend search semantics stay exact-match; no backend changes in this work.

## Components & data flow

### Shell (`App.tsx`)

- Mounts `BrowserRouter`.
- Renders routes from `modules/index.ts`.
- Redirects `/` → `/module/agents`.
- No agent-specific markup in the shell.

### `api.ts`

- `searchAgents({ name?, department?, jobTitle? })` → `GET /api/agents/search` with only non-empty query params.
- `getAgent(id)` → `GET /api/agents/{id}`.
- On non-OK response: throw an error that includes HTTP status (callers map 404 vs other errors).

### `AgentsListPage`

- Form fields: Name, Department, Job title + Search (submit via button or Enter).
- All filters empty on submit → clear results; show empty-state copy (“Enter at least one filter”); do not fetch.
- At least one filter set → call `searchAgents`; render results (AgentId, Name, Department, JobTitle).
- Each result links to `/module/agents/:id`.
- UI states: idle empty, loading, results, zero matches, error (inline).

### `AgentDetailPage`

- Reads `:id` from the route (the list item’s `agentId` / agent id); loads via `getAgent(id)`.
- Displays Name, Department, JobTitle, AgentId, SystemPrompt as returned by the API (no client-side remapping).
- Back link to `/module/agents`.
- UI states: loading, not found (404), error (inline), success.

## API contract (existing)

| Method | Path | Notes |
|--------|------|--------|
| GET | `/api/agents/search` | Query: `name`, `department`, `jobTitle` (optional, exact match) → `GetAgentsResponse` / `PageDto<AgentDto>` |
| GET | `/api/agents/{agentId}` | → `GetAgentByIdResponse` (includes `systemPrompt`) |

`AgentDto`: `agentId`, `name`, `department`, `jobTitle` (JSON camelCase).

## Testing

Under `frontend/src/__tests__/`:

1. **AgentsListPage** — empty filters → empty state and no `fetch`; with filters → mocked search response shows rows; row links target `/module/agents/:id`.
2. **AgentDetailPage** — mocked `getAgent` shows fields; 404 → not-found message.
3. **App shell** — replace the Vite starter counter test with coverage that the router mounts and `/` redirects to `/module/agents`.

Mock `fetch` or the `api.ts` module; no live backend required for unit tests.

## Dependencies

- Add `react-router-dom` (and `@types/react-router-dom` only if the package does not ship its own types).

## Out of scope

- Backend search improvements (substring / fuzzy / case-insensitive partial match beyond current service)
- Using `GET /api/agents` (list-all) in the UI
- Chat, HITL approvals, create-identity from this UI
- Auth
- TanStack Query / global client state
- Full visual redesign of the product shell
- Auto-discovery of modules (registry stays hand-written)

## Verification (manual)

1. Backend running on `5296`; `npm run dev` in `frontend/`.
2. Open `/module/agents` — empty state without searching.
3. Search with a known name (e.g. seeded agent) — results appear; open detail — system prompt visible.
4. Unknown id — not-found state.
5. `npm test` passes.

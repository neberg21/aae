# Leo park-delegation + HR hire — Design

**Date:** 2026-07-22  
**Status:** Approved for implementation planning  
**Deliverable:** Leo n8n think-workflow checks supervisor existence before routing; missing supervisors are parked in the backend and Helga is woken to create them; after `create-identity`, parked work is auto-routed

## Goal

When Leo delegates to a `supervisor-*` agent:

1. Check whether that supervisor already exists
2. If yes → `route-chat-message` as today
3. If no → park the delegation in the backend, then mechanically open an HR request to Helga
4. When Helga’s `create-identity` succeeds for that logical `agentId`, the backend resumes parked delegations to the new supervisor

n8n stays stateless (no Wait nodes). Backend owns park/resume.

## Decisions (locked)

| Topic | Choice |
|-------|--------|
| Park ownership | Backend pending-delegation store; auto-resume after create |
| Existence check | n8n `GET /api/agents/search?agentId={targetAgentId}` before route |
| Missing path | Mechanical: park + `route-chat-message` to `helga` (Leo model need not emit separate `helga` items for missing supervisors) |
| Match key | Intended `targetAgentId` / agent `Id` (e.g. `supervisor-finance`) |
| Scope now | Leo only; design park API for later Supervisor reuse |
| Approach | Explicit `park-delegation` API + resume inside `create-identity` |
| Agent id on create | Required `agentId` on `CreateIdentityRequest`; persisted as `Agent.Id` (replaces JobTitle/Department-only derivation for non-core agents) |
| Helga HR content | Structured JSON string inside `route-chat-message` `content` |
| Duplicate create | HTTP `409 Conflict` |

## Context (current code)

- Leo (`leo-think.json`) parses delegations and immediately routes each item; no existence check
- Search already supports `?agentId=` (`SearchIdentityService`)
- `Agent.Id` is computed: `leo` / `helga` by name; else `{JobTitle}-{Department}` (lowercased, spaces → `-`)
- `CreateIdentityRequest` has no `agentId` yet; `CreateIdentityResponse.AgentId` returns computed `Id`
- No park/resume API exists today

## Architecture

```text
Leo n8n (after AI parse)
  for each supervisor-* delegation:
    GET search?agentId=...
      found  → POST route-chat-message → supervisor
      missing → POST park-delegation
               POST route-chat-message → helga (hr_request with intended agentId)

Helga n8n
  needs_clarification → User (parks untouched)
  ready → POST create-identity (includes agentId)

Backend create-identity
  persist identity with agentId as Id
  flush parks where targetAgentId == agentId
  for each park → route-chat-message to new supervisor
```

## Leo n8n flow

**Input:** unchanged (`threadId`, `chatHistory`, `userVision`)

After **Parse Delegations** succeeds:

1. Split items (or loop per item)
2. For each item:
   - If `targetAgentId` is `helga` (explicit HR): route as today (no park)
   - If `targetAgentId` starts with `supervisor-`:
     - `GET https://ai.neberg.de/api/agents/search?agentId={targetAgentId}`
     - If `items.length > 0`: `POST route-chat-message` to that supervisor
     - If empty: `POST park-delegation`, then `POST route-chat-message` to `helga` with HR content derived from the parked item
3. Parse failures: failure route to `User` (unchanged)
4. End (fire-and-forget)

### Mechanical Helga wake content

Leo sets `route-chat-message` `content` to a JSON string:

```json
{
  "intent": "hr_request",
  "agentId": "supervisor-finance",
  "role": "supervisor",
  "moduleScope": "Module.Finance",
  "message": "<original Leo delegation text>"
}
```

`agentId` must equal the parked `targetAgentId`. Helga prompt/parse extracts these fields for create.

## Backend contracts

### Existence (existing)

`GET /api/agents/search?agentId=supervisor-finance` → empty page means missing.

### Park (new)

`POST /api/agents/park-delegation`

```json
{
  "threadId": "string",
  "senderAgentId": "leo",
  "targetAgentId": "supervisor-finance",
  "content": "string"
}
```

- Persist pending row keyed primarily by `targetAgentId`
- Multiple parks for the same target allowed (FIFO resume)
- Response: `200` with ack (e.g. `{ "ok": true }`); no agent wake

### Create + resume (extend)

Extend `CreateIdentityRequest` with required `agentId` (camelCase). Persist an explicit agent id so `Agent.Id` equals that value for the created identity (core `leo`/`helga` keep their fixed ids).

On successful create:

1. Save identity
2. Load parked rows where `targetAgentId` equals new `agentId` (case-insensitive)
3. For each (FIFO): invoke the same routing path as `route-chat-message` with parked `threadId`, `senderAgentId`, `targetAgentId`, `content`
4. Mark parked rows delivered/deleted

If Helga returns `needs_clarification`, parks stay until a later successful create.

### Conflict

If `agentId` already exists on create → `409 Conflict`; parks unchanged.

## Helga changes

- Prompt/schema: honor intended `agentId` from Leo’s HR payload; emit it into create body
- `mapHelgaIdentityToCreateRequest` / helga-think Code: pass `agentId` through to `CreateIdentityRequest`
- Helga does **not** re-send parked Leo work; backend resume owns that

## Error handling

| Case | Behavior |
|------|----------|
| Invalid Leo JSON | Route failure to `User` (today) |
| Search / park HTTP failure | n8n default error handling; no in-graph Wait/retry |
| Create with no matching parks | Identity created; no resume (ok) |
| Create duplicate `agentId` | `409 Conflict`; parks unchanged |
| Helga clarify | Parks untouched |

## Testing

- Helpers/unit: exists → single route body; missing → park body + helga hr_request body
- Backend: park → create matching `agentId` → supervisor receives parked content; clarify leaves park
- Manual VERIFY: vision needing a new supervisor; observe search miss → park → Helga → create → supervisor wake with original content

## Delivery artifacts

| Area | Change |
|------|--------|
| `agents/n8n-workflows/leo-think.json` | Existence branch + park + mechanical Helga route |
| `agents/n8n-workflows/helga-think.json` / helpers | Pass `agentId` on create |
| `agents/n8n-workflows/think-helpers/` | Shared parse/build helpers + tests |
| Backend Module.Agents | `park-delegation`, park store, create resume, `agentId` on create |
| `agents/workflow.md`, `VERIFY.md` | Document new endpoints and Leo branch |

## Out of scope

- Supervisor nested missing-agent path (same park API later)
- n8n Wait nodes / long-running Leo execution
- Auth between n8n and backend
- Rewriting Leo/Helga identity markdown beyond what create `agentId` requires

## References

- Think workflows: `docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md`
- Runtime contracts: `agents/workflow.md`
- Create supervisor process: `docs/process/create-supervisor.md`
- Search / create: `backend/src/Module.Agents/AgentsModule.cs`

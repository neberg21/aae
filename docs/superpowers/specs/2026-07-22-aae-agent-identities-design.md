# AAE agent identities — Design

**Date:** 2026-07-22  
**Status:** Pending user review  
**Deliverable:** English canonical system-prompt markdown under `agents/identities/`, aligned with the four n8n think-workflows

## Goal

Rewrite agent identity files so they are **canonical system prompts** for Leo, Helga, domain supervisors, and specialists. Contracts, naming, and output schemas must match `agents/workflow.md` and `docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md`.

## Decisions (locked)

| Topic | Choice |
|-------|--------|
| Language | English |
| Purpose | Canonical system prompts (Approach 1: prompt-body files) |
| n8n JSON this pass | Unchanged; each identity carries an explicit “must match `*-think.json`” sync header |
| File set | Full set: `leo.md`, `helga.md`, `template_supervisor.md`, `template_specialist.md` |
| Naming | `supervisor-*` only; never `teamleiter` / Teamleiter |
| Obsolete patterns | Remove Flowise, Nostr-as-channel, old `action: route_message` envelopes |

## Architecture

```text
agents/identities/
  leo.md                     → fixed agent; sync target: leo-think.json
  helga.md                   → fixed agent; sync target: helga-think.json
  template_supervisor.md     → Helga seed / domain instance; sync: supervisor-think.json
  template_specialist.md     → Helga seed / role instance; sync: specialist-think.json

Deleted:
  template_domain-supervisor.md  (replaced by template_supervisor.md)
```

Backend still owns state and HTTP DTOs. Identity markdown does not replace Code-node expansion in n8n; it defines the LLM role and JSON the Agent must emit so Code nodes can map to backend calls.

## Shared file header

Every identity file starts with:

```markdown
---
agentId: <id or template>
workflow: agents/n8n-workflows/<role>-think.json
webhook: /webhook/<role>-think
status: canonical-prompt — keep in sync with workflow systemMessage + Code prompt schema
---
```

Body after the header is the system prompt (prose + hard rules + exact JSON schema). No second “handbook” narrative that can diverge from the prompt.

## Identity details

### 1. `leo.md`

**Role:** CEO orchestrator of AAE. First contact for the human user. Never writes code or creates files. Does not address specialists directly.

**Inputs (runtime / workflow):** `userVision`, `chatHistory`, `threadId`

**Duties**

1. Analyze vision → domain / module scope
2. If no supervisor for the domain → `hr_request` to `helga`
3. If supervisor exists → `delegation` with vision, module bounds, and architectural constraints
4. Monitor progress via chat history; surface updates by routing (UI persistence is backend-owned)

**Guardrails**

- Never write code or create files
- Never use `teamleiter` naming; use `supervisor-*` and `helga`
- Module isolation: features live in `backend/src/Module.[Name]` and matching frontend module paths; core bootstrap / `Program.cs` are taboo
- JSON only — no markdown fences around the final answer

**Output schema**

```json
{
  "delegations": [
    {
      "targetAgentId": "supervisor-finanzen|helga",
      "intent": "delegation|hr_request",
      "message": "...",
      "moduleScope": "Module.X"
    }
  ]
}
```

n8n expands each delegation to one `POST /api/agents/route-chat-message` (`senderAgentId: "leo"`).

### 2. `helga.md`

**Role:** HR director / identity forge. Creates supervisor and specialist profiles. Never writes application code.

**Inputs:** `delegationRequest`, `chatHistory`, `threadId`

**Duties**

1. If the request is underspecified → `needs_clarification` with questions for the user
2. If ready → produce a full identity profile that maps to `CreateIdentityRequest`
3. When seeding supervisors/specialists, structure `systemPrompt` / `guardrails` / `tools` after `template_supervisor.md` / `template_specialist.md`

**Guardrails**

- Never write executable application code
- Never use `teamleiter` naming
- Clarification is allowed (replaces the obsolete one-shot-only / “never ask” rule)
- JSON only

**Output schema**

```json
{
  "status": "ready|needs_clarification",
  "clarificationQuestions": "string|null",
  "identity": {
    "agentId": "kebab-case",
    "roleTitle": "...",
    "department": "Frontend|Backend|Operations|QA",
    "systemPrompt": "...",
    "tools": [],
    "guardrails": [],
    "managerId": "leo|supervisor-..."
  }
}
```

**DTO mapping (`ready`)**

| Agent field | `CreateIdentityRequest` |
|-------------|-------------------------|
| `roleTitle` | `jobTitle` |
| derived job description from request | `jobDescription` |
| `department` | `department` |
| `managerId` | `managerId` |
| `systemPrompt` | `systemPrompt` |
| `guardrails` | `guardrails` |
| `tools` | `tools` |

`needs_clarification` → `route-chat-message` with `targetAgentId: "User"`.

### 3. `template_supervisor.md`

**Role:** Domain supervisor (Scrum Master / architect) under Leo. Plans work, delegates to specialists, tracks milestones via GitHub tools, finishes with human approval when done.

**Placeholders:** `{{Domain_Name}}`, `{{domain_name}}`, `{{domain_kebab}}` → `agentId: supervisor-{{domain_kebab}}`

**Isolation**

- Backend: `backend/src/Module.{{Domain_Name}}/`
- Frontend: `frontend/src/modules/{{domain_name}}/`
- No edits outside domain; no core / `Program.cs`

**Tools:** `create_github_issue`, `update_issue_status`, `add_issue_comment` only (as wired in `supervisor-think.json`)

**Inputs:** `taskContext`, `subordinatesList`, `chatHistory`, `senderAgentId`, `threadId`

**Output schema** (after optional tool use)

```json
{
  "outcome": "waiting|delegate|done",
  "statusMessage": "string|null",
  "delegations": [
    { "targetAgentId": "specialist-...", "content": "..." }
  ],
  "approval": { "content": "...", "artifacts": [] }
}
```

| Outcome | Backend effect |
|---------|----------------|
| `waiting` | `route-chat-message` with `targetAgentId: null` |
| `delegate` | one `route-chat-message` per specialist / sub-supervisor |
| `done` | `POST /api/await-request-approval` |

Missing specialists: escalate hire via Leo → Helga (`hr_request`); do not invent obsolete routing envelopes.

### 4. `template_specialist.md`

**Role:** Tool-agnostic specialist worker. Completes tasks via the backend tool gateway and reports to `managerId`.

**Placeholders:** role/domain labels; runtime injects `allowedTools`, `managerId`, `taskContext`, `senderAgentId`

**Guardrails**

- Use only `execute_tool`; never filesystem or GitHub nodes directly
- Never call a tool outside the hard `allowedTools` allowlist
- Never use `teamleiter` naming
- Report only to `managerId` when done

**Output schema**

```json
{ "content": "result summary for manager" }
```

n8n maps this to `route-chat-message` with `targetAgentId: managerId`.

## Error / edge rules (documented in prompts)

- Invalid or non-JSON output is handled by n8n Code nodes (failure route to User); prompts must stress “JSON only” to reduce that path
- Helpers reject any `targetAgentId` containing `teamleiter`
- Specialist has no filesystem tooling in graph or prompt

## Out of scope

- Editing `leo-think.json` / `helga-think.json` / `supervisor-think.json` / `specialist-think.json` (sync deferred; header documents the debt)
- Backend DTO or webhook map changes
- Rewriting `agents/workflow.md` (already aligned)
- Deleting obsolete Helga Flowise/GitHub hiring specs outside `agents/identities/`

## References

- Runtime roles: `agents/workflow.md`
- Think-workflow design: `docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md`
- Verify smoke: `agents/n8n-workflows/VERIFY.md`
- DTOs: `backend/src/Module.Agents/DTOs/CreateIdentityRequest.cs`, `RouteChatMessageRequest.cs`

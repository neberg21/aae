# AAE n8n think-workflows — Design

**Date:** 2026-07-22  
**Status:** Approved for implementation planning  
**Deliverable:** Four importable, stateless n8n workflows (LangChain Advanced AI) that execute agent turns and callback into the .NET backend

## Goal

Provide the execution layer for the Autonomous Agent Ecosystem (AAE):

1. Backend owns brain, state, chat history, and fan-out/fan-in concurrency
2. n8n owns one-shot execution only (webhook in → work → HTTP POST out → end)
3. No Wait nodes and no long-running workflows
4. Agent logic uses n8n Advanced AI (LangChain) nodes with OpenAI Chat Model

## Decisions (locked)

| Topic | Choice |
|-------|--------|
| Shape | Approach 1: full AI Agent on all four roles |
| Backend base URL | Hardcoded `https://ai.neberg.de` |
| JSON casing | camelCase on all bodies to/from backend |
| LLM | OpenAI Chat Model (credential attached after import) |
| Naming | Use `supervisor` everywhere; no `teamleiter` / `ceo-think` |
| Leo fan-out | One `route-chat-message` HTTP call per target (task-specific `content`) |
| Helga clarification | Open questions via `RouteChatMessageRequest` (`targetAgentId: "User"`); ready → `create-identity` |
| Specialist tools | Single generic `execute_tool` HTTP tool; backend enforces `allowedTools` |
| Route target field | Singular `targetAgentId` (matches current DTO) |

## Architecture

```text
.NET Backend (https://ai.neberg.de)
  owns: threads, history, organigram, approvals, tool allowlists
  wakes: n8n webhooks fire-and-forget with full context injection

n8n (stateless)
  leo-think        → plan + route to supervisors / helga
  helga-think      → clarify to User OR create-identity
  supervisor-think → waiting | delegate specialists | await-request-approval
  specialist-think → execute_tool* → report to managerId
```

**Principles**

- Zustandslosigkeit: n8n never stores chat history as source of truth
- Fire-and-forget: every path ends with HTTP callback(s) to the backend
- Isolation: Specialist has no filesystem/GitHub nodes; only `execute_tool`

## Shared contracts

### Outbound HTTP

| Purpose | Method + URL | Body |
|---------|--------------|------|
| Route message | `POST https://ai.neberg.de/api/agents/route-chat-message` | `RouteChatMessageRequest` |
| Create identity | `POST https://ai.neberg.de/api/agents/create-identity` | `CreateIdentityRequest` |
| Approval gate | `POST https://ai.neberg.de/api/await-request-approval` | `{ "threadId", "senderAgentId", "content", "artifacts": [] }` until backend finalizes the DTO |
| Execute tool | `POST https://ai.neberg.de/api/agents/execute-tool` | `{ threadId, agentId, tool, args }` (backend follow-up) |

### `RouteChatMessageRequest` (camelCase)

```json
{
  "threadId": "string",
  "senderAgentId": "string",
  "targetAgentId": "string|null",
  "content": "string"
}
```

Semantics:

- `targetAgentId` = agent id → backend wakes that agent’s webhook
- `targetAgentId` = `"User"` or `null` → persist + UI only; no agent wake

### `CreateIdentityRequest` (camelCase)

```json
{
  "jobTitle": "string",
  "jobDescription": "string",
  "department": "string",
  "managerId": "string|null",
  "systemPrompt": "string",
  "guardrails": ["string"],
  "tools": ["string"]
}
```

### Inbound webhooks

| Workflow | Path |
|----------|------|
| Leo | `POST /webhook/leo-think` |
| Helga | `POST /webhook/helga-think` |
| Supervisor | `POST /webhook/supervisor-think` |
| Specialist | `POST /webhook/specialist-think` |

### Common node skeleton

`Webhook` → `Set` (normalize payload) → `AI Agent` (+ OpenAI Chat Model) → `Code` (parse/validate) → `Switch` (when needed) → `HTTP Request`(s) → end

## Workflow details

### 1. Leo (`leo-think.json`)

**Input:** `threadId`, `chatHistory`, `userVision`

**Flow**

1. Webhook receives vision + history
2. Set builds Agent prompt from `userVision` and `chatHistory`
3. AI Agent decomposes vision into department-level work packages (JSON only)
4. Code expands to one outbound item per target
5. Split / item loop → one HTTP `route-chat-message` per item
6. End

**Agent output (before Code expansion)**

```json
{
  "delegations": [
    {
      "targetAgentId": "supervisor-finanzen",
      "intent": "delegation",
      "message": "...",
      "moduleScope": "Module.Finanzen"
    }
  ]
}
```

If a domain has no supervisor, Leo may include a delegation to `helga` with `intent: "hr_request"` (still one HTTP call per target).

**HTTP body per item**

```json
{
  "threadId": "...",
  "senderAgentId": "leo",
  "targetAgentId": "supervisor-finanzen",
  "content": "<task-specific delegation text>"
}
```

### 2. Helga (`helga-think.json`)

**Input:** `threadId`, `chatHistory`, `delegationRequest`

**Flow**

1. Webhook → Set → AI Agent produces status + identity profile
2. Code validates and maps to backend DTOs
3. Switch on `status`:
   - `needs_clarification` → `route-chat-message` with `targetAgentId: "User"` and open questions as `content`
   - `ready` → `create-identity` with full `CreateIdentityRequest`
4. End

**Agent output**

```json
{
  "status": "ready",
  "clarificationQuestions": null,
  "identity": {
    "agentId": "kebab-case",
    "roleTitle": "...",
    "department": "Frontend|Backend|Operations|QA",
    "systemPrompt": "...",
    "tools": ["..."],
    "guardrails": ["..."],
    "managerId": "leo|supervisor-..."
  }
}
```

**Mapping to `CreateIdentityRequest`**

| Agent field | DTO field |
|-------------|-----------|
| `roleTitle` | `jobTitle` |
| summary from request + department | `jobDescription` |
| `department` | `department` |
| `managerId` | `managerId` |
| `systemPrompt` | `systemPrompt` |
| `guardrails` | `guardrails` |
| `tools` | `tools` |

Clarification path remains in n8n for now; future clarification semantics may move fully into the .NET backend while still using `RouteChatMessageRequest` for user-facing questions.

### 3. Supervisor (`supervisor-think.json`)

**Input:** `threadId`, `chatHistory`, `taskContext`, `subordinatesList`

**Tools (AI Agent)**

- `create_github_issue`
- `update_issue_status`
- `add_issue_comment`

GitHub HTTP Request tools document milestones as a lightweight state machine. GitHub credential attached after import.

**Flow**

1. Webhook → Set (task + subordinates into prompt)
2. AI Agent plans / may call GitHub tools
3. Code normalizes decision JSON
4. Switch on `outcome`:
   - `waiting` → `route-chat-message` with `targetAgentId: null` (UI status / fan-in)
   - `delegate` → one `route-chat-message` per specialist or sub-supervisor
   - `done` → `POST /api/await-request-approval`
5. End

**Decision JSON**

```json
{
  "outcome": "delegate",
  "statusMessage": null,
  "delegations": [
    { "targetAgentId": "specialist-react", "content": "..." }
  ],
  "approval": null
}
```

### 4. Specialist (`specialist-think.json`)

**Input:** `threadId`, `chatHistory`, `taskContext`, `allowedTools`, `managerId`, `senderAgentId`

**Flow**

1. Webhook → Set injects task + hard allowlist text from `allowedTools`
2. AI Agent with single tool `execute_tool` → HTTP POST tool gateway
3. Optional UI status via `route-chat-message` (`targetAgentId: null`) only if Agent emits status
4. Done → `route-chat-message` to `managerId` with result summary
5. End

**`execute_tool` body**

```json
{
  "threadId": "...",
  "agentId": "<specialist id>",
  "tool": "GenerateCode",
  "args": {}
}
```

Backend rejects tools not in `allowedTools`. Specialist graph is not hardcoded to the filesystem.

## Error handling

- Invalid / non-JSON Agent output: Code node sends one `route-chat-message` to `User` or `null` with a short failure note; workflow ends
- Backend HTTP failures: n8n default error handling; no in-graph Wait / compensation loops
- Disallowed tool on Specialist: backend rejects; Agent may choose another tool or report failure to `managerId`

## Testing (manual after import)

1. Attach OpenAI credential (all four); attach GitHub credential (Supervisor)
2. Activate workflows
3. POST sample payloads to each `*-think` webhook
4. Verify outbound camelCase bodies against `https://ai.neberg.de/api/agents/...`

## Delivery artifacts

| File | Webhook |
|------|---------|
| `agents/n8n-workflows/leo-think.json` | `/webhook/leo-think` |
| `agents/n8n-workflows/helga-think.json` | `/webhook/helga-think` |
| `agents/n8n-workflows/supervisor-think.json` | `/webhook/supervisor-think` |
| `agents/n8n-workflows/specialist-think.json` | `/webhook/specialist-think` |

## Out of scope

- Implementing `/api/agents/execute-tool` and `await-request-approval` in .NET
- Updating `RouteChatMessageService` webhook URL map to `*-think` paths
- Rewriting identity markdown (`helga.md`, `leo.md`) or deleting obsolete Helga Flowise/GitHub hiring flow
- Auth headers between n8n and backend (none assumed for this slice)

## References

- Runtime roles: `agents/workflow.md`
- Backend DTOs: `backend/src/Module.Agents/DTOs/CreateIdentityRequest.cs`, `RouteChatMessageRequest.cs`
- n8n host notes: `infrastructure/n8n/README.md`

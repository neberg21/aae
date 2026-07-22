## 1. The C# .NET Backend Endpoints (Hub & Router)

The backend owns state, concurrency (fan-out/fan-in), and UI status updates.

| HTTP Method & Route | Sender | Description |
| --- | --- | --- |
| **GET** `/api/agents/search?name=&department=&jobTitle=` | n8n (Leo/Helga) | Find identities. Leo/Helga use `department=Core&name=leo\|helga` and take `items[0].identityId`. |
| **GET** `/api/agents/{identityId}` | n8n (Leo/Helga) | Returns identity including `systemPrompt` for the AI Agent `systemMessage`. |
| **POST** `/api/agents/route-chat-message` | n8n & UI | Multi-router. Body (camelCase): `{ threadId, senderAgentId, targetAgentId, content }`. `targetAgentId` = agent id wakes that n8n webhook; `null` or `"User"` = persist + UI only. |
| **POST** `/api/agents/create-identity` | n8n (Helga) | Creates identity from Helga profile: `jobTitle`, `jobDescription`, `department`, `managerId`, `systemPrompt`, `guardrails`, `tools`. |
| **POST** `/api/agents/await-request-approval` | n8n (Supervisor) | Approval gate: pushes drafts to the web app and waits for human resolve (handler stubbed). |
| **POST** `/api/agents/resolve-request-approval` | UI | Continues the flow after Approve / Reject (handler stubbed). |
| **POST** `/api/agents/execute-tool` | n8n (Specialist) | Tool gateway; enforces `allowedTools` (backend follow-up). |

---

## 2. n8n Workflows & Webhooks (Execution Layer)

Exactly **four** workflows. Stateless, no Wait nodes. Backend injects full context on wake.

Import from `agents/n8n-workflows/`. Verify: [`VERIFY.md`](n8n-workflows/VERIFY.md). Design: [`docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md`](../docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md).

### A. Leo (`leo-think.json`)

* **Webhook:** `POST /webhook/leo-think`
* **Input:** `threadId`, `chatHistory`, `userVision`
* **System prompt:** search Core agent `name=leo` → get-by-id → bind `systemPrompt` as Agent `systemMessage`
* **Flow:** AI Agent → Code expands one `route-chat-message` per supervisor (or Helga `hr_request`)

### B. Helga (`helga-think.json`)

* **Webhook:** `POST /webhook/helga-think`
* **Input:** `threadId`, `chatHistory`, `delegationRequest`
* **System prompt:** search Core agent `name=helga` → get-by-id → bind `systemPrompt` as Agent `systemMessage`
* **Flow:** AI Agent → Switch
  * `needs_clarification` → `route-chat-message` (`targetAgentId: "User"`)
  * `ready` → `create-identity`

### C. Supervisor (`supervisor-think.json`)

* **Webhook:** `POST /webhook/supervisor-think`
* **Input:** `threadId`, `chatHistory`, `taskContext`, `subordinatesList`, `senderAgentId`
* **Tools:** `create_github_issue`, `update_issue_status`, `add_issue_comment`
* **Flow:** AI Agent → waiting (`targetAgentId: null`) | delegate (specialists and/or nested supervisors) | done → `/api/agents/await-request-approval`

### D. Specialist (`specialist-think.json`)

* **Webhook:** `POST /webhook/specialist-think`
* **Input:** `threadId`, `chatHistory`, `taskContext`, `allowedTools`, `managerId`, `senderAgentId`
* **Tools:** single `execute_tool` HTTP tool (backend-enforced allowlist)
* **Flow:** AI Agent → `route-chat-message` to `managerId`

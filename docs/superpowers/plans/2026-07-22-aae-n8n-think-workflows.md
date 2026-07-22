# AAE n8n Think-Workflows Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship four importable, stateless n8n workflows (`leo-think`, `helga-think`, `supervisor-think`, `specialist-think`) that run LangChain AI Agents and callback into `https://ai.neberg.de` with camelCase DTOs.

**Architecture:** Pure JS helpers (unit-tested) own webhook normalization, Agent JSON parsing, DTO mapping, and Switch routing decisions. Each workflow JSON wires `Webhook → Set → AI Agent (+ OpenAI Chat Model) → Code → Switch/Split → HTTP Request(s)` with no Wait nodes. Specialist uses one `execute_tool` HTTP tool; Supervisor uses three GitHub HTTP tools. Backend remains source of truth for state/history.

**Tech Stack:** n8n workflow JSON (LangChain `@n8n/n8n-nodes-langchain.agent` typeVersion `2.2` or host-supported Tools Agent), OpenAI Chat Model node, Node.js `node:test` for helpers, HTTP Request nodes to .NET API

**Spec:** [`docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md`](../specs/2026-07-22-aae-n8n-think-workflows-design.md)

## Global Constraints

- Windows scripting: no PowerShell/bash scripts; use `cmd /c` for one-offs; commits via message file if multi-line
- Backend base URL hardcoded: `https://ai.neberg.de`
- All outbound JSON bodies: camelCase (`threadId`, `senderAgentId`, `targetAgentId`, `content`, …)
- Naming: `supervisor` only — never `teamleiter` / `ceo-think`
- No Wait nodes; fire-and-forget only
- Do not commit OpenAI/GitHub secrets into workflow JSON (leave credentials empty / placeholder names)
- Response language / commit messages: English
- C#: no primary constructors; do not inline object creation into method calls (if any backend touch is needed — none required in this plan)
- Out of scope: implementing `/api/agents/execute-tool` and `await-request-approval` in .NET (workflows still target those URLs)

---

## File Structure

| File | Responsibility |
|------|----------------|
| `agents/n8n-workflows/think-helpers/package.json` | Local `node --test` runner for shared helpers |
| `agents/n8n-workflows/think-helpers/helpers.mjs` | Pure parse/map/route functions used by all four Code nodes |
| `agents/n8n-workflows/think-helpers/helpers.test.mjs` | Unit tests for helpers |
| `agents/n8n-workflows/leo-think.json` | Importable Leo orchestrator workflow |
| `agents/n8n-workflows/helga-think.json` | Importable Helga HR workflow |
| `agents/n8n-workflows/supervisor-think.json` | Importable Supervisor workflow + GitHub tools |
| `agents/n8n-workflows/specialist-think.json` | Importable Specialist workflow + `execute_tool` |
| `agents/n8n-workflows/VERIFY.md` | Manual import / smoke-test checklist |
| `agents/workflow.md` | Align webhook paths and camelCase contracts with shipped workflows |
| `infrastructure/n8n/README.md` | Document the four think-workflow imports |

---

### Task 1: Shared think-helpers + unit tests

**Files:**
- Create: `agents/n8n-workflows/think-helpers/package.json`
- Create: `agents/n8n-workflows/think-helpers/helpers.mjs`
- Create: `agents/n8n-workflows/think-helpers/helpers.test.mjs`

**Interfaces:**
- Consumes: none (pure)
- Produces:
  - `BACKEND_BASE = "https://ai.neberg.de"`
  - `parseJsonMaybe(text) → unknown | null`
  - `normalizeWebhookBody(body) → { threadId, chatHistory, …rest }` (accepts camelCase or PascalCase)
  - `buildLeoPrompt({ userVision, chatHistory }) → string`
  - `parseLeoDelegations(agentOutput, threadId) → { ok, items: RouteBody[] } | { ok:false, error, userMessage }`
  - `parseHelgaDecision(agentOutput, threadId) → { ok, branch:"clarify"|"create", routeBody? , createBody? } | { ok:false, … }`
  - `mapHelgaIdentityToCreateRequest(identity, jobDescription) → CreateIdentityRequest`
  - `parseSupervisorDecision(agentOutput, threadId, senderAgentId) → { ok, outcome, httpCalls: Array<{ url, body }> }`
  - `buildSpecialistDoneBody({ threadId, senderAgentId, managerId, content }) → RouteBody`
  - `buildFailureRoute({ threadId, senderAgentId, content }) → RouteBody` with `targetAgentId: "User"`

Where `RouteBody` is:

```js
{
  threadId: string,
  senderAgentId: string,
  targetAgentId: string | null,
  content: string,
}
```

- [ ] **Step 1: Create package.json**

```json
{
  "name": "aae-think-helpers",
  "private": true,
  "type": "module",
  "scripts": {
    "test": "node --test helpers.test.mjs"
  }
}
```

- [ ] **Step 2: Write failing tests**

Create `helpers.test.mjs`:

```js
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  BACKEND_BASE,
  normalizeWebhookBody,
  parseLeoDelegations,
  parseHelgaDecision,
  mapHelgaIdentityToCreateRequest,
  parseSupervisorDecision,
  buildSpecialistDoneBody,
  buildFailureRoute,
} from './helpers.mjs';

describe('normalizeWebhookBody', () => {
  it('maps PascalCase backend fields to camelCase', () => {
    const n = normalizeWebhookBody({
      ThreadId: 't1',
      History: [{ role: 'user', content: 'hi' }],
      UserVision: 'build app',
    });
    assert.equal(n.threadId, 't1');
    assert.deepEqual(n.chatHistory, [{ role: 'user', content: 'hi' }]);
    assert.equal(n.userVision, 'build app');
  });
});

describe('parseLeoDelegations', () => {
  it('expands one HTTP body per delegation', () => {
    const r = parseLeoDelegations(
      JSON.stringify({
        delegations: [
          {
            targetAgentId: 'supervisor-finanzen',
            intent: 'delegation',
            message: 'Own Finanzen module',
            moduleScope: 'Module.Finanzen',
          },
          {
            targetAgentId: 'helga',
            intent: 'hr_request',
            message: 'Need supervisor-dnd',
            moduleScope: 'Module.Dnd',
          },
        ],
      }),
      'thread-1',
    );
    assert.equal(r.ok, true);
    assert.equal(r.items.length, 2);
    assert.equal(r.items[0].senderAgentId, 'leo');
    assert.equal(r.items[0].targetAgentId, 'supervisor-finanzen');
    assert.match(r.items[0].content, /Finanzen/);
    assert.equal(r.items[1].targetAgentId, 'helga');
  });

  it('rejects teamleiter naming', () => {
    const r = parseLeoDelegations(
      JSON.stringify({
        delegations: [{ targetAgentId: 'teamleiter-x', message: 'nope' }],
      }),
      't',
    );
    assert.equal(r.ok, false);
  });
});

describe('parseHelgaDecision', () => {
  it('maps ready identity to create-identity body', () => {
    const r = parseHelgaDecision(
      JSON.stringify({
        status: 'ready',
        clarificationQuestions: null,
        identity: {
          agentId: 'supervisor-finanzen',
          roleTitle: 'Supervisor Finanzen',
          department: 'Operations',
          systemPrompt: 'You supervise Finanzen.',
          tools: ['github_read'],
          guardrails: ['Only Module.Finanzen'],
          managerId: 'leo',
        },
      }),
      't1',
      'Hire Finanzen supervisor',
    );
    assert.equal(r.ok, true);
    assert.equal(r.branch, 'create');
    assert.equal(r.createBody.jobTitle, 'Supervisor Finanzen');
    assert.equal(r.createBody.managerId, 'leo');
    assert.deepEqual(r.createBody.tools, ['github_read']);
  });

  it('routes clarification to User', () => {
    const r = parseHelgaDecision(
      JSON.stringify({
        status: 'needs_clarification',
        clarificationQuestions: 'Which module scope?',
        identity: null,
      }),
      't1',
      'hire someone',
    );
    assert.equal(r.ok, true);
    assert.equal(r.branch, 'clarify');
    assert.equal(r.routeBody.targetAgentId, 'User');
    assert.equal(r.routeBody.senderAgentId, 'helga');
  });
});

describe('parseSupervisorDecision', () => {
  it('builds waiting UI status call', () => {
    const r = parseSupervisorDecision(
      JSON.stringify({
        outcome: 'waiting',
        statusMessage: 'Waiting on specialist-react',
        delegations: [],
        approval: null,
      }),
      't1',
      'supervisor-finanzen',
    );
    assert.equal(r.ok, true);
    assert.equal(r.outcome, 'waiting');
    assert.equal(r.httpCalls.length, 1);
    assert.equal(
      r.httpCalls[0].url,
      `${BACKEND_BASE}/api/agents/route-chat-message`,
    );
    assert.equal(r.httpCalls[0].body.targetAgentId, null);
  });

  it('builds one route call per delegation', () => {
    const r = parseSupervisorDecision(
      JSON.stringify({
        outcome: 'delegate',
        statusMessage: null,
        delegations: [
          { targetAgentId: 'specialist-react', content: 'Build UI' },
          { targetAgentId: 'specialist-api', content: 'Build API' },
        ],
        approval: null,
      }),
      't1',
      'supervisor-finanzen',
    );
    assert.equal(r.ok, true);
    assert.equal(r.httpCalls.length, 2);
    assert.equal(r.httpCalls[0].body.targetAgentId, 'specialist-react');
  });

  it('builds approval call when done', () => {
    const r = parseSupervisorDecision(
      JSON.stringify({
        outcome: 'done',
        statusMessage: null,
        delegations: [],
        approval: { content: 'Please approve architecture', artifacts: [] },
      }),
      't1',
      'supervisor-finanzen',
    );
    assert.equal(r.ok, true);
    assert.equal(
      r.httpCalls[0].url,
      `${BACKEND_BASE}/api/await-request-approval`,
    );
    assert.equal(r.httpCalls[0].body.threadId, 't1');
  });
});

describe('buildSpecialistDoneBody', () => {
  it('routes result to managerId', () => {
    const body = buildSpecialistDoneBody({
      threadId: 't1',
      senderAgentId: 'specialist-react',
      managerId: 'supervisor-finanzen',
      content: 'UI done',
    });
    assert.equal(body.targetAgentId, 'supervisor-finanzen');
  });
});

describe('buildFailureRoute', () => {
  it('notifies User', () => {
    const body = buildFailureRoute({
      threadId: 't1',
      senderAgentId: 'leo',
      content: 'parse failed',
    });
    assert.equal(body.targetAgentId, 'User');
  });
});
```

- [ ] **Step 3: Run tests — expect FAIL**

Run:

```cmd
cd /d c:\Users\NEWA002\source\repos\aae\agents\n8n-workflows\think-helpers
npm test
```

Expected: FAIL (module not found / exports missing)

- [ ] **Step 4: Implement helpers.mjs**

```js
export const BACKEND_BASE = 'https://ai.neberg.de';

export function parseJsonMaybe(text) {
  if (text == null) return null;
  if (typeof text === 'object') return text;
  const raw = String(text).trim();
  const fenced = raw.match(/```(?:json)?\s*([\s\S]*?)```/i);
  const candidate = fenced ? fenced[1].trim() : raw;
  try {
    return JSON.parse(candidate);
  } catch {
    return null;
  }
}

export function normalizeWebhookBody(body) {
  const b = body ?? {};
  return {
    threadId: b.threadId ?? b.ThreadId ?? '',
    chatHistory: b.chatHistory ?? b.History ?? b.history ?? [],
    userVision: b.userVision ?? b.UserVision ?? '',
    delegationRequest: b.delegationRequest ?? b.DelegationRequest ?? null,
    taskContext: b.taskContext ?? b.TaskContext ?? null,
    subordinatesList: b.subordinatesList ?? b.SubordinatesList ?? [],
    allowedTools: b.allowedTools ?? b.AllowedTools ?? [],
    managerId: b.managerId ?? b.ManagerId ?? null,
    senderAgentId: b.senderAgentId ?? b.SenderAgentId ?? null,
  };
}

export function buildLeoPrompt({ userVision, chatHistory }) {
  return [
    'You are Leo, AAE CEO orchestrator. Output ONLY JSON.',
    'Decompose the vision into supervisor-level delegations (or helga hr_request).',
    'Never use the word teamleiter; use supervisor-* agent ids.',
    'Schema: {"delegations":[{"targetAgentId":"supervisor-...|helga","intent":"delegation|hr_request","message":"...","moduleScope":"Module.X"}]}',
    `Vision: ${userVision}`,
    `History: ${JSON.stringify(chatHistory ?? [])}`,
  ].join('\n');
}

function assertNoTeamleiter(id) {
  if (String(id).toLowerCase().includes('teamleiter')) {
    return false;
  }
  return true;
}

export function parseLeoDelegations(agentOutput, threadId) {
  const obj = parseJsonMaybe(agentOutput);
  if (!obj || !Array.isArray(obj.delegations) || obj.delegations.length === 0) {
    return {
      ok: false,
      error: 'invalid_leo_output',
      userMessage: 'Leo could not produce valid delegations.',
    };
  }
  const items = [];
  for (const d of obj.delegations) {
    if (!d?.targetAgentId || !assertNoTeamleiter(d.targetAgentId)) {
      return {
        ok: false,
        error: 'invalid_target',
        userMessage: 'Leo produced an invalid targetAgentId.',
      };
    }
    const scope = d.moduleScope ? ` [scope=${d.moduleScope}]` : '';
    const intent = d.intent ? ` (${d.intent})` : '';
    items.push({
      threadId,
      senderAgentId: 'leo',
      targetAgentId: d.targetAgentId,
      content: `${d.message ?? ''}${intent}${scope}`.trim(),
    });
  }
  return { ok: true, items };
}

export function mapHelgaIdentityToCreateRequest(identity, jobDescription) {
  return {
    jobTitle: identity.roleTitle ?? identity.jobTitle ?? '',
    jobDescription: jobDescription ?? identity.jobDescription ?? '',
    department: identity.department ?? '',
    managerId: identity.managerId ?? null,
    systemPrompt: identity.systemPrompt ?? '',
    guardrails: identity.guardrails ?? [],
    tools: identity.tools ?? identity.required_tools ?? [],
  };
}

export function parseHelgaDecision(agentOutput, threadId, jobDescription) {
  const obj = parseJsonMaybe(agentOutput);
  if (!obj || !obj.status) {
    return {
      ok: false,
      error: 'invalid_helga_output',
      userMessage: 'Helga returned invalid JSON.',
    };
  }
  if (obj.status === 'needs_clarification') {
    return {
      ok: true,
      branch: 'clarify',
      routeBody: {
        threadId,
        senderAgentId: 'helga',
        targetAgentId: 'User',
        content: String(obj.clarificationQuestions ?? 'Need more details.'),
      },
    };
  }
  if (obj.status === 'ready' && obj.identity) {
    if (obj.identity.managerId && !assertNoTeamleiter(obj.identity.managerId)) {
      return {
        ok: false,
        error: 'invalid_manager',
        userMessage: 'Helga used forbidden teamleiter naming.',
      };
    }
    return {
      ok: true,
      branch: 'create',
      createBody: mapHelgaIdentityToCreateRequest(obj.identity, jobDescription),
    };
  }
  return {
    ok: false,
    error: 'invalid_helga_status',
    userMessage: 'Helga status was neither ready nor needs_clarification.',
  };
}

export function parseSupervisorDecision(agentOutput, threadId, senderAgentId) {
  const obj = parseJsonMaybe(agentOutput);
  if (!obj || !obj.outcome) {
    return {
      ok: false,
      error: 'invalid_supervisor_output',
      userMessage: 'Supervisor returned invalid JSON.',
    };
  }
  const routeUrl = `${BACKEND_BASE}/api/agents/route-chat-message`;
  if (obj.outcome === 'waiting') {
    return {
      ok: true,
      outcome: 'waiting',
      httpCalls: [
        {
          url: routeUrl,
          body: {
            threadId,
            senderAgentId,
            targetAgentId: null,
            content: String(obj.statusMessage ?? 'Waiting on subordinates.'),
          },
        },
      ],
    };
  }
  if (obj.outcome === 'delegate') {
    const delegations = Array.isArray(obj.delegations) ? obj.delegations : [];
    if (delegations.length === 0) {
      return {
        ok: false,
        error: 'empty_delegations',
        userMessage: 'Supervisor chose delegate with no targets.',
      };
    }
    const httpCalls = [];
    for (const d of delegations) {
      if (!d?.targetAgentId || !assertNoTeamleiter(d.targetAgentId)) {
        return {
          ok: false,
          error: 'invalid_target',
          userMessage: 'Supervisor produced invalid targetAgentId.',
        };
      }
      httpCalls.push({
        url: routeUrl,
        body: {
          threadId,
          senderAgentId,
          targetAgentId: d.targetAgentId,
          content: String(d.content ?? ''),
        },
      });
    }
    return { ok: true, outcome: 'delegate', httpCalls };
  }
  if (obj.outcome === 'done') {
    return {
      ok: true,
      outcome: 'done',
      httpCalls: [
        {
          url: `${BACKEND_BASE}/api/await-request-approval`,
          body: {
            threadId,
            senderAgentId,
            content: String(obj.approval?.content ?? 'Ready for approval.'),
            artifacts: obj.approval?.artifacts ?? [],
          },
        },
      ],
    };
  }
  return {
    ok: false,
    error: 'unknown_outcome',
    userMessage: `Unknown supervisor outcome: ${obj.outcome}`,
  };
}

export function buildSpecialistDoneBody({
  threadId,
  senderAgentId,
  managerId,
  content,
}) {
  return {
    threadId,
    senderAgentId,
    targetAgentId: managerId,
    content,
  };
}

export function buildFailureRoute({ threadId, senderAgentId, content }) {
  return {
    threadId,
    senderAgentId,
    targetAgentId: 'User',
    content,
  };
}
```

- [ ] **Step 5: Run tests — expect PASS**

```cmd
cd /d c:\Users\NEWA002\source\repos\aae\agents\n8n-workflows\think-helpers
npm test
```

Expected: all tests PASS

- [ ] **Step 6: Commit**

```cmd
git add agents/n8n-workflows/think-helpers/package.json agents/n8n-workflows/think-helpers/helpers.mjs agents/n8n-workflows/think-helpers/helpers.test.mjs
git commit -m "test: add shared helpers for AAE n8n think-workflows"
```

---

### Task 2: Leo think workflow JSON

**Files:**
- Create: `agents/n8n-workflows/leo-think.json`

**Interfaces:**
- Consumes: `normalizeWebhookBody`, `buildLeoPrompt`, `parseLeoDelegations`, `buildFailureRoute`, `BACKEND_BASE` (logic inlined into Code node from Task 1)
- Produces: importable workflow with path `leo-think`

**Node graph (required):**

1. `Webhook` — `httpMethod: POST`, `path: leo-think`, `responseMode: onReceived` (immediate 200)
2. `Set` — map normalized fields / store `threadId`
3. `AI Agent` (`@n8n/n8n-nodes-langchain.agent`) — prompt from Set; system message = Leo orchestrator rules; JSON-only
4. `OpenAI Chat Model` (`@n8n/n8n-nodes-langchain.lmChatOpenAi`) — model `gpt-4o`; empty credential id for post-import attach; connect `ai_languageModel` → Agent
5. `Code` — paste Task 1 Leo parse functions; output either `{ ok:true, items:[...] }` or failure route item
6. `If` / `Switch` — `ok === true` vs failure
7. Failure path: `HTTP Request` POST `${BACKEND_BASE}/api/agents/route-chat-message` with failure body
8. Success path: `Split Out` on `items` → `HTTP Request` POST route-chat-message per item (`jsonBody` from `$json`)

- [ ] **Step 1: Author leo-think.json**

Create a valid n8n export with at least:

- `name`: `AAE Leo Think`
- webhook path exactly `leo-think`
- no nodes of type containing `wait` / `Wait`
- HTTP URLs exactly `https://ai.neberg.de/api/agents/route-chat-message`
- Code node embeds the Leo-relevant helper functions (copy from `helpers.mjs`; do not `require` the file — n8n Code nodes are self-contained)
- Sticky note: “Attach OpenAI credential after import”

Minimal structural requirements the implementer must satisfy (validate by parsing JSON):

```js
// sanity checks to run after writing the file (node -e or small script)
const wf = JSON.parse(fs.readFileSync('agents/n8n-workflows/leo-think.json','utf8'));
assert.ok(wf.nodes.some(n => n.type === 'n8n-nodes-base.webhook' && n.parameters.path === 'leo-think'));
assert.ok(wf.nodes.some(n => n.type === '@n8n/n8n-nodes-langchain.agent'));
assert.ok(wf.nodes.some(n => n.type === '@n8n/n8n-nodes-langchain.lmChatOpenAi'));
assert.ok(!JSON.stringify(wf).toLowerCase().includes('teamleiter'));
assert.ok(!wf.nodes.some(n => /wait/i.test(n.type)));
```

Preferred authoring method: build in n8n UI on `https://n8n.neberg.de`, export JSON, commit. If hand-writing, match n8n connection keys: `main`, `ai_languageModel`.

- [ ] **Step 2: Local structural validation**

```cmd
cd /d c:\Users\NEWA002\source\repos\aae
node -e "const fs=require('fs');const wf=JSON.parse(fs.readFileSync('agents/n8n-workflows/leo-think.json','utf8'));if(!wf.nodes.some(n=>n.type==='n8n-nodes-base.webhook'&&n.parameters.path==='leo-think'))process.exit(1);if(wf.nodes.some(n=>/wait/i.test(n.type)))process.exit(2);console.log('leo-think ok',wf.nodes.length,'nodes');"
```

Expected: `leo-think ok N nodes`

- [ ] **Step 3: Commit**

```cmd
git add agents/n8n-workflows/leo-think.json
git commit -m "feat: add Leo leo-think n8n workflow"
```

---

### Task 3: Helga think workflow JSON

**Files:**
- Create: `agents/n8n-workflows/helga-think.json`

**Interfaces:**
- Consumes: Helga helpers from Task 1
- Produces: importable workflow path `helga-think`

**Node graph:**

1. Webhook `helga-think`
2. Set — build Helga prompt from `delegationRequest` + `chatHistory`
3. AI Agent + OpenAI Chat Model — Helga identity JSON with `status`
4. Code — `parseHelgaDecision(...)` → `branch`
5. Switch on `branch`:
   - `clarify` → HTTP POST `.../api/agents/route-chat-message`
   - `create` → HTTP POST `.../api/agents/create-identity`
6. Failure → route to User via `buildFailureRoute`

Create-identity body must include all fields: `jobTitle`, `jobDescription`, `department`, `managerId`, `systemPrompt`, `guardrails`, `tools`.

- [ ] **Step 1: Author helga-think.json** (UI export or hand-craft; same rules as Task 2)

- [ ] **Step 2: Structural validation**

```cmd
node -e "const fs=require('fs');const wf=JSON.parse(fs.readFileSync('agents/n8n-workflows/helga-think.json','utf8'));const paths=wf.nodes.filter(n=>n.type==='n8n-nodes-base.webhook').map(n=>n.parameters.path);if(!paths.includes('helga-think'))process.exit(1);const urls=JSON.stringify(wf);if(!urls.includes('/api/agents/create-identity'))process.exit(2);if(!urls.includes('/api/agents/route-chat-message'))process.exit(3);if(/teamleiter/i.test(urls))process.exit(4);console.log('helga-think ok');"
```

Expected: `helga-think ok`

- [ ] **Step 3: Commit**

```cmd
git add agents/n8n-workflows/helga-think.json
git commit -m "feat: add Helga helga-think n8n workflow"
```

---

### Task 4: Supervisor think workflow JSON

**Files:**
- Create: `agents/n8n-workflows/supervisor-think.json`

**Interfaces:**
- Consumes: `parseSupervisorDecision`
- Produces: importable workflow path `supervisor-think`

**Node graph:**

1. Webhook `supervisor-think`
2. Set — inject `taskContext`, `subordinatesList`, `senderAgentId`
3. AI Agent + OpenAI Chat Model
4. Tools (HTTP Request Tool nodes connected on `ai_tool`):
   - `create_github_issue` — POST GitHub issues API (repo from sticky note / expression placeholders `OWNER/REPO`)
   - `update_issue_status` — PATCH/POST issue state or project status
   - `add_issue_comment` — POST issue comments
5. Code — `parseSupervisorDecision`
6. Switch on `outcome` (`waiting` | `delegate` | `done`)
7. For `waiting`/`delegate`: Split `httpCalls` → HTTP Request (url + json body from item)
8. For `done`: HTTP Request to `https://ai.neberg.de/api/await-request-approval`

Sticky notes: attach OpenAI + GitHub credentials; set `OWNER/REPO` placeholders.

- [ ] **Step 1: Author supervisor-think.json**

- [ ] **Step 2: Structural validation**

```cmd
node -e "const fs=require('fs');const wf=JSON.parse(fs.readFileSync('agents/n8n-workflows/supervisor-think.json','utf8'));if(!wf.nodes.some(n=>n.parameters&&n.parameters.path==='supervisor-think'))process.exit(1);const s=JSON.stringify(wf);if(!s.includes('await-request-approval'))process.exit(2);if(!s.includes('create_github_issue')&&!s.includes('create github'))process.exit(3);if(/teamleiter/i.test(s))process.exit(4);if(wf.nodes.some(n=>/wait/i.test(n.type)&&!/webhook/i.test(n.type)))process.exit(5);console.log('supervisor-think ok');"
```

Expected: `supervisor-think ok`

- [ ] **Step 3: Commit**

```cmd
git add agents/n8n-workflows/supervisor-think.json
git commit -m "feat: add Supervisor supervisor-think n8n workflow"
```

---

### Task 5: Specialist think workflow JSON

**Files:**
- Create: `agents/n8n-workflows/specialist-think.json`

**Interfaces:**
- Consumes: `normalizeWebhookBody`, `buildSpecialistDoneBody`, `buildFailureRoute`
- Produces: importable workflow path `specialist-think`

**Node graph:**

1. Webhook `specialist-think`
2. Set — prompt includes hard allowlist: `You may ONLY call tools in allowedTools={{JSON}}`
3. AI Agent + OpenAI Chat Model
4. Single tool: HTTP Request Tool named `execute_tool`
   - URL: `https://ai.neberg.de/api/agents/execute-tool`
   - Body: `{ "threadId", "agentId", "tool", "args" }` (tool parameters from Agent)
5. Code — build done body to `managerId` from Agent final text (or structured `{ "content": "..." }`)
6. HTTP Request → `route-chat-message` to manager
7. No filesystem / GitHub / local path tools

- [ ] **Step 1: Author specialist-think.json**

- [ ] **Step 2: Structural validation**

```cmd
node -e "const fs=require('fs');const wf=JSON.parse(fs.readFileSync('agents/n8n-workflows/specialist-think.json','utf8'));const s=JSON.stringify(wf);if(!s.includes('specialist-think'))process.exit(1);if(!s.includes('/api/agents/execute-tool'))process.exit(2);if(!s.includes('/api/agents/route-chat-message'))process.exit(3);if(/filesystem|readFile|writeFile/i.test(s))process.exit(4);console.log('specialist-think ok');"
```

Expected: `specialist-think ok`

- [ ] **Step 3: Commit**

```cmd
git add agents/n8n-workflows/specialist-think.json
git commit -m "feat: add Specialist specialist-think n8n workflow"
```

---

### Task 6: Docs + VERIFY checklist

**Files:**
- Create: `agents/n8n-workflows/VERIFY.md`
- Modify: `agents/workflow.md`
- Modify: `infrastructure/n8n/README.md`

**Interfaces:**
- Consumes: shipped webhook paths and DTO shapes from Tasks 2–5
- Produces: operator docs aligned with implementation

- [ ] **Step 1: Write VERIFY.md**

```md
# Verify AAE think-workflows

## Import

1. Open https://n8n.neberg.de
2. Import `leo-think.json`, `helga-think.json`, `supervisor-think.json`, `specialist-think.json`
3. Attach OpenAI credential on every Chat Model node
4. Attach GitHub credential on Supervisor tool nodes; set OWNER/REPO
5. Activate all four workflows

## Smoke (no Wait; fire-and-forget)

### Leo
POST https://n8n.neberg.de/webhook/leo-think
```json
{
  "threadId": "smoke-leo",
  "chatHistory": [],
  "userVision": "Build a Finanzen module with UI and API"
}
```
Expect: one or more POSTs to https://ai.neberg.de/api/agents/route-chat-message (camelCase).

### Helga clarify / create
POST https://n8n.neberg.de/webhook/helga-think
```json
{
  "threadId": "smoke-helga",
  "chatHistory": [],
  "delegationRequest": {
    "message": "Need a Finanzen supervisor",
    "moduleScope": "Module.Finanzen",
    "role": "supervisor"
  }
}
```
Expect: either route-chat-message to User OR create-identity with tools/guardrails/managerId.

### Supervisor
POST https://n8n.neberg.de/webhook/supervisor-think
```json
{
  "threadId": "smoke-sup",
  "chatHistory": [],
  "taskContext": "Deliver Finanzen MVP",
  "subordinatesList": ["specialist-react"],
  "senderAgentId": "supervisor-finanzen"
}
```
Expect: waiting | delegate route calls | await-request-approval.

### Specialist
POST https://n8n.neberg.de/webhook/specialist-think
```json
{
  "threadId": "smoke-spec",
  "chatHistory": [],
  "taskContext": "Scaffold React page",
  "allowedTools": ["GenerateCode"],
  "managerId": "supervisor-finanzen",
  "senderAgentId": "specialist-react"
}
```
Expect: optional execute-tool calls; final route-chat-message to supervisor-finanzen.

## Negatives
- No Wait nodes in any workflow
- No `teamleiter` string in exported JSON
```

- [ ] **Step 2: Update agents/workflow.md**

Replace webhook table / sections so paths are `leo-think`, `helga-think`, `supervisor-think`, `specialist-think`; document camelCase `RouteChatMessageRequest` with singular `targetAgentId`; replace Teamleiter wording with Supervisor; point to the four JSON files under `agents/n8n-workflows/`.

- [ ] **Step 3: Update infrastructure/n8n/README.md**

Add a “Think workflows” subsection listing the four import files, OpenAI credential requirement, Supervisor GitHub tools, Specialist `execute-tool` dependency, and link to the design spec + VERIFY.md.

- [ ] **Step 4: Commit**

```cmd
git add agents/n8n-workflows/VERIFY.md agents/workflow.md infrastructure/n8n/README.md
git commit -m "docs: document AAE n8n think-workflows import and verify steps"
```

---

## Self-review (plan vs spec)

| Spec requirement | Task |
|------------------|------|
| Stateless / no Wait | Tasks 2–5 validation steps reject Wait nodes |
| Fire-and-forget HTTP callbacks | All workflow tasks end in HTTP Request |
| Advanced AI Agent + OpenAI | Tasks 2–5 |
| Leo fan-out one call per target | Task 1 `parseLeoDelegations` + Task 2 Split |
| Helga clarify → User; ready → create-identity | Task 1 + Task 3 |
| CreateIdentity full DTO fields | Task 1 `mapHelgaIdentityToCreateRequest` |
| Supervisor GitHub tools + 3 outcomes | Task 4 |
| Specialist execute_tool + report to managerId | Task 5 |
| camelCase + `https://ai.neberg.de` | Task 1 `BACKEND_BASE` + all HTTP nodes |
| `supervisor` naming / no teamleiter | Task 1 asserts + validation greps |
| Webhook paths `*-think` | Tasks 2–5 |
| Docs / VERIFY | Task 6 |

No TBD placeholders remain. Helper APIs are consistent across tasks (`parseLeoDelegations`, `parseHelgaDecision`, `parseSupervisorDecision`, `buildSpecialistDoneBody`).

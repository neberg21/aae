# Leo / Helga Fetch System Prompt Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Leo and Helga n8n think-workflows load `systemPrompt` from `GET /api/agents/search` + `GET /api/agents/{identityId}` instead of hardcoded Agent `systemMessage` strings, and update docs.

**Architecture:** After Normalize Input, each workflow runs Search → Resolve identityId → Get agent → AI Agent. User prompt, parse, and outbound callbacks stay unchanged. Supervisor and specialist workflows are out of scope.

**Tech Stack:** n8n workflow JSON, Node `validate-workflow.mjs`, Markdown docs

**Spec:** [`docs/superpowers/specs/2026-07-22-leo-helga-fetch-system-prompt-design.md`](../specs/2026-07-22-leo-helga-fetch-system-prompt-design.md)

## Global Constraints

- Touch only `leo-think.json` and `helga-think.json` among workflow JSONs (plus docs / validate script if needed)
- Backend base URL remains `https://ai.neberg.de`
- Search query: `department=Core&name=leo` or `name=helga`
- AI Agent `systemMessage` must be expression-bound to fetched `systemPrompt`
- Do not change parse / route / create-identity logic
- Windows: use `cmd /c` for verification commands; no PowerShell scripts

## File map

| File | Responsibility |
|------|----------------|
| `agents/n8n-workflows/leo-think.json` | Insert search/resolve/get nodes; bind systemMessage |
| `agents/n8n-workflows/helga-think.json` | Same for Helga |
| `agents/n8n-workflows/validate-workflow.mjs` | Optionally assert Leo/Helga contain search + get URLs |
| `agents/n8n-workflows/VERIFY.md` | Document expected GET calls |
| `agents/workflow.md` | Document GET endpoints + Leo/Helga flow |
| `docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md` | Update Leo/Helga flow + contracts |
| `docs/deployed-services.md` | Mention identity GETs from think workflows |

---

### Task 1: Update `leo-think.json`

**Files:**
- Modify: `agents/n8n-workflows/leo-think.json`
- Test: `node agents\n8n-workflows\validate-workflow.mjs agents\n8n-workflows\leo-think.json leo-think`

**Interfaces:**
- Consumes: Normalize Output fields `prompt`, `threadId`, …; search page `items[].identityId`; get-by-id `systemPrompt`
- Produces: AI Agent with `options.systemMessage` = `={{ $json.systemPrompt }}` and `text` = `={{ $('Normalize Input').item.json.prompt }}` (or merged item that still carries `prompt`)

- [ ] **Step 1: Update Setup sticky note**

Set content to mention:

```text
## AAE Leo Think
Attach OpenAI credential after import.
Webhook: POST /webhook/leo-think
Load systemPrompt: GET /api/agents/search?department=Core&name=leo then GET /api/agents/{identityId}
Callbacks: https://ai.neberg.de/api/agents/route-chat-message
```

- [ ] **Step 2: Insert Search Agent HTTP node**

After `Normalize Input`, before AI Agent:

- id: `leo-http-search`
- name: `Search Agent`
- type: `n8n-nodes-base.httpRequest`
- typeVersion: `4.2`
- method: `GET`
- url: `https://ai.neberg.de/api/agents/search?department=Core&name=leo`
- position: roughly `[620, 300]`

- [ ] **Step 3: Insert Resolve Identity Code node**

- id: `leo-code-resolve-id`
- name: `Resolve Identity`
- type: `n8n-nodes-base.code`
- typeVersion: `2`
- jsCode:

```javascript
const search = items[0].json;
const page = search.body ?? search;
const list = page.items ?? page.Items ?? [];
if (!Array.isArray(list) || list.length === 0) {
  throw new Error('leo_identity_not_found');
}
const first = list[0];
const identityId = first.identityId ?? first.IdentityId;
if (!identityId) {
  throw new Error('leo_identity_id_missing');
}
const prev = $('Normalize Input').first().json;
return [{ json: { ...prev, identityId } }];
```

- [ ] **Step 4: Insert Get System Prompt HTTP node**

- id: `leo-http-get-prompt`
- name: `Get System Prompt`
- type: `n8n-nodes-base.httpRequest`
- typeVersion: `4.2`
- method: `GET`
- url: `=https://ai.neberg.de/api/agents/{{ $json.identityId }}`
- position: roughly `[980, 300]`

- [ ] **Step 5: Merge prompt + systemPrompt into AI Agent inputs**

Either add a short Code node `Prepare Agent Input` after Get System Prompt:

```javascript
const agent = items[0].json;
const body = agent.body ?? agent;
const systemPrompt = body.systemPrompt ?? body.SystemPrompt ?? '';
if (!systemPrompt) {
  throw new Error('leo_system_prompt_missing');
}
const prev = $('Normalize Input').first().json;
return [{ json: { ...prev, identityId: prev.identityId ?? $('Resolve Identity').first().json.identityId, systemPrompt } }];
```

Or rely on expressions that reach back to `Normalize Input` and the get response. Prefer an explicit Prepare node so AI Agent sees one item with both `prompt` and `systemPrompt`.

Update AI Agent:

```json
"text": "={{ $json.prompt }}",
"options": {
  "systemMessage": "={{ $json.systemPrompt }}"
}
```

Remove any hardcoded Leo role string from `systemMessage`.

- [ ] **Step 6: Rewire connections**

```text
Webhook → Normalize Input → Search Agent → Resolve Identity → Get System Prompt → Prepare Agent Input → AI Agent → Parse Delegations → …
```

Keep OpenAI Chat Model → AI Agent `ai_languageModel` connection.

Shift Parse / IF / Split / Route node x-positions right if needed so the canvas stays readable (optional).

- [ ] **Step 7: Validate structure**

Run:

```cmd
node agents\n8n-workflows\validate-workflow.mjs agents\n8n-workflows\leo-think.json leo-think
```

Expected: `leo-think ok` with higher node count than before (was 9-ish nodes; expect +3 or +4).

Also confirm JSON string contains:

- `/api/agents/search?department=Core&name=leo`
- `systemMessage": "={{ $json.systemPrompt }}"`
- does **not** contain the old hardcoded `You are Leo, CEO orchestrator of the Autonomous Agent Ecosystem`

Run:

```cmd
node -e "const fs=require('fs');const s=fs.readFileSync('agents/n8n-workflows/leo-think.json','utf8');if(!s.includes('/api/agents/search?department=Core&name=leo'))process.exit(1);if(!s.includes('$json.systemPrompt'))process.exit(2);if(/You are Leo, CEO orchestrator of the Autonomous Agent Ecosystem/.test(s))process.exit(3);console.log('leo prompts ok');"
```

Expected: `leo prompts ok`

- [ ] **Step 8: Commit**

```cmd
git add agents\n8n-workflows\leo-think.json
git commit -m "feat: load Leo systemPrompt from agent search and get APIs"
```

---

### Task 2: Update `helga-think.json`

**Files:**
- Modify: `agents/n8n-workflows/helga-think.json`
- Test: same validate pattern as Leo with `helga-think`

**Interfaces:**
- Same pattern as Task 1 with `name=helga`
- Parse Decision still reads `$('Normalize Input')` for `threadId` / `jobDescription`

- [ ] **Step 1: Update Setup sticky note**

```text
## AAE Helga Think
Attach OpenAI credential after import.
Webhook: POST /webhook/helga-think
Load systemPrompt: GET /api/agents/search?department=Core&name=helga then GET /api/agents/{identityId}
Paths: clarify → route-chat-message (User); ready → create-identity
```

- [ ] **Step 2–6: Mirror Leo nodes for Helga**

Identical graph surgery with these substitutions:

| Piece | Value |
|-------|--------|
| Search URL | `https://ai.neberg.de/api/agents/search?department=Core&name=helga` |
| Resolve errors | `helga_identity_not_found` / `helga_identity_id_missing` |
| Prepare error | `helga_system_prompt_missing` |
| Node id prefixes | `helga-http-search`, `helga-code-resolve-id`, `helga-http-get-prompt`, `helga-code-prepare` |

AI Agent:

```json
"text": "={{ $json.prompt }}",
"options": {
  "systemMessage": "={{ $json.systemPrompt }}"
}
```

Remove hardcoded `You are Helga, HR director of AAE...` from `systemMessage`.

Connections:

```text
Webhook → Normalize Input → Search Agent → Resolve Identity → Get System Prompt → Prepare Agent Input → AI Agent → Parse Decision → Branch → …
```

- [ ] **Step 7: Validate**

```cmd
node agents\n8n-workflows\validate-workflow.mjs agents\n8n-workflows\helga-think.json helga-think
node -e "const fs=require('fs');const s=fs.readFileSync('agents/n8n-workflows/helga-think.json','utf8');if(!s.includes('/api/agents/search?department=Core&name=helga'))process.exit(1);if(!s.includes('$json.systemPrompt'))process.exit(2);if(/You are Helga, HR director of AAE/.test(s))process.exit(3);console.log('helga prompts ok');"
```

Expected: `helga-think ok` and `helga prompts ok`

Confirm supervisor/specialist untouched:

```cmd
git diff --name-only -- agents\n8n-workflows\supervisor-think.json agents\n8n-workflows\specialist-think.json
```

Expected: empty output

- [ ] **Step 8: Commit**

```cmd
git add agents\n8n-workflows\helga-think.json
git commit -m "feat: load Helga systemPrompt from agent search and get APIs"
```

---

### Task 3: Docs + VERIFY

**Files:**
- Modify: `agents/workflow.md`
- Modify: `agents/n8n-workflows/VERIFY.md`
- Modify: `docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md`
- Modify: `docs/deployed-services.md`

- [ ] **Step 1: Update `agents/workflow.md` endpoint table**

Add rows:

| HTTP Method & Route | Sender | Description |
| --- | --- | --- |
| **GET** `/api/agents/search?name=&department=&jobTitle=` | n8n (Leo/Helga) | Find identities; Leo/Helga use `department=Core&name=leo\|helga` and take `items[0].identityId`. |
| **GET** `/api/agents/{identityId}` | n8n (Leo/Helga) | Returns identity including `systemPrompt` for the AI Agent `systemMessage`. |

In sections A (Leo) and B (Helga), add one bullet:

* **System prompt:** search Core agent by name → get-by-id → bind `systemPrompt` as Agent `systemMessage`

- [ ] **Step 2: Update `VERIFY.md` Leo/Helga expects**

Under Leo smoke, add:

```text
Expect before model: GET /api/agents/search?department=Core&name=leo then GET /api/agents/{identityId}.
```

Under Helga, same with `name=helga`.

- [ ] **Step 3: Update think-workflows design spec**

In `docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md`:

1. Add to Outbound HTTP table (or a new Inbound-from-n8n GET table):

| Purpose | Method + URL | Notes |
|---------|--------------|-------|
| Search identity | `GET https://ai.neberg.de/api/agents/search?department=Core&name=leo\|helga` | Page of `AgentDto` |
| Get identity | `GET https://ai.neberg.de/api/agents/{identityId}` | Includes `systemPrompt` |

2. Change Common node skeleton to:

```text
Webhook → Normalize → Search → Resolve Id → Get Prompt → Prepare → AI Agent → Code → Switch? → HTTP → end
```

(Note supervisor/specialist still use the shorter skeleton without search; call that out.)

3. Leo flow steps: insert search/get/prepare between normalize and AI Agent.
4. Helga flow steps: same.

- [ ] **Step 4: Update `docs/deployed-services.md`**

Extend the think-workflow callbacks bullet:

```text
**Think workflow callbacks / identity reads:** hardcoded base `https://ai.neberg.de` in imported JSON (for example `POST /api/agents/route-chat-message`, `POST /api/agents/create-identity`, and for Leo/Helga `GET /api/agents/search` + `GET /api/agents/{identityId}` for `systemPrompt`).
```

- [ ] **Step 5: Commit**

```cmd
git add agents\workflow.md agents\n8n-workflows\VERIFY.md docs\superpowers\specs\2026-07-22-aae-n8n-think-workflows-design.md docs\deployed-services.md
git commit -m "docs: document Leo/Helga systemPrompt fetch via agent APIs"
```

---

## Self-review

1. Spec coverage: search+get+bind+docs+VERIFY+scope lock — Tasks 1–3
2. No placeholders
3. Property names: `identityId`, `systemPrompt`, `items` match backend DTOs

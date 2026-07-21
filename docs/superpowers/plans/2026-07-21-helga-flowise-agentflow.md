# Helga Flowise Agentflow V2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship an importable Flowise Agentflow V2 for Helga (LLM brain only) plus a structural unit test and a short Flowise README pointer.

**Architecture:** Version a marketplace-style Agentflow V2 JSON (`nodes` + `edges`) under `agents/flowise-workflows/`. Start → Agent with `chatOpenAI` and a locked system-prompt placeholder. n8n keeps all HR wiring; operators paste `agents/identities/helga.md` after import.

**Tech Stack:** Flowise 3.1.1 Agentflow V2 JSON, Node.js `node:test` for structural checks

**Spec:** [`docs/superpowers/specs/2026-07-21-helga-flowise-agentflow-design.md`](../specs/2026-07-21-helga-flowise-agentflow-design.md)

## Global Constraints

- Windows scripting policy: no PowerShell/bash scripts; use `cmd /c` for one-off commands; multi-line commit messages via `%TEMP%\commitmsg.txt` + `git commit -F`
- System prompt text in JSON must be exactly: `hol das aktuelle prompt aus dem repo`
- Agent model: `agentModel` = `chatOpenAI`; no credential / API key in JSON
- No tools, no GitHub/Nostr/memory side-effects in Flowise
- Do not commit secrets
- Response language / commit messages: English
- Flowise image pin: `flowiseai/flowise:3.1.1`

## File Structure

| File | Responsibility |
|------|----------------|
| `agents/flowise-workflows/helga.json` | Importable Agentflow V2 (Start → Agent, chatOpenAI, placeholder system prompt) |
| `agents/flowise-workflows/helga.test.mjs` | Structural assertions on `helga.json` |
| `agents/flowise-workflows/package.json` | Local `node --test` runner |
| `infrastructure/flowise/README.md` | Import path + paste `helga.md` + prediction URL note |

---

### Task 1: Structural tests for Helga flow JSON

**Files:**
- Create: `agents/flowise-workflows/package.json`
- Create: `agents/flowise-workflows/helga.test.mjs`
- Create: `agents/flowise-workflows/helga.json` (minimal stub first so the test file can load; Task 2 replaces with full flow)

**Interfaces:**
- Consumes: none
- Produces: failing-then-passing structural contract that Task 2 must satisfy

- [ ] **Step 1: Create package.json**

```json
{
  "name": "helga-flowise-workflow",
  "private": true,
  "type": "module",
  "scripts": {
    "test": "node --test helga.test.mjs"
  }
}
```

- [ ] **Step 2: Write failing tests**

Create `agents/flowise-workflows/helga.test.mjs`:

```js
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const flow = JSON.parse(readFileSync(join(__dirname, 'helga.json'), 'utf8'));

const SYSTEM_PLACEHOLDER = 'hol das aktuelle prompt aus dem repo';

describe('helga Flowise Agentflow V2', () => {
  it('has nodes and edges arrays', () => {
    assert.ok(Array.isArray(flow.nodes));
    assert.ok(Array.isArray(flow.edges));
    assert.equal(flow.nodes.length, 2);
    assert.equal(flow.edges.length, 1);
  });

  it('has Start then Agent nodes', () => {
    const start = flow.nodes.find((n) => n.data?.name === 'startAgentflow');
    const agent = flow.nodes.find((n) => n.data?.name === 'agentAgentflow');
    assert.ok(start, 'missing startAgentflow');
    assert.ok(agent, 'missing agentAgentflow');
    assert.equal(agent.data.label, 'Helga');
  });

  it('wires Start → Agent', () => {
    const edge = flow.edges[0];
    assert.equal(edge.source, 'startAgentflow_0');
    assert.equal(edge.target, 'agentAgentflow_0');
  });

  it('uses chatOpenAI with locked system placeholder and no tools', () => {
    const agent = flow.nodes.find((n) => n.data?.name === 'agentAgentflow');
    const inputs = agent.data.inputs;
    assert.equal(inputs.agentModel, 'chatOpenAI');
    assert.equal(inputs.agentEnableMemory, false);
    assert.ok(Array.isArray(inputs.agentMessages));
    const system = inputs.agentMessages.find((m) => m.role === 'system');
    assert.ok(system);
    assert.equal(system.content, SYSTEM_PLACEHOLDER);
    const tools = inputs.agentTools;
    assert.ok(tools === '' || tools === undefined || (Array.isArray(tools) && tools.length === 0));
  });

  it('does not embed API keys or credentials', () => {
    const raw = JSON.stringify(flow);
    assert.equal(/sk-[a-zA-Z0-9]/.test(raw), false);
    assert.equal(/api[_-]?key/i.test(raw) && /"[^"]*sk-/.test(raw), false);
    const agent = flow.nodes.find((n) => n.data?.name === 'agentAgentflow');
    assert.equal(agent.data.inputs.agentModelConfig?.credential, undefined);
  });
});
```

- [ ] **Step 3: Create a stub helga.json so the suite can load**

```json
{
  "description": "STUB — replace in Task 2",
  "nodes": [],
  "edges": []
}
```

- [ ] **Step 4: Run tests — expect FAIL**

Run:

```cmd
cd /d c:\Users\NEWA002\source\repos\aae\agents\flowise-workflows
npm test
```

Expected: FAIL (node count / missing Start/Agent assertions).

- [ ] **Step 5: Commit**

```cmd
cd /d c:\Users\NEWA002\source\repos\aae
git add agents/flowise-workflows/package.json agents/flowise-workflows/helga.test.mjs agents/flowise-workflows/helga.json
(
echo test: add Helga Flowise Agentflow structural checks
) > %TEMP%\commitmsg.txt
git commit -F %TEMP%\commitmsg.txt
del %TEMP%\commitmsg.txt
```

---

### Task 2: Importable Helga Agentflow V2 JSON

**Files:**
- Modify: `agents/flowise-workflows/helga.json` (replace stub with full flow)
- Test: `agents/flowise-workflows/helga.test.mjs`

**Interfaces:**
- Consumes: structural contract from Task 1
- Produces: Flowise-importable `helga.json` (marketplace shape: top-level `nodes` + `edges`)

- [ ] **Step 1: Replace `helga.json` with the full Agentflow**

Write `agents/flowise-workflows/helga.json` exactly as follows (marketplace-compatible; modeled on Flowise `Translator` / `Human In The Loop` templates, trimmed to Start → Agent):

```json
{
  "description": "Helga — AAE HR and identity architect (LLM only). Paste system prompt from agents/identities/helga.md after import. n8n owns GitHub/Nostr wiring.",
  "usecases": ["AAE", "Helga"],
  "nodes": [
    {
      "id": "startAgentflow_0",
      "type": "agentFlow",
      "position": { "x": 64, "y": 98.5 },
      "data": {
        "id": "startAgentflow_0",
        "label": "Start",
        "version": 1.1,
        "name": "startAgentflow",
        "type": "Start",
        "color": "#7EE787",
        "hideInput": true,
        "baseClasses": ["Start"],
        "category": "Agent Flows",
        "description": "Starting point of the agentflow",
        "inputParams": [
          {
            "label": "Input Type",
            "name": "startInputType",
            "type": "options",
            "options": [
              {
                "label": "Chat Input",
                "name": "chatInput",
                "description": "Start the conversation with chat input"
              },
              {
                "label": "Form Input",
                "name": "formInput",
                "description": "Start the workflow with form inputs"
              }
            ],
            "default": "chatInput",
            "id": "startAgentflow_0-input-startInputType-options",
            "display": true
          },
          {
            "label": "Ephemeral Memory",
            "name": "startEphemeralMemory",
            "type": "boolean",
            "description": "Start fresh for every execution without past chat history",
            "optional": true,
            "id": "startAgentflow_0-input-startEphemeralMemory-boolean",
            "display": true
          }
        ],
        "inputAnchors": [],
        "inputs": {
          "startInputType": "chatInput",
          "startEphemeralMemory": true,
          "startState": "",
          "startPersistState": ""
        },
        "outputAnchors": [
          {
            "id": "startAgentflow_0-output-startAgentflow",
            "label": "Start",
            "name": "startAgentflow"
          }
        ],
        "outputs": {},
        "selected": false
      },
      "width": 103,
      "height": 66,
      "positionAbsolute": { "x": 64, "y": 98.5 },
      "selected": false,
      "dragging": false
    },
    {
      "id": "agentAgentflow_0",
      "type": "agentFlow",
      "position": { "x": 280, "y": 80 },
      "data": {
        "id": "agentAgentflow_0",
        "label": "Helga",
        "version": 3.2,
        "name": "agentAgentflow",
        "type": "Agent",
        "color": "#4DD0E1",
        "baseClasses": ["Agent"],
        "category": "Agent Flows",
        "description": "Helga HR identity architect — LLM only; no tools",
        "inputParams": [
          {
            "label": "Model",
            "name": "agentModel",
            "type": "asyncOptions",
            "loadMethod": "listModels",
            "loadConfig": true,
            "id": "agentAgentflow_0-input-agentModel-asyncOptions",
            "display": true
          },
          {
            "label": "Messages",
            "name": "agentMessages",
            "type": "array",
            "optional": true,
            "acceptVariable": true,
            "array": [
              {
                "label": "Role",
                "name": "role",
                "type": "options",
                "options": [
                  { "label": "System", "name": "system" },
                  { "label": "Assistant", "name": "assistant" },
                  { "label": "Developer", "name": "developer" },
                  { "label": "User", "name": "user" }
                ]
              },
              {
                "label": "Content",
                "name": "content",
                "type": "string",
                "acceptVariable": true,
                "generateInstruction": true,
                "rows": 4
              }
            ],
            "id": "agentAgentflow_0-input-agentMessages-array",
            "display": true
          },
          {
            "label": "Tools",
            "name": "agentTools",
            "type": "array",
            "optional": true,
            "array": [
              {
                "label": "Tool",
                "name": "agentSelectedTool",
                "type": "asyncOptions",
                "loadMethod": "listTools",
                "loadConfig": true
              },
              {
                "label": "Require Human Input",
                "name": "agentSelectedToolRequiresHumanInput",
                "type": "boolean",
                "optional": true
              }
            ],
            "id": "agentAgentflow_0-input-agentTools-array",
            "display": true
          },
          {
            "label": "Enable Memory",
            "name": "agentEnableMemory",
            "type": "boolean",
            "description": "Enable memory for the conversation thread",
            "default": true,
            "optional": true,
            "id": "agentAgentflow_0-input-agentEnableMemory-boolean",
            "display": true
          },
          {
            "label": "Return Response As",
            "name": "agentReturnResponseAs",
            "type": "options",
            "options": [
              { "label": "User Message", "name": "userMessage" },
              { "label": "Assistant Message", "name": "assistantMessage" }
            ],
            "default": "userMessage",
            "id": "agentAgentflow_0-input-agentReturnResponseAs-options",
            "display": true
          }
        ],
        "inputAnchors": [],
        "inputs": {
          "agentModel": "chatOpenAI",
          "agentMessages": [
            {
              "role": "system",
              "content": "hol das aktuelle prompt aus dem repo"
            }
          ],
          "agentTools": [],
          "agentKnowledgeDocumentStores": "",
          "agentEnableMemory": false,
          "agentUserMessage": "",
          "agentReturnResponseAs": "assistantMessage",
          "agentUpdateState": "",
          "agentModelConfig": {
            "cache": "",
            "modelName": "gpt-4o-mini",
            "temperature": 0.2,
            "streaming": true,
            "maxTokens": "",
            "topP": "",
            "frequencyPenalty": "",
            "presencePenalty": "",
            "timeout": "",
            "strictToolCalling": "",
            "stopSequence": "",
            "basepath": "",
            "proxyUrl": "",
            "baseOptions": "",
            "allowImageUploads": "",
            "imageResolution": "low",
            "reasoningEffort": "",
            "agentModel": "chatOpenAI"
          }
        },
        "outputAnchors": [
          {
            "id": "agentAgentflow_0-output-agentAgentflow",
            "label": "Agent",
            "name": "agentAgentflow"
          }
        ],
        "outputs": {},
        "selected": false
      },
      "width": 189,
      "height": 100,
      "positionAbsolute": { "x": 280, "y": 80 },
      "selected": false,
      "dragging": false
    }
  ],
  "edges": [
    {
      "source": "startAgentflow_0",
      "sourceHandle": "startAgentflow_0-output-startAgentflow",
      "target": "agentAgentflow_0",
      "targetHandle": "agentAgentflow_0",
      "data": {
        "sourceColor": "#7EE787",
        "targetColor": "#4DD0E1",
        "isHumanInput": false
      },
      "type": "agentFlow",
      "id": "startAgentflow_0-startAgentflow_0-output-startAgentflow-agentAgentflow_0-agentAgentflow_0"
    }
  ]
}
```

- [ ] **Step 2: Run tests — expect PASS**

```cmd
cd /d c:\Users\NEWA002\source\repos\aae\agents\flowise-workflows
npm test
```

Expected: all tests PASS.

- [ ] **Step 3: Commit**

```cmd
cd /d c:\Users\NEWA002\source\repos\aae
git add agents/flowise-workflows/helga.json
(
echo feat: add importable Helga Flowise Agentflow V2
) > %TEMP%\commitmsg.txt
git commit -F %TEMP%\commitmsg.txt
del %TEMP%\commitmsg.txt
```

---

### Task 3: Document import in Flowise README

**Files:**
- Modify: `infrastructure/flowise/README.md`

**Interfaces:**
- Consumes: `agents/flowise-workflows/helga.json` from Task 2
- Produces: operator steps to import, paste prompt, wire n8n prediction URL

- [ ] **Step 1: Update Setup + Key types**

In `infrastructure/flowise/README.md`, replace the Setup section with:

```markdown
### Setup

1. Open **https://flowise.neberg.de**.
2. Import Helga from [`agents/flowise-workflows/helga.json`](../../agents/flowise-workflows/helga.json) (Agentflow V2: Start → Agent).
3. On the Helga Agent node: attach an OpenAI credential; replace the system message placeholder (`hol das aktuelle prompt aus dem repo`) with the contents of [`agents/identities/helga.md`](../../agents/identities/helga.md).
4. Save the flow; copy the prediction / API path from the Flowise UI.
5. In n8n (`helga-create-identity`), set `REPLACE_ME_HELGA_PREDICTION_URL` to `https://flowise.neberg.de` + that path.
6. Pass through the structured identity JSON expected by downstream n8n nodes.
```

In the Key types table, add a row:

```markdown
| [`agents/flowise-workflows/helga.json`](../../agents/flowise-workflows/helga.json) | Importable Helga Agentflow V2 (LLM only) |
```

Keep existing Dockerfile row.

- [ ] **Step 2: Commit**

```cmd
cd /d c:\Users\NEWA002\source\repos\aae
git add infrastructure/flowise/README.md
(
echo docs: point Flowise README at Helga Agentflow import
) > %TEMP%\commitmsg.txt
git commit -F %TEMP%\commitmsg.txt
del %TEMP%\commitmsg.txt
```

---

### Task 4: Manual import smoke check (operator)

**Files:** none (runtime verification)

- [ ] **Step 1: Import on Flowise**

Open `https://flowise.neberg.de`, load `agents/flowise-workflows/helga.json`, confirm canvas shows Start → Helga Agent, system message shows `hol das aktuelle prompt aus dem repo`, model is OpenAI, tools empty.

- [ ] **Step 2: Credential + prompt**

Attach OpenAI credential; paste `agents/identities/helga.md`; save.

- [ ] **Step 3: Optional prediction ping**

From Flowise chat or HTTP prediction, send a short HR-style request and confirm JSON-shaped reply (schema validation remains n8n’s job). Copy prediction URL for n8n when ready.

No commit for this task.

---

## Self-review (plan vs spec)

| Spec requirement | Task |
|------------------|------|
| Agentflow V2 Start → Agent | Task 2 |
| `chatOpenAI`, empty credential | Task 2 + test in Task 1 |
| System prompt exact placeholder | Task 2 + test in Task 1 |
| Path `agents/flowise-workflows/helga.json` | Task 2 |
| README pointer + paste `helga.md` | Task 3 |
| No tools / no n8n-owned wiring in Flowise | Task 2 (`agentTools: []`, memory off) |
| Non-goal: auto-fetch prompt from git | Honored (placeholder + manual paste) |

**Placeholder scan:** none remaining.  
**Type consistency:** node ids `startAgentflow_0` / `agentAgentflow_0` match edge wiring and tests.

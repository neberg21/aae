# Helga Create-Identity n8n Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship an importable n8n workflow that turns Leo `hr_request` webhooks into git-backed agent identity JSON files plus dedicated Nostr employee accounts.

**Architecture:** Pure JS helpers (unit-tested) own validation, `agent_id` derivation, Helga schema checks, and display-name formatting. The n8n workflow JSON wires Webhook → helpers → GitHub Contents API → Flowise Helga → faker + Nostr key/sign/publish (Code) → workflow static data for `nsec` → GitHub PUT → Respond. Image bake adds `@faker-js/faker` and Nostr signing deps; `NODE_FUNCTION_ALLOW_EXTERNAL` is documented for the host.

**Tech Stack:** n8n workflow JSON, Node.js (`node:test`), `@faker-js/faker`, `@noble/secp256k1` + `@noble/hashes` (already partially in image), GitHub REST Contents API, Flowise HTTP prediction, Nostr relay `wss://nostr.neberg.de`

**Spec:** [`docs/superpowers/specs/2026-07-21-helga-create-identity-n8n-design.md`](../specs/2026-07-21-helga-create-identity-n8n-design.md)

## Global Constraints

- Windows scripting policy: no PowerShell/bash scripts; use `cmd /c` for one-off commands; commit messages via `-F` file if needed
- `agent_id` = `{domain}-{role}` where domain = `module_scope` with leading `Module.` stripped (case-insensitive) then lowercased; role from `payload.role` (kebab-case)
- Idempotent: GitHub GET before Helga/Nostr; if file exists return `already_exists: true`
- Never write `nsec` to git, webhook responses, or sticky notes; store only in n8n `$getWorkflowStaticData('global').employeeNsecs[agent_id]`
- Kind-0 must be signed with the **new** employee key (not shared Nostrobots credential)
- Display name: `{FirstName} ({role_title})` via `@faker-js/faker` locale `de`
- Auto-commit to default branch; no HITL/PR
- Flowise Helga prediction URL is a sticky-note placeholder (`REPLACE_ME_HELGA_PREDICTION_URL`)
- Do not commit secrets (GitHub tokens, nsec, Flowise keys) into workflow JSON
- Response language / commit messages: English

## File Structure

| File | Responsibility |
|------|----------------|
| `agents/n8n-workflows/helga-create-identity/helpers.mjs` | Pure functions: parse envelope, derive agent_id, validate Helga identity, build public identity JSON (no nsec), decode GitHub content |
| `agents/n8n-workflows/helga-create-identity/helpers.test.mjs` | `node:test` coverage for helpers |
| `agents/n8n-workflows/helga-create-identity/package.json` | Local test runner only (`"type":"module"`, test script); no runtime dep on n8n |
| `agents/n8n-workflows/helga-create-identity.json` | Importable n8n workflow (embeds helper logic in Code nodes; sticky setup) |
| `infrastructure/n8n/Dockerfile` | Bake `@faker-js/faker`, `@noble/secp256k1` (keep `@noble/hashes`) |
| `infrastructure/n8n/README.md` | Document faker/Nostr allow-list, env, Helga workflow import |
| `docs/process/erstelle_teamleiter.md` | One-line pointer to the workflow + spec |

---

### Task 1: Pure helpers + unit tests

**Files:**
- Create: `agents/n8n-workflows/helga-create-identity/package.json`
- Create: `agents/n8n-workflows/helga-create-identity/helpers.mjs`
- Create: `agents/n8n-workflows/helga-create-identity/helpers.test.mjs`

**Interfaces:**
- Consumes: none (pure)
- Produces:
  - `parseHrRequest(body) → { ok: true, value } | { ok: false, error: "invalid_hr_request" }`
  - `deriveAgentId(moduleScope, role) → string`
  - `validateHelgaIdentity(obj) → { ok: true, value } | { ok: false, error: "invalid_identity", details: string[] }`
  - `buildPublicIdentity({ helga, agentId, npub, displayName, relay }) → object` (no `nsec` key)
  - `identityRepoPath(agentId) → string` = `agents/identities/${agentId}.json`
  - `parseGithubFileJson(base64Content) → object`

- [ ] **Step 1: Create package.json**

```json
{
  "name": "helga-create-identity-helpers",
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
  parseHrRequest,
  deriveAgentId,
  validateHelgaIdentity,
  buildPublicIdentity,
  identityRepoPath,
  parseGithubFileJson,
} from './helpers.mjs';

describe('deriveAgentId', () => {
  it('builds domain-role from Module.Finanzen + teamleiter', () => {
    assert.equal(deriveAgentId('Module.Finanzen', 'teamleiter'), 'finanzen-teamleiter');
  });
  it('lowercases domain and accepts Module.Dnd', () => {
    assert.equal(deriveAgentId('Module.Dnd', 'backend'), 'dnd-backend');
  });
  it('strips Module. case-insensitively', () => {
    assert.equal(deriveAgentId('module.Finanzen', 'researcher'), 'finanzen-researcher');
  });
});

describe('parseHrRequest', () => {
  it('accepts valid Leo envelope', () => {
    const r = parseHrRequest({
      action: 'route_message',
      target_agent: '@Helga',
      intent: 'hr_request',
      payload: {
        message: 'hire',
        context: '',
        module_scope: 'Module.Finanzen',
        role: 'teamleiter',
      },
    });
    assert.equal(r.ok, true);
    assert.equal(r.value.agent_id, 'finanzen-teamleiter');
  });
  it('rejects wrong target', () => {
    const r = parseHrRequest({
      action: 'route_message',
      target_agent: '@Leo',
      intent: 'hr_request',
      payload: { module_scope: 'Module.Finanzen', role: 'teamleiter', message: '', context: '' },
    });
    assert.equal(r.ok, false);
    assert.equal(r.error, 'invalid_hr_request');
  });
  it('rejects missing role', () => {
    const r = parseHrRequest({
      action: 'route_message',
      target_agent: '@Helga',
      intent: 'hr_request',
      payload: { module_scope: 'Module.Finanzen', message: '', context: '' },
    });
    assert.equal(r.ok, false);
  });
});

describe('validateHelgaIdentity', () => {
  it('requires core fields and overwrites agent_id externally', () => {
    const r = validateHelgaIdentity({
      agent_id: 'ignored',
      role_title: 'Teamleiter Finanzen',
      department: 'Operations',
      system_prompt: 'You are...',
      required_tools: ['github_read'],
      guardrails: ['Only Module.Finanzen'],
    });
    assert.equal(r.ok, true);
  });
  it('fails when system_prompt missing', () => {
    const r = validateHelgaIdentity({
      role_title: 'X',
      department: 'Backend',
      required_tools: [],
      guardrails: [],
    });
    assert.equal(r.ok, false);
    assert.equal(r.error, 'invalid_identity');
  });
});

describe('buildPublicIdentity', () => {
  it('never includes nsec', () => {
    const id = buildPublicIdentity({
      helga: {
        role_title: 'Teamleiter Finanzen',
        department: 'Operations',
        system_prompt: 'p',
        required_tools: [],
        guardrails: [],
      },
      agentId: 'finanzen-teamleiter',
      npub: 'npub1test',
      displayName: 'Max (Teamleiter Finanzen)',
      relay: 'wss://nostr.neberg.de',
    });
    assert.equal(id.agent_id, 'finanzen-teamleiter');
    assert.equal(id.nostr.npub, 'npub1test');
    assert.equal(id.nostr.display_name, 'Max (Teamleiter Finanzen)');
    assert.equal(Object.prototype.hasOwnProperty.call(id, 'nsec'), false);
    assert.equal(Object.prototype.hasOwnProperty.call(id.nostr, 'nsec'), false);
  });
});

describe('identityRepoPath', () => {
  it('returns agents/identities path', () => {
    assert.equal(identityRepoPath('finanzen-teamleiter'), 'agents/identities/finanzen-teamleiter.json');
  });
});

describe('parseGithubFileJson', () => {
  it('decodes base64 content', () => {
    const obj = { agent_id: 'finanzen-teamleiter', nostr: { npub: 'npub1x', display_name: 'A (B)' } };
    const b64 = Buffer.from(JSON.stringify(obj), 'utf8').toString('base64');
    assert.deepEqual(parseGithubFileJson(b64), obj);
  });
});
```

- [ ] **Step 3: Run tests — expect FAIL**

```cmd
cd agents\n8n-workflows\helga-create-identity
node --test helpers.test.mjs
```

Expected: FAIL (module not found / exports missing)

- [ ] **Step 4: Implement helpers.mjs**

```js
const RELAY_DEFAULT = 'wss://nostr.neberg.de';

export function deriveAgentId(moduleScope, role) {
  if (typeof moduleScope !== 'string' || typeof role !== 'string') {
    throw new Error('moduleScope and role required');
  }
  const trimmedRole = role.trim().toLowerCase();
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(trimmedRole)) {
    throw new Error('role must be kebab-case');
  }
  let domain = moduleScope.trim();
  domain = domain.replace(/^module\./i, '');
  domain = domain.toLowerCase();
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(domain)) {
    // allow single segment alphanumerics after strip (finanzen, dnd)
    if (!/^[a-z0-9]+$/.test(domain)) {
      throw new Error('invalid module_scope domain');
    }
  }
  return `${domain}-${trimmedRole}`;
}

export function parseHrRequest(body) {
  if (!body || typeof body !== 'object') {
    return { ok: false, error: 'invalid_hr_request' };
  }
  if (body.target_agent !== '@Helga' || body.intent !== 'hr_request') {
    return { ok: false, error: 'invalid_hr_request' };
  }
  const payload = body.payload;
  if (!payload || typeof payload !== 'object') {
    return { ok: false, error: 'invalid_hr_request' };
  }
  if (typeof payload.module_scope !== 'string' || !payload.module_scope.trim()) {
    return { ok: false, error: 'invalid_hr_request' };
  }
  if (typeof payload.role !== 'string' || !payload.role.trim()) {
    return { ok: false, error: 'invalid_hr_request' };
  }
  let agentId;
  try {
    agentId = deriveAgentId(payload.module_scope, payload.role);
  } catch {
    return { ok: false, error: 'invalid_hr_request' };
  }
  return {
    ok: true,
    value: {
      action: body.action,
      target_agent: body.target_agent,
      intent: body.intent,
      payload: {
        message: typeof payload.message === 'string' ? payload.message : '',
        context: typeof payload.context === 'string' ? payload.context : '',
        module_scope: payload.module_scope.trim(),
        role: payload.role.trim().toLowerCase(),
      },
      agent_id: agentId,
      path: identityRepoPath(agentId),
    },
  };
}

export function validateHelgaIdentity(obj) {
  const details = [];
  if (!obj || typeof obj !== 'object') {
    return { ok: false, error: 'invalid_identity', details: ['body'] };
  }
  for (const key of ['role_title', 'department', 'system_prompt']) {
    if (typeof obj[key] !== 'string' || !obj[key].trim()) details.push(key);
  }
  if (!Array.isArray(obj.required_tools)) details.push('required_tools');
  if (!Array.isArray(obj.guardrails)) details.push('guardrails');
  if (details.length) {
    return { ok: false, error: 'invalid_identity', details };
  }
  return {
    ok: true,
    value: {
      role_title: obj.role_title.trim(),
      department: obj.department.trim(),
      system_prompt: obj.system_prompt,
      required_tools: obj.required_tools,
      guardrails: obj.guardrails,
    },
  };
}

export function buildPublicIdentity({ helga, agentId, npub, displayName, relay }) {
  return {
    agent_id: agentId,
    role_title: helga.role_title,
    department: helga.department,
    system_prompt: helga.system_prompt,
    required_tools: helga.required_tools,
    guardrails: helga.guardrails,
    nostr: {
      npub,
      display_name: displayName,
      relay: relay || RELAY_DEFAULT,
    },
  };
}

export function identityRepoPath(agentId) {
  return `agents/identities/${agentId}.json`;
}

export function parseGithubFileJson(base64Content) {
  const json = Buffer.from(base64Content, 'base64').toString('utf8');
  return JSON.parse(json);
}
```

- [ ] **Step 5: Run tests — expect PASS**

```cmd
cd agents\n8n-workflows\helga-create-identity
node --test helpers.test.mjs
```

Expected: all tests pass

- [ ] **Step 6: Commit**

```cmd
git add agents\n8n-workflows\helga-create-identity\package.json agents\n8n-workflows\helga-create-identity\helpers.mjs agents\n8n-workflows\helga-create-identity\helpers.test.mjs
git commit -F %TEMP%\commitmsg.txt
```

Message file contents:

```text
test: add Helga create-identity helper unit tests and pure JS helpers
```

(Write the message with a temp file first; do not use bash HEREDOC.)

---

### Task 2: Bake faker + Nostr signing deps into n8n image

**Files:**
- Modify: `infrastructure/n8n/Dockerfile`
- Modify: `infrastructure/n8n/README.md`

**Interfaces:**
- Consumes: existing Dockerfile `npm install` under `/home/node/.n8n/nodes`
- Produces: image with `@faker-js/faker`, `@noble/secp256k1`, `@noble/hashes`; README documents `NODE_FUNCTION_ALLOW_EXTERNAL`

- [ ] **Step 1: Update Dockerfile npm install line**

Replace the `npm install` packages line so it becomes:

```dockerfile
FROM n8nio/n8n:latest

USER root

# Community nodes + Code-node libraries for Helga HR workflow
RUN mkdir -p /home/node/.n8n/nodes \
    && cd /home/node/.n8n/nodes \
    && npm init -y \
    && npm install n8n-nodes-nostrobots@1.2.1 @noble/hashes@1.3.1 @noble/secp256k1@2.1.0 @faker-js/faker@9.3.0 nostr-tools@2.10.4 \
    && chown -R node:node /home/node/.n8n

USER node
```

Note: Code nodes resolve `require()` from n8n’s allow-list against packages available on the runtime; if Code resolution fails against `.n8n/nodes`, also document installing the same packages globally in the image under `/usr/local/lib/node_modules` **only if** runtime testing on Task 3 shows `require('@faker-js/faker')` fails — do not add a second install path preemptively (YAGNI). Prefer first verifying with:

```js
require('@faker-js/faker');
require('@noble/secp256k1');
require('@noble/hashes/sha256');
```

in a throwaway Code node after image rebuild.

- [ ] **Step 2: Document env + Helga workflow in README**

Append to `infrastructure/n8n/README.md` under Configuration (or Extension points):

```markdown
### Helga create-identity workflow

Import [`agents/n8n-workflows/helga-create-identity.json`](../../agents/n8n-workflows/helga-create-identity.json).

Host env (required for Code nodes):

```text
NODE_FUNCTION_ALLOW_EXTERNAL=faker,@faker-js/faker,@noble/secp256k1,@noble/hashes,nostr-tools
```

Baked packages (see Dockerfile): `n8n-nodes-nostrobots@1.2.1`, `@noble/hashes@1.3.1`, `@noble/secp256k1@2.1.0`, `@faker-js/faker@9.3.0`, `nostr-tools@2.10.4`.

Design: [`docs/superpowers/specs/2026-07-21-helga-create-identity-n8n-design.md`](../../docs/superpowers/specs/2026-07-21-helga-create-identity-n8n-design.md).

Helpers (unit-tested logic mirrored in Code nodes): [`agents/n8n-workflows/helga-create-identity/`](../../agents/n8n-workflows/helga-create-identity/).
```

Also add under Common mistakes:

```markdown
- Forgetting `NODE_FUNCTION_ALLOW_EXTERNAL` for Helga HR (symptoms: `faker_unavailable`)
- Committing employee `nsec` into `agents/identities/*.json`
```

- [ ] **Step 3: Commit**

```text
build: bake faker and noble secp256k1 into n8n image for Helga HR
```

---

### Task 3: Importable n8n workflow JSON

**Files:**
- Create: `agents/n8n-workflows/helga-create-identity.json`

**Interfaces:**
- Consumes: helper algorithms from Task 1 (inlined into Code nodes — n8n cannot import the `.mjs` file at runtime)
- Consumes: packages from Task 2
- Produces: webhook workflow named `Helga Create Identity`

**Credentials (placeholders in JSON):**
- GitHub API credential name placeholder: `GitHub AAE` (id `PLACEHOLDER_GITHUB`)
- No Flowise auth in JSON unless required; leave header empty with sticky note for API key
- Repo owner/name sticky defaults: `OWNER/REPO` → document as `neberg21/aae` (or current remote) to set after import

- [ ] **Step 1: Create workflow skeleton with sticky note**

Workflow `name`: `Helga Create Identity`

Sticky note content (exact):

```text
## Setup — Helga Create Identity
1. Rebuild/redeploy n8n image from infrastructure/n8n (faker + noble).
2. Set env NODE_FUNCTION_ALLOW_EXTERNAL=faker,@faker-js/faker,@noble/secp256k1,@noble/hashes
3. Create GitHub credential with contents:write on the AAE repo.
4. Set GitHub owner/repo in HTTP nodes (default target path agents/identities/{agent_id}.json).
5. Replace REPLACE_ME_HELGA_PREDICTION_URL with Flowise Helga prediction URL (https://flowise.neberg.de/...).
6. Activate workflow; copy Production webhook URL.
7. Never commit nsec. Secrets live in workflow static data only.

Spec: docs/superpowers/specs/2026-07-21-helga-create-identity-n8n-design.md
Relay: wss://nostr.neberg.de
```

- [ ] **Step 2: Add Webhook node**

- Type: `n8n-nodes-base.webhook`
- HTTP Method: `POST`
- Path: `helga-create-identity`
- Response mode: `responseNode` (Respond to Webhook later)
- On Error: continue / use IF branches for HTTP status via Respond nodes

- [ ] **Step 3: Code node `Parse HR Request`**

Inline the logic from `parseHrRequest` + `deriveAgentId` + `identityRepoPath` (copy from helpers.mjs). Output either:

```js
return [{ json: { ok: true, ...value } }];
```

or

```js
return [{ json: { ok: false, error: 'invalid_hr_request', httpStatus: 400 } }];
```

- [ ] **Step 4: IF `ok` — else Respond 400**

Respond body: `{{ { ok: false, error: $json.error } }}` with response code from `httpStatus` or 400.

- [ ] **Step 5: HTTP Request `GitHub GET identity`**

- Method: `GET`
- URL: `https://api.github.com/repos/{{$env.AAE_GITHUB_REPO || 'neberg21/aae'}}/contents/{{$json.path}}`
  - Prefer expression built from sticky-configured owner/repo fields stored in a Set node `Config` with `githubRepo: neberg21/aae` (no secrets).
- Headers: `Accept: application/vnd.github+json`, `Authorization: Bearer {{$credentials.githubApi.accessToken}}` (use n8n GitHub credential node type if available — prefer official GitHub node **Get File** if present in instance; otherwise HTTP + credential).
- Options: full response / never error on 404 — map 404 to `exists: false`

**Recommended:** use `n8n-nodes-base.github` File → Get if credential type exists; on 404 set `exists=false` via IF on error message / status.

Code node after GET:

```js
const status = $input.first().json.statusCode ?? $input.first().json.status;
if (status === 404) {
  return [{ json: { ...$('Parse HR Request').item.json, exists: false } }];
}
if (status >= 200 && status < 300) {
  const content = $input.first().json.content; // base64
  const parsed = JSON.parse(Buffer.from(content, 'base64').toString('utf8'));
  return [{
    json: {
      ok: true,
      already_exists: true,
      agent_id: parsed.agent_id,
      path: $('Parse HR Request').item.json.path,
      npub: parsed.nostr?.npub,
      display_name: parsed.nostr?.display_name,
      httpStatus: 200,
    },
  }];
}
return [{ json: { ok: false, error: 'github_get_failed', httpStatus: 502 } }];
```

- [ ] **Step 6: IF `already_exists` → Respond 200 with idempotent body; else continue**

- [ ] **Step 7: HTTP Request `Call Helga Flowise`**

- Method: `POST`
- URL: `REPLACE_ME_HELGA_PREDICTION_URL` (literal placeholder string in node — operator replaces after import)
- Body JSON:

```json
{
  "question": "{{$json.payload.message}}",
  "overrideConfig": {
    "vars": {
      "module_scope": "{{$json.payload.module_scope}}",
      "role": "{{$json.payload.role}}",
      "context": "{{$json.payload.context}}"
    }
  }
}
```

(Adjust to actual Flowise prediction shape when URL is known; sticky note says to align body with Flowise UI.)

On transport failure → Respond 502 `{ ok:false, error:"helga_unavailable" }`.

- [ ] **Step 8: Code `Validate Helga + inject agent_id`**

Parse Flowise text/JSON (handle stringified JSON in `text` / `json` fields). Run `validateHelgaIdentity`. On failure Respond 422. On success attach `helga` + `agent_id` from Parse HR Request (overwrite Helga’s agent_id).

- [ ] **Step 9: Code `Faker display name`**

```js
const { faker } = require('@faker-js/faker/locale/de');
try {
  const first = faker.person.firstName();
  const roleTitle = $json.helga.role_title;
  const display_name = `${first} (${roleTitle})`;
  return [{ json: { ...$json, display_name, first_name: first } }];
} catch (e) {
  return [{ json: { ok: false, error: 'faker_unavailable', httpStatus: 500, details: String(e) } }];
}
```

IF error → Respond 500.

- [ ] **Step 10: Code `Nostr keygen + kind-0 publish`**

Use `nostr-tools@2.10.4` for keygen/bech32/finalizeEvent (add it in Task 2 Dockerfile + allow-list together with faker). Publish over WebSocket to `wss://nostr.neberg.de`.

Code node sketch:

```js
const { generateSecretKey, getPublicKey, nip19 } = require('nostr-tools/pure');
const { finalizeEvent } = require('nostr-tools/pure');
const WebSocket = require('ws'); // may already exist in n8n image

const sk = generateSecretKey();
const pk = getPublicKey(sk);
const nsec = nip19.nsecEncode(sk);
const npub = nip19.npubEncode(pk);
const display_name = $json.display_name;
const about = `${$json.helga.department} · ${$json.helga.role_title}`;

const event = finalizeEvent({
  kind: 0,
  created_at: Math.floor(Date.now() / 1000),
  tags: [],
  content: JSON.stringify({ name: display_name, about }),
}, sk);

// Publish via WebSocket to wss://nostr.neberg.de: send ["EVENT", event], wait for OK
// On failure return { ok:false, error:'nostr_profile_failed', httpStatus:502 }
// On success return { ...$json, npub, nsec }  -- nsec only for next Code node then strip
```

Use a short timeout (10s). Do not log `nsec`.

If `ws` is unavailable in Code node, use HTTP `POST` to a relay that accepts NIP-98/HTTP — AAE relay is WebSocket-first; stick with WebSocket.

- [ ] **Step 11: Code `Store nsec in static data` + strip for downstream**

```js
const staticData = $getWorkflowStaticData('global');
staticData.employeeNsecs = staticData.employeeNsecs || {};
staticData.employeeNsecs[$json.agent_id] = $json.nsec;

const { nsec, ...rest } = $json;
return [{ json: { ...rest, nsec_stored: true } }];
```

- [ ] **Step 12: Code `Build public identity JSON`**

Use `buildPublicIdentity` logic; output `fileContent` string (pretty-printed JSON) and `path`.

- [ ] **Step 13: HTTP/GitHub `PUT create file`**

- Message: `hr: add identity {{$json.agent_id}}`
- Content: base64 of `fileContent`
- Branch: default (`main`)
- On failure → Respond 502 `{ ok:false, error:'github_commit_failed', npub, orphan_nostr_key:true }`

- [ ] **Step 14: Respond 200 created**

```json
{
  "ok": true,
  "already_exists": false,
  "agent_id": "...",
  "path": "...",
  "commit_sha": "...",
  "npub": "...",
  "display_name": "..."
}
```

Map `commit_sha` from GitHub PUT response `content.sha` or `commit.sha`.

- [ ] **Step 15: Sanity-check exported JSON**

- No real tokens
- Path `helga-create-identity`
- Sticky contains `REPLACE_ME_HELGA_PREDICTION_URL` and relay URL
- Search workflow file for `nsec` string usages: only in Code that writes static data / local vars, never in Respond bodies

- [ ] **Step 16: Commit**

```text
feat: add Helga create-identity n8n workflow
```

---

### Task 4: Process doc pointer

**Files:**
- Modify: `docs/process/erstelle_teamleiter.md`

**Interfaces:**
- Consumes: workflow path + spec path
- Produces: discoverability link for humans

- [ ] **Step 1: Append pointer**

Add at end of `docs/process/erstelle_teamleiter.md`:

```markdown

## Runtime (n8n)

Neue Identitäten werden per Webhook-Workflow angelegt:

- Workflow: [`agents/n8n-workflows/helga-create-identity.json`](../../agents/n8n-workflows/helga-create-identity.json)
- Design: [`docs/superpowers/specs/2026-07-21-helga-create-identity-n8n-design.md`](../superpowers/specs/2026-07-21-helga-create-identity-n8n-design.md)

`agent_id` = `{domain}-{role}` (z. B. `finanzen-teamleiter`). Nostr-Profilname: `{Vorname} ({role_title})`.
```

- [ ] **Step 2: Commit**

```text
docs: link Helga create-identity n8n workflow from teamleiter process
```

---

### Task 5: Manual verification checklist (no new code)

**Files:** none (ops checklist)

- [ ] **Step 1: Rebuild n8n image and deploy** (operator) with Task 2 Dockerfile + env allow-list

- [ ] **Step 2: Import workflow**, set GitHub credential, replace Helga URL (or temporarily stub Helga HTTP with a Code node returning fixed valid Helga JSON if Flowise flow missing — local-only; do not commit the stub)

- [ ] **Step 3: POST create**

```cmd
curl -s -X POST https://n8n.neberg.de/webhook/helga-create-identity -H "Content-Type: application/json" -d "{\"action\":\"route_message\",\"target_agent\":\"@Helga\",\"intent\":\"hr_request\",\"payload\":{\"message\":\"Create Teamleiter Finanzen\",\"context\":\"\",\"module_scope\":\"Module.Finanzen\",\"role\":\"teamleiter\"}}"
```

Expected: `ok:true`, `already_exists:false`, `agent_id":"finanzen-teamleiter"`, `npub` present; GitHub file created without `nsec`.

- [ ] **Step 4: POST same again**

Expected: `already_exists:true`; no new Nostr key.

- [ ] **Step 5: POST researcher**

`role: researcher` → `finanzen-researcher.json`

- [ ] **Step 6: Confirm kind-0** on `wss://nostr.neberg.de` for the returned `npub` (name matches `display_name`)

---

## Plan self-review

| Spec requirement | Task |
|------------------|------|
| Webhook Leo envelope | Task 3 |
| Deterministic `{domain}-{role}` | Task 1 + 3 |
| Idempotent GitHub GET | Task 3 |
| Flowise Helga call + schema validate | Task 3 |
| Faker `de` display name | Task 2 + 3 |
| Nostr keygen + kind-0 as employee | Task 2 + 3 |
| `nsec` in n8n static data only | Task 3 |
| GitHub PUT JSON seed | Task 3 |
| Error codes 400/422/502/500 | Task 3 |
| Dockerfile + README | Task 2 |
| Process pointer | Task 4 |
| Manual test plan | Task 5 |
| No Helga Flowise authoring / no Leo wiring | Non-goals honored |

**Placeholder scan:** Helga URL intentionally `REPLACE_ME_HELGA_PREDICTION_URL`. GitHub repo default `neberg21/aae` via Config Set node — adjust if remote differs.

**Type consistency:** `agent_id`, `path`, `already_exists`, `display_name`, `npub`, error strings match the spec.

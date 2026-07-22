# AAE Agent Identities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace stale German identity markdown with English canonical system prompts aligned to the four n8n think-workflows.

**Architecture:** Prompt-body identity files under `agents/identities/` with a sync header pointing at the matching `*-think.json`. Fixed agents (`leo`, `helga`) plus templates for supervisors and specialists. n8n workflow JSON is not edited in this plan.

**Tech Stack:** Markdown identity prompts; verification via `rg` / `cmd` string checks (no app runtime)

**Spec:** [`docs/superpowers/specs/2026-07-22-aae-agent-identities-design.md`](../specs/2026-07-22-aae-agent-identities-design.md)

## Global Constraints

- Language: English only in identity bodies
- Purpose: canonical system prompts (no divergent handbook appendix)
- Naming: `supervisor-*` only — never `teamleiter` / `Teamleiter`
- Forbidden obsolete patterns: Flowise, Nostr-as-channel, `"action": "route_message"`, `required_tools`, one-shot “never clarify” for Helga
- Every file must include the sync header (`agentId`, `workflow`, `webhook`, `status`)
- Do not edit `agents/n8n-workflows/*-think.json` in this plan
- Do not edit backend DTOs or `agents/workflow.md`
- Windows scripting: no PowerShell/bash scripts; use `cmd /c` for one-offs; commits via `%TEMP%\commitmsg.txt` if multi-line
- Response language / commit messages: English
- Only commit when the user asked to commit in the conversation, or when executing a plan step that the user approved for execution including commits

---

## File Structure

| File | Responsibility |
|------|----------------|
| `agents/identities/leo.md` | Canonical Leo CEO orchestrator prompt + delegations JSON schema |
| `agents/identities/helga.md` | Canonical Helga HR prompt + ready/clarify identity JSON schema |
| `agents/identities/template_supervisor.md` | Domain supervisor template (replaces old teamleiter template) |
| `agents/identities/template_specialist.md` | Specialist worker template |
| `agents/identities/template_domain-supervisor.md` | Delete after `template_supervisor.md` exists |
| `docs/superpowers/specs/2026-07-22-aae-agent-identities-design.md` | Mark Status Approved when plan execution starts (optional housekeeping) |

---

### Task 1: Rewrite `leo.md`

**Files:**
- Modify: `agents/identities/leo.md` (full replace)
- Test: repo root string checks via `rg`

**Interfaces:**
- Consumes: Leo output schema from think-workflow design / `leo-think.json` Code prompt
- Produces: English canonical Leo prompt with `delegations[]` schema

- [ ] **Step 1: Write the full file**

Replace `agents/identities/leo.md` with exactly:

```markdown
---
agentId: leo
workflow: agents/n8n-workflows/leo-think.json
webhook: /webhook/leo-think
status: canonical-prompt — keep in sync with workflow systemMessage + Code prompt schema
---

You are Leo, CEO orchestrator of the Autonomous Agent Ecosystem (AAE).

You are the first contact for the human user. You understand visions, assign work at department level, and delegate. You never write code and never create files. You never address specialists directly — only supervisors and Helga.

## Runtime inputs

The workflow injects: `userVision`, `chatHistory`, `threadId`.

## Duties

1. Analyze the vision and identify domain / module scope (for example Finanzen → `Module.Finanzen`).
2. If no supervisor exists for that domain, send an `hr_request` to `helga`.
3. If a supervisor exists, send a `delegation` with the vision, architectural bounds, and module scope.
4. Use chat history to monitor progress. Do not call CI or GitHub tools yourself.

## Hard rules

- Never write code or create files.
- Never use the word teamleiter; use `supervisor-*` agent ids and `helga`.
- Features belong in isolated modules: `backend/src/Module.[Name]/` and matching frontend module paths. Core bootstrap and `Program.cs` are taboo.
- Reply with JSON only. No markdown fences. No prose outside JSON.

## Output schema

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

Each delegation becomes one backend `POST /api/agents/route-chat-message` with `senderAgentId` `leo`.
```

- [ ] **Step 2: Verify Leo content**

Run from repo root:

```cmd
rg -n "teamleiter|Teamleiter|Flowise|route_message|Nostr" agents\identities\leo.md
rg -n "delegations|supervisor-|hr_request|leo-think" agents\identities\leo.md
```

Expected: first command finds **no matches**. Second command finds matches for schema/sync terms.

- [ ] **Step 3: Commit (only if user approved commits for this execution)**

```cmd
git add agents/identities/leo.md
(
echo rewrite leo identity as English canonical think-workflow prompt
) > %TEMP%\commitmsg.txt
git commit -F %TEMP%\commitmsg.txt
del %TEMP%\commitmsg.txt
```

---

### Task 2: Rewrite `helga.md`

**Files:**
- Modify: `agents/identities/helga.md` (full replace)
- Test: `rg` string checks

**Interfaces:**
- Consumes: Helga decision schema from think-workflow design / `helga-think.json`
- Produces: English Helga prompt with `needs_clarification` | `ready` and `CreateIdentityRequest`-compatible identity fields (`tools`, not `required_tools`)

- [ ] **Step 1: Write the full file**

Replace `agents/identities/helga.md` with exactly:

```markdown
---
agentId: helga
workflow: agents/n8n-workflows/helga-think.json
webhook: /webhook/helga-think
status: canonical-prompt — keep in sync with workflow systemMessage + Code prompt schema
---

You are Helga, HR director and identity forge of the Autonomous Agent Ecosystem (AAE).

You recruit and shape digital workers (supervisors and specialists). You never write application code (.NET, React, etc.). You never build or wire workflows.

## Runtime inputs

The workflow injects: `delegationRequest`, `chatHistory`, `threadId`.

`delegationRequest` may include a free-text message plus fields such as `moduleScope` and `role`.

## Duties

1. If the request is underspecified, set `status` to `needs_clarification` and put open questions in `clarificationQuestions` (shown to the user).
2. If ready, set `status` to `ready` and fill `identity` completely.
3. When writing `systemPrompt`, `guardrails`, and `tools` for new agents, follow `agents/identities/template_supervisor.md` or `agents/identities/template_specialist.md` structure.

## Hard rules

- Never write executable application code.
- Never use the word teamleiter; use `supervisor-*` ids.
- Clarification is allowed when needed.
- Infer sensible defaults from module scope + role when details are missing but still sufficient to create.
- Reply with JSON only. No markdown fences. No prose outside JSON.

## Output schema

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

## Backend mapping when ready

| Your field | CreateIdentityRequest |
|------------|------------------------|
| `roleTitle` | `jobTitle` |
| summary from request + department | `jobDescription` |
| `department` | `department` |
| `managerId` | `managerId` |
| `systemPrompt` | `systemPrompt` |
| `guardrails` | `guardrails` |
| `tools` | `tools` |

`needs_clarification` becomes `route-chat-message` with `targetAgentId` `User`.
```

- [ ] **Step 2: Verify Helga content**

```cmd
rg -n "teamleiter|Teamleiter|Flowise|route_message|required_tools|never ask|niemals.*[Rr]ückfragen" agents\identities\helga.md
rg -n "needs_clarification|CreateIdentityRequest|helga-think|\"tools\"" agents\identities\helga.md
```

Expected: first command finds **no matches**. Second finds clarification + tools + workflow sync terms.

- [ ] **Step 3: Commit (only if user approved commits for this execution)**

```cmd
git add agents/identities/helga.md
(
echo rewrite helga identity for clarify-or-create think-workflow contract
) > %TEMP%\commitmsg.txt
git commit -F %TEMP%\commitmsg.txt
del %TEMP%\commitmsg.txt
```

---

### Task 3: Add `template_supervisor.md` and delete old teamleiter template

**Files:**
- Create: `agents/identities/template_supervisor.md`
- Delete: `agents/identities/template_domain-supervisor.md`
- Test: `rg` string checks; confirm old file gone

**Interfaces:**
- Consumes: Supervisor decision schema from `supervisor-think.json` / design
- Produces: Domain-templated supervisor prompt with `waiting|delegate|done`

- [ ] **Step 1: Create `template_supervisor.md`**

Write `agents/identities/template_supervisor.md` with exactly:

```markdown
---
agentId: supervisor-{{domain_kebab}}
workflow: agents/n8n-workflows/supervisor-think.json
webhook: /webhook/supervisor-think
status: canonical-prompt — keep in sync with workflow systemMessage + Code prompt schema
---

You are the domain supervisor (Scrum Master / architect) for **{{Domain_Name}}** in the Autonomous Agent Ecosystem (AAE).

You report to Leo. You lead specialists. You plan work, delegate isolated tasks, track milestones with GitHub tools, and request human approval when the package is done. Prefer not to write code yourself.

## Placeholders

- `{{Domain_Name}}` — Pascal domain (example: `Finanzen`)
- `{{domain_name}}` — frontend folder slug (example: `finanzen`)
- `{{domain_kebab}}` — agent id slug (example: `finanzen` → `supervisor-finanzen`)

## Isolation

Work only inside:

- Backend: `backend/src/Module.{{Domain_Name}}/`
- Frontend: `frontend/src/modules/{{domain_name}}/`

Never change core bootstrap or `Program.cs`. If global resources are required, escalate to Leo.

## Runtime inputs

The workflow injects: `taskContext`, `subordinatesList`, `chatHistory`, `senderAgentId`, `threadId`.

## Tools

You may use only these GitHub tools (as wired in the workflow):

- `create_github_issue`
- `update_issue_status`
- `add_issue_comment`

## Duties

1. Break Leo’s assignment into concrete technical tasks.
2. Delegate to specialists listed in `subordinatesList` (or known specialists for this domain).
3. If a required specialist is missing, escalate hiring via Leo → Helga (`hr_request`). Do not invent obsolete routing envelopes.
4. When blocked on fan-in, choose `waiting`.
5. When the deliverable is ready for human review, choose `done` with approval content.

## Hard rules

- Never use the word teamleiter; you are a supervisor.
- Never address the human user as a specialist peer channel; outcomes drive backend routing.
- Reply with JSON only after any optional tool use. No markdown fences.

## Output schema

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
| `waiting` | `route-chat-message` with `targetAgentId` null |
| `delegate` | one `route-chat-message` per specialist target |
| `done` | `POST /api/await-request-approval` |
```

- [ ] **Step 2: Delete the obsolete template**

```cmd
del agents\identities\template_domain-supervisor.md
```

Expected: file removed.

- [ ] **Step 3: Verify**

```cmd
rg -n "teamleiter|Teamleiter|Flowise|route_message|Orchestrator \| @Helga" agents\identities\template_supervisor.md
rg -n "waiting\|delegate\|done|supervisor-think|create_github_issue" agents\identities\template_supervisor.md
dir agents\identities\template_domain-supervisor.md
```

Expected: first `rg` finds **no matches**; second finds outcome/tools/sync terms; `dir` reports file not found.

- [ ] **Step 4: Commit (only if user approved commits for this execution)**

```cmd
git add agents/identities/template_supervisor.md
git add agents/identities/template_domain-supervisor.md
(
echo replace domain teamleiter template with supervisor think-workflow prompt
) > %TEMP%\commitmsg.txt
git commit -F %TEMP%\commitmsg.txt
del %TEMP%\commitmsg.txt
```

---

### Task 4: Add `template_specialist.md`

**Files:**
- Create: `agents/identities/template_specialist.md`
- Test: `rg` string checks

**Interfaces:**
- Consumes: Specialist done schema from `specialist-think.json` / design
- Produces: Specialist template with `execute_tool` + `{ "content": "..." }`

- [ ] **Step 1: Create the file**

Write `agents/identities/template_specialist.md` with exactly:

```markdown
---
agentId: specialist-{{role_kebab}}
workflow: agents/n8n-workflows/specialist-think.json
webhook: /webhook/specialist-think
status: canonical-prompt — keep in sync with workflow systemMessage + Code prompt schema
---

You are a tool-agnostic specialist worker in the Autonomous Agent Ecosystem (AAE).

Your job is to complete the assigned task using only the backend tool gateway, then report a concise result to your manager.

## Placeholders

- `{{role_kebab}}` — specialist id slug (example: `react` → `specialist-react`)
- `{{Domain_Name}}` / `{{domain_name}}` — when domain-scoped, stay inside that module

## Runtime inputs

The workflow injects: `taskContext`, `chatHistory`, `allowedTools`, `managerId`, `senderAgentId`, `threadId`.

## Tools

- Use only the `execute_tool` tool.
- Never call a tool name outside the hard `allowedTools` allowlist.
- Never access the filesystem or GitHub directly from this role.

## Isolation (when domain-scoped)

- Backend: `backend/src/Module.{{Domain_Name}}/`
- Frontend: `frontend/src/modules/{{domain_name}}/`
- Never change core bootstrap or `Program.cs`.

## Hard rules

- Never use the word teamleiter.
- Do not message Leo or Helga directly; finish by reporting to `managerId`.
- Reply with JSON only when finished. No markdown fences.

## Output schema

```json
{ "content": "result summary for manager" }
```

The workflow maps this to `route-chat-message` with `targetAgentId` set to `managerId`.
```

- [ ] **Step 2: Verify**

```cmd
rg -n "teamleiter|Teamleiter|Flowise|filesystem tool|route_message" agents\identities\template_specialist.md
rg -n "execute_tool|allowedTools|managerId|specialist-think" agents\identities\template_specialist.md
```

Expected: first finds **no matches**; second finds tool/allowlist/sync terms.

- [ ] **Step 3: Commit (only if user approved commits for this execution)**

```cmd
git add agents/identities/template_specialist.md
(
echo add specialist identity template for think-workflow contract
) > %TEMP%\commitmsg.txt
git commit -F %TEMP%\commitmsg.txt
del %TEMP%\commitmsg.txt
```

---

### Task 5: Folder-wide regression check + mark spec approved

**Files:**
- Modify: `docs/superpowers/specs/2026-07-22-aae-agent-identities-design.md` (Status line only)
- Test: folder-wide `rg`

**Interfaces:**
- Consumes: all four identity files from Tasks 1–4
- Produces: confirmed clean `agents/identities/` set + approved spec status

- [ ] **Step 1: Update spec status**

In `docs/superpowers/specs/2026-07-22-aae-agent-identities-design.md`, change:

```markdown
**Status:** Pending user review
```

to:

```markdown
**Status:** Approved
```

- [ ] **Step 2: List identity files**

```cmd
dir /b agents\identities
```

Expected exactly:

```text
helga.md
leo.md
template_specialist.md
template_supervisor.md
```

(order may vary)

- [ ] **Step 3: Forbidden-pattern sweep**

```cmd
rg -n -i "teamleiter|flowise|route_message|required_tools" agents\identities
```

Expected: **no matches**.

- [ ] **Step 4: Required sync headers present**

```cmd
rg -n "canonical-prompt|n8n-workflows/.*-think\.json|/webhook/" agents\identities
```

Expected: each of the four files contributes header hits.

- [ ] **Step 5: Commit (only if user approved commits for this execution)**

```cmd
git add agents/identities docs/superpowers/specs/2026-07-22-aae-agent-identities-design.md
(
echo finish agent identity rewrite and mark identities design approved
) > %TEMP%\commitmsg.txt
git commit -F %TEMP%\commitmsg.txt
del %TEMP%\commitmsg.txt
```

---

## Plan self-review

1. **Spec coverage:** Leo, Helga, supervisor template, specialist template, delete old template, English, sync headers, no n8n JSON edits — all have tasks.
2. **Placeholders:** none; full file bodies inlined.
3. **Consistency:** schemas match think-workflow design (`delegations`, Helga status/identity, supervisor outcomes, specialist `content`).

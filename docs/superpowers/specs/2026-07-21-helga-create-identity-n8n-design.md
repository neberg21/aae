# Helga create-identity n8n workflow — Design

**Date:** 2026-07-21  
**Status:** Approved for implementation planning  
**Deliverable:** Versioned n8n workflow that hires a new AAE agent identity (Helga JSON + Nostr employee account + git seed file)

## Goal

When Leo (or a caller) posts an `hr_request` to n8n, the workflow:

1. Derives a deterministic `agent_id`
2. Short-circuits if that identity already exists (idempotent)
3. Otherwise invokes Helga in Flowise for the identity profile
4. Provisions a dedicated Nostr account (keypair + kind-0 profile with a human display name)
5. Persists public identity JSON under `agents/identities/{agent_id}.json` on the default branch via GitHub Contents API
6. Stores `nsec` only in n8n (never in git or webhook responses)

## Decisions (locked)

| Topic | Choice |
|-------|--------|
| Shape | Single linear n8n workflow (Approach 1) |
| Trigger | Webhook only; Leo routing JSON envelope |
| Persist | Git seed files under `agents/identities/` |
| Approval | Auto-commit to default branch after validation (no HITL/PR) |
| File format | `.json` matching Helga schema + public `nostr` block |
| `agent_id` | n8n-owned: `{domain}-{role}` from `module_scope` + `payload.role` |
| Idempotency | GitHub GET first; if file exists → return existing, no Helga/Nostr/write |
| Display name | `{FirstName} ({role_title})` e.g. `Max (Teamleiter Finanzen)` |
| Name source | `@faker-js/faker` with **de** locale |
| Nostr | Generate keypair; sign+publish kind-0 as the new employee to `wss://nostr.neberg.de`; `npub` in JSON; `nsec` in n8n Data Store (not shared Nostrobots credential) |

## Architecture

```text
Webhook (Leo hr_request)
    → Validate envelope + required payload.role + module_scope
    → Derive agent_id = "{domain}-{role}"
    → GitHub GET agents/identities/{agent_id}.json
         exists → 200 { ok, already_exists: true, agent_id, path, nostr }
         missing ↓
    → HTTP POST Flowise Helga prediction
    → Parse + schema-validate identity JSON (strip/ignore Helga agent_id)
    → Inject agent_id from n8n
    → Code: faker de first name → display_name
    → Code: generate Nostr keypair; sign+publish kind-0 as the new employee
    → Store nsec in n8n Data Store (key = agent_id)
    → GitHub PUT create file (no nsec in body)
    → 200 { ok, agent_id, path, commit_sha, npub, display_name }
```

**Boundaries**

- n8n: routing, id derivation, validation, Nostr provision, GitHub write, secret storage
- Flowise: Helga LLM only (system prompt from `agents/identities/helga.md`)
- Git: public identity source of truth for this slice
- Out of scope: Helga Flowise flow authoring, Leo→webhook wiring, HITL, key rotation/deletion, overwrite of existing identities

## Contracts

### Webhook input

```json
{
  "action": "route_message",
  "target_agent": "@Helga",
  "intent": "hr_request",
  "payload": {
    "message": "Create a domain supervisor for Finanzen",
    "context": "...",
    "module_scope": "Module.Finanzen",
    "role": "teamleiter"
  }
}
```

**Validation**

- `target_agent` must be `@Helga`
- `intent` must be `hr_request`
- `payload.module_scope` required (e.g. `Module.Finanzen` → domain slug `finanzen`)
- `payload.role` required (kebab-case: `teamleiter`, `researcher`, `backend`, `frontend`, …)

**`agent_id` examples**

| module_scope | role | agent_id |
|--------------|------|----------|
| `Module.Finanzen` | `teamleiter` | `finanzen-teamleiter` |
| `Module.Finanzen` | `researcher` | `finanzen-researcher` |
| `Module.Dnd` | `backend` | `dnd-backend` |

### Helga output (validated before Nostr/GitHub)

Required fields: `role_title`, `department`, `system_prompt`, `required_tools` (array), `guardrails` (array).  
Helga may emit `agent_id`; n8n **overwrites** it with the derived id.

### Committed file `agents/identities/{agent_id}.json`

Helga fields plus:

```json
"nostr": {
  "npub": "...",
  "display_name": "Max (Teamleiter Finanzen)",
  "relay": "wss://nostr.neberg.de"
}
```

Never include `nsec`.

### Success responses

**Created**

```json
{
  "ok": true,
  "already_exists": false,
  "agent_id": "finanzen-teamleiter",
  "path": "agents/identities/finanzen-teamleiter.json",
  "commit_sha": "...",
  "npub": "...",
  "display_name": "Klaus (Teamleiter Finanzen)"
}
```

**Idempotent hit**

```json
{
  "ok": true,
  "already_exists": true,
  "agent_id": "finanzen-teamleiter",
  "path": "agents/identities/finanzen-teamleiter.json",
  "npub": "...",
  "display_name": "..."
}
```

## Components (n8n nodes)

1. Webhook  
2. Validate envelope + derive `agent_id` (Code / IF)  
3. GitHub GET contents for path  
4. IF exists → respond already_exists  
5. HTTP Request → Flowise Helga (prediction URL placeholder in sticky note)  
6. Parse + schema validate; inject `agent_id`  
7. Code: `@faker-js/faker` (`de`) → first name; `display_name = "{first} ({role_title})"`  
8. Code: generate Nostr keypair (`npub` / `nsec`)  
9. Code (or HTTP): build + sign kind-0 with the **new** `nsec`, publish to `wss://nostr.neberg.de` — do **not** use a shared Nostrobots credential (that would publish as Leo/ops, not the employee)  
10. n8n Data Store: save `nsec` keyed by `agent_id`  
11. GitHub PUT create file on default branch  
12. Respond success JSON  

**Domain slug rule:** from `payload.module_scope`, strip a leading `Module.` (case-insensitive), then lowercase → e.g. `Module.Finanzen` → `finanzen`, `Module.Dnd` → `dnd`. `agent_id = "{domain}-{role}"` with `role` already kebab-case.

**Host prerequisites**

- Live: `https://n8n.neberg.de`, `https://flowise.neberg.de`, `wss://nostr.neberg.de`
- Credentials: GitHub (contents:write on the AAE repo), Flowise API if required
- Nostr libraries in Code runtime as needed to generate keys and sign kind-0 (e.g. allow-listed nostr tooling / `@noble/*`); shared `n8n-nodes-nostrobots` credential is **not** required for this workflow
- Env: allow Code node external modules for faker and Nostr signing deps, e.g. `NODE_FUNCTION_ALLOW_EXTERNAL=faker,@faker-js/faker,...` (exact list documented in sticky note + `infrastructure/n8n/README.md`)
- Package: ensure `@faker-js/faker` (and chosen Nostr signing deps) are available to the n8n Code runtime (image install or documented host setup)

## Error handling

| Failure | HTTP | Body |
|---------|------|------|
| Bad envelope / missing role or module_scope | 400 | `{ ok: false, error: "invalid_hr_request" }` |
| Flowise/Helga down or non-JSON | 502 | `{ ok: false, error: "helga_unavailable" }` |
| Schema validation fails | 422 | `{ ok: false, error: "invalid_identity", details }` |
| Faker / external module blocked | 500 | `{ ok: false, error: "faker_unavailable" }` |
| Nostr kind-0 fails | 502 | `{ ok: false, error: "nostr_profile_failed" }` — no GitHub write |
| GitHub PUT fails after Nostr success | 502 | `{ ok: false, error: "github_commit_failed", npub, orphan_nostr_key: true }` — nsec kept in Data Store for cleanup |

Webhook responses and downstream item fields must never include `nsec`. Code nodes must not pass `nsec` into nodes that log full items unnecessarily.

## Testing

1. Import `agents/n8n-workflows/helga-create-identity.json` on n8n.neberg.de; fill credentials and Helga prediction URL  
2. POST `Module.Finanzen` + `role: teamleiter` → new `finanzen-teamleiter.json`, kind-0 name like `Klaus (Teamleiter Finanzen)`, response has `npub`  
3. POST same payload again → `already_exists: true`, no new commit / no new key  
4. POST `role: researcher` → `finanzen-researcher.json`  
5. Confirm committed JSON has no `nsec`

## Repo deliverables

- `agents/n8n-workflows/helga-create-identity.json` — importable workflow with setup sticky notes  
- Update `infrastructure/n8n/README.md` — faker allow-list / package note  
- Optional: one-line pointer from `docs/process/erstelle_teamleiter.md` to this workflow (only if needed for discoverability)

## Non-goals

- Building the Helga Flowise chat/agent flow UI definition in this slice (URL is a placeholder until that flow exists)  
- Automatic Leo dispatcher calling this webhook  
- PR / human approval before commit  
- Overwriting or deleting identities  
- Nostr key rotation or Data Store backup strategy beyond “store by agent_id”

## References

- [`agents/identities/helga.md`](../../../agents/identities/helga.md)  
- [`docs/process/erstelle_teamleiter.md`](../../process/erstelle_teamleiter.md)  
- [`docs/deployed-services.md`](../../deployed-services.md)  
- [`infrastructure/n8n/README.md`](../../../infrastructure/n8n/README.md)  
- Existing workflow pattern: [`agents/n8n-workflows/nostr-dm-listener.json`](../../../agents/n8n-workflows/nostr-dm-listener.json)  

# Helga Flowise Agentflow V2 — Design

**Date:** 2026-07-21  
**Status:** Approved for implementation planning  
**Deliverable:** Versioned, importable Flowise Agentflow V2 for Helga (LLM brain only)

## Goal

Ship an importable Flowise flow so n8n’s Helga create-identity workflow can call a real prediction URL on `https://flowise.neberg.de`. The flow is Helga’s LLM only; all HR wiring stays in n8n.

## Decisions (locked)

| Topic | Choice |
|-------|--------|
| Flow type | Agentflow V2 (`AGENTFLOW`) |
| Canvas shape | Minimal: Start → Agent (no tools) |
| Model | OpenAI via Agent node `agentModel: chatOpenAI`; credential empty for attach-on-import |
| System prompt in JSON | Exact placeholder: `hol das aktuelle prompt aus dem repo` |
| Canonical prompt source | `agents/identities/helga.md` (paste into Flowise after import; not auto-loaded) |
| Tools / GitHub / Nostr | None in Flowise — owned by n8n |
| Memory | Off / ephemeral enough for one-shot prediction from n8n |
| Repo path | `agents/flowise-workflows/helga.json` |

## Architecture

```text
n8n (helga-create-identity)
  → HTTP POST Flowise /api/v1/prediction/{chatflowId}
       → Agentflow V2: Start → Agent (chatOpenAI + system prompt)
       ← identity JSON text (Helga schema)
  → n8n validates, Nostr, GitHub, secrets
```

**Boundaries (unchanged from Helga n8n design)**

- n8n: routing, `agent_id`, validation, Nostr, GitHub, secret storage
- Flowise: Helga LLM only
- Prompt truth in git: `agents/identities/helga.md`
- Prompt in Flowise UI: operator pastes from that file (placeholder in export reminds them)

## Flow canvas (Agentflow V2)

| Node | Type name | Role |
|------|-----------|------|
| Start | `startAgentflow` | Chat input; entry for prediction API |
| Agent | `agentAgentflow` | `agentModel` = `chatOpenAI`; `agentMessages` system content = placeholder; no tools |

In Flowise 3.1 Agentflow V2, the chat model is configured **on** the Agent node (`agentModel` / `agentModelConfig`), not as a separate ChatOpenAI canvas node.

**System message (locked text)**

```text
hol das aktuelle prompt aus dem repo
```

## Import / ops contract

1. Import `agents/flowise-workflows/helga.json` into Flowise (`https://flowise.neberg.de`).
2. Attach OpenAI credential on the Agent model config.
3. Replace the system message placeholder with the contents of `agents/identities/helga.md`.
4. Save; copy prediction URL into n8n sticky / `REPLACE_ME_HELGA_PREDICTION_URL`.

Do not commit OpenAI API keys or Flowise API keys.

## Repo deliverables

- `agents/flowise-workflows/helga.json` — importable Agentflow V2 (nodes + edges; Flowise 3.1.1 compatible)
- Short pointer in `infrastructure/flowise/README.md` to that file (import path + reminder to paste `helga.md`)

## Non-goals

- Auto-fetching `helga.md` from git at runtime
- Tools, memory windows, or business side-effects inside Flowise
- Changing the n8n Helga create-identity workflow beyond documenting the prediction URL once the flow exists
- Authoring Leo or other agent flows in this slice

## References

- [`docs/superpowers/specs/2026-07-21-helga-create-identity-n8n-design.md`](2026-07-21-helga-create-identity-n8n-design.md) — n8n owns wiring; Flowise is Helga LLM only
- [`docs/deployed-services.md`](../../deployed-services.md) — `flowise.neberg.de`
- [`agents/identities/helga.md`](../../../agents/identities/helga.md) — canonical system prompt
- [`infrastructure/flowise/README.md`](../../../infrastructure/flowise/README.md) — Flowise packaging / import guidance
- Flowise image: `flowiseai/flowise:3.1.1`

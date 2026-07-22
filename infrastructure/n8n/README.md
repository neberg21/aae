# n8n (AAE)

## Integration Guide

### What it does

Self-hosted n8n image with the Nostr community node preinstalled. Live instance: **https://n8n.neberg.de**. Canonical host table: [`docs/deployed-services.md`](../../docs/deployed-services.md).

### Prerequisites

- Deployed n8n at `https://n8n.neberg.de` (or local image from this Dockerfile)
- Nostr credential (nsec/hex) for `n8n-nodes-nostrobots`
- AAE relay: `wss://nostr.neberg.de`

### Setup

1. Open **https://n8n.neberg.de** (or Settings → Community Nodes on a fresh install and install `n8n-nodes-nostrobots` if not using this image).
2. Create credential type **Nostrobots API** with the private Nostr key (nsec or hex).
3. Import a workflow from [`agents/n8n-workflows/`](../../agents/n8n-workflows/) (Import from File).
4. Select the credential on all Nostr nodes; set relay to `wss://nostr.neberg.de` (add other relays only if intentional).
5. Replace the placeholder logic node; activate the workflow.

Polling DM path runs about every 2 minutes (NIP-04). Dedupe in the Code node avoids double-processing.

### Configuration

| Item | Value |
|------|--------|
| Base URL | `https://n8n.neberg.de` |
| Webhooks | `https://n8n.neberg.de/webhook/...` |
| Flowise calls | `https://flowise.neberg.de` + flow path |
| Nostr relay | `wss://nostr.neberg.de` |

Image installs `n8n-nodes-nostrobots@1.2.1`, `@noble/hashes@1.3.1`, `@noble/secp256k1@2.1.0`, `@faker-js/faker@9.3.0`, `nostr-tools@2.10.4`, and `ws@8.18.0` under `/home/node/.n8n/nodes`. The image sets `NODE_PATH=/home/node/.n8n/nodes/node_modules` so the JS task runner can `require()` those packages (community-node path alone is not enough).

### Think workflows (Leo / Helga / Supervisor / Specialist)

Import these four files from [`agents/n8n-workflows/`](../../agents/n8n-workflows/):

| File | Webhook |
|------|---------|
| `leo-think.json` | `/webhook/leo-think` |
| `helga-think.json` | `/webhook/helga-think` |
| `supervisor-think.json` | `/webhook/supervisor-think` |
| `specialist-think.json` | `/webhook/specialist-think` |

After import:

1. Attach an **OpenAI** credential on every Chat Model node
2. On Supervisor: attach **GitHub** credentials and replace `OWNER/REPO` in tool URLs
3. Activate workflows
4. Follow smoke tests in [`agents/n8n-workflows/VERIFY.md`](../../agents/n8n-workflows/VERIFY.md)

Backend callbacks use hardcoded base URL `https://ai.neberg.de` (camelCase bodies). Specialist depends on `/api/agents/execute-tool`; Supervisor done-path calls `/api/await-request-approval` (backend may still stub those).

Helpers (unit-tested, mirrored in Code nodes): [`agents/n8n-workflows/think-helpers/`](../../agents/n8n-workflows/think-helpers/).

Design: [`docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md`](../../docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md).

### Common mistakes

- Leaving example relays (`damus`, `nos.lol`, …) instead of `wss://nostr.neberg.de`
- Expecting realtime DMs from the poll path (it is schedule-based)
- Committing real credentials into workflow JSON
- Forgetting to attach OpenAI (or GitHub for Supervisor) after import
- Expecting Wait / long-running HITL inside n8n — approvals are backend-owned
- Committing employee `nsec` into `agents/identities/*.json`

## Implementation Details

### Runtime flow

See [`docs/deployed-services.md`](../../docs/deployed-services.md) §4 for Nostr → n8n → app sequencing. HITL is backend-owned (no n8n Wait nodes); see [`docs/process/human-in-the-loop.md`](../../docs/process/human-in-the-loop.md).

### Key types

| Artifact | Responsibility |
|----------|----------------|
| [`Dockerfile`](Dockerfile) | Bake community Nostr nodes into `n8nio/n8n` |
| Workflow JSON under `agents/n8n-workflows/` | Versioned importable graphs |

### Extension points

- Add community packages in the Dockerfile `npm install` step
- Version workflows in `agents/n8n-workflows/`

### Limitations

- NIP-04 leaks sender/recipient metadata; content only is encrypted
- Event trigger path for mentions is vendor-marked BETA/experimental

### References

- Deployed hosts: [`docs/deployed-services.md`](../../docs/deployed-services.md)
- Infrastructure overview: [`../README.md`](../README.md)

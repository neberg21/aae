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

Image installs `n8n-nodes-nostrobots@1.2.1`, `@noble/hashes@1.3.1`, `@noble/secp256k1@2.1.0`, `@faker-js/faker@9.3.0`, and `nostr-tools@2.10.4` under `/home/node/.n8n/nodes`.

### Helga create-identity workflow

Import [`agents/n8n-workflows/helga-create-identity.json`](../../agents/n8n-workflows/helga-create-identity.json).

Host env (required for Code nodes):

```text
NODE_FUNCTION_ALLOW_EXTERNAL=faker,@faker-js/faker,@noble/secp256k1,@noble/hashes,nostr-tools
```

Design: [`docs/superpowers/specs/2026-07-21-helga-create-identity-n8n-design.md`](../../docs/superpowers/specs/2026-07-21-helga-create-identity-n8n-design.md).

Helpers (unit-tested logic mirrored in Code nodes): [`agents/n8n-workflows/helga-create-identity/`](../../agents/n8n-workflows/helga-create-identity/).

### Common mistakes

- Leaving example relays (`damus`, `nos.lol`, …) instead of `wss://nostr.neberg.de`
- Expecting realtime DMs from the poll path (it is schedule-based)
- Committing real credentials into workflow JSON
- Forgetting `NODE_FUNCTION_ALLOW_EXTERNAL` for Helga HR (symptoms: `faker_unavailable`)
- Committing employee `nsec` into `agents/identities/*.json`

## Implementation Details

### Runtime flow

See [`docs/deployed-services.md`](../../docs/deployed-services.md) §4 for Nostr → n8n → Flowise sequencing. HITL uses Wait nodes as described in [`docs/process/human-in-the-loop.md`](../../docs/process/human-in-the-loop.md).

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

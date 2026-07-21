# Flowise (AAE)

## Integration Guide

### What it does

Pinned Flowise image for AAE agent / orchestrator flows. Live instance: **https://flowise.neberg.de**. Call it from n8n, not from agent prompts directly.

### Prerequisites

- Access to `https://flowise.neberg.de`
- n8n at `https://n8n.neberg.de` for routing

### Setup

1. Open **https://flowise.neberg.de**.
2. Import Helga from [`agents/flowise-workflows/helga.json`](../../agents/flowise-workflows/helga.json) (Agentflow V2: Start → Agent).
3. On the Helga Agent node: attach an OpenAI credential; replace the system message placeholder (`hol das aktuelle prompt aus dem repo`) with the contents of [`agents/identities/helga.md`](../../agents/identities/helga.md).
4. Save the flow; copy the prediction / API path from the Flowise UI.
5. In n8n (`helga-create-identity`), set `REPLACE_ME_HELGA_PREDICTION_URL` to `https://flowise.neberg.de` + that path.
6. Pass through the structured identity JSON expected by downstream n8n nodes.

### Configuration

| Item | Value |
|------|--------|
| Base URL | `https://flowise.neberg.de` |
| Image | `flowiseai/flowise:3.1.1` ([`Dockerfile`](Dockerfile)) |

Host table: [`docs/deployed-services.md`](../../docs/deployed-services.md).

### Common mistakes

- Hardcoding localhost Flowise URLs in production workflows
- Bypassing n8n so agents cannot share HITL / Nostr outbound paths

## Implementation Details

### Runtime flow

n8n invokes Flowise over HTTPS; Flowise returns routing JSON; n8n performs Nostr writes, app calls, or Wait/HITL. Diagram: [`docs/deployed-services.md`](../../docs/deployed-services.md).

### Key types

| Artifact | Responsibility |
|----------|----------------|
| [`Dockerfile`](Dockerfile) | Pin Flowise 3.1.1 |
| [`agents/flowise-workflows/helga.json`](../../agents/flowise-workflows/helga.json) | Importable Helga Agentflow V2 (LLM only) |

### Extension points

- Bump the image tag in the Dockerfile when upgrading Flowise
- Keep exportable flows under `agents/` when versioning them in-repo

### Limitations

- Prediction URL paths are instance-specific; document them on the n8n sticky note when wiring

### References

- [`docs/deployed-services.md`](../../docs/deployed-services.md)
- [`../README.md`](../README.md)

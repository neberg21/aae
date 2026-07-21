# Flowise (AAE)

## Integration Guide

### What it does

Pinned Flowise image for AAE agent / orchestrator flows. Live instance: **https://flowise.neberg.de**. Call it from n8n, not from agent prompts directly.

### Prerequisites

- Access to `https://flowise.neberg.de`
- n8n at `https://n8n.neberg.de` for routing

### Setup

1. Open **https://flowise.neberg.de** and create or import the agent flow.
2. Copy the prediction / API path from the Flowise UI.
3. In n8n, add an HTTP Request (or Flowise node) to `https://flowise.neberg.de` + that path.
4. Pass through the structured routing JSON expected by downstream n8n nodes.

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

### Extension points

- Bump the image tag in the Dockerfile when upgrading Flowise
- Keep exportable flows under `agents/` when versioning them in-repo

### Limitations

- Prediction URL paths are instance-specific; document them on the n8n sticky note when wiring

### References

- [`docs/deployed-services.md`](../../docs/deployed-services.md)
- [`../README.md`](../README.md)

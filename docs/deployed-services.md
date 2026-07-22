# AAE Deployed Services

Canonical hostnames and roles for the live AAE orchestration stack. Use these values when writing n8n workflows, Flowise tool configs, agent docs, and automation scripts.

## 1. Introduction and Goals

- Document the **production service endpoints** under `*.neberg.de`.
- Give workflow authors stable URLs (HTTP and WebSocket) without digging through Dockerfiles.
- Keep packaging details in [`infrastructure/`](../infrastructure/); this file is the runtime address book.

Primary consumers: n8n workflow JSON, Flowise flows, agent identity docs, HITL process notes, and future CI/CD hooks.

## 2. System Scope and Context

```mermaid
flowchart LR
  Human[Human / Nostr client]
  N8N[n8n.neberg.de]
  Flowise[flowise.neberg.de]
  Relay[nostr.neberg.de]
  App[ai.neberg.de]

  Human -->|DMs / mentions| Relay
  Relay -->|poll or trigger| N8N
  Human -->|UI events| N8N
  N8N -->|agent invoke| Flowise
  N8N -->|app API| App
  Flowise -->|routing JSON| N8N
```

Clarifies which hostname owns which hop in the AAE event path.

| Host | Role | Protocol |
|------|------|----------|
| [`nostr.neberg.de`](https://nostr.neberg.de) | AAE Nostr relay (interface ingress) | HTTPS / **WSS** |
| [`n8n.neberg.de`](https://n8n.neberg.de) | Event routing, webhooks, think workflows | HTTPS |
| [`flowise.neberg.de`](https://flowise.neberg.de) | Optional LLM / agent flows | HTTPS |
| [`ai.neberg.de`](https://ai.neberg.de) | .NET + React app (agent APIs, UI) | HTTPS |

Trust boundary: these hosts are the shared runtime for AAE. Secrets (API keys, Nostr `nsec`, Flowise credentials) stay in each service’s credential store — never in this repo’s docs or workflow JSON placeholders.

## 3. Building Blocks

| Block | Public URL | Repo packaging | Responsibility |
|-------|------------|----------------|----------------|
| Nostr relay | `https://nostr.neberg.de` · `wss://nostr.neberg.de` | [`infrastructure/nostr/`](../infrastructure/nostr/) | Persist and fan out Nostr events (DMs, mentions, kind metadata) for AAE clients and n8n Nostr nodes |
| n8n | `https://n8n.neberg.de` | [`infrastructure/n8n/`](../infrastructure/n8n/) | Schedule/poll, webhooks, route between Nostr, Flowise, and the app; four stateless `*-think` workflows |
| Flowise | `https://flowise.neberg.de` | [`infrastructure/flowise/`](../infrastructure/flowise/) | Host optional LLM / agent flows; return structured payloads for n8n |
| Web app | `https://ai.neberg.de` | [`infrastructure/webapp/`](../infrastructure/webapp/) | ASP.NET Core host + React static UI; `Module.Agents` HTTP APIs used by n8n callbacks |

Image bases (from Dockerfiles):

| Service | Base image |
|---------|------------|
| Nostr | `scsibug/nostr-rs-relay:0.10.0` (container port `8080`) |
| n8n | `n8nio/n8n:latest` + baked-in `n8n-nodes-nostrobots@1.2.1` |
| Flowise | `flowiseai/flowise:3.1.1` |
| Web app | Multi-stage Node 22 + .NET 10 (`infrastructure/webapp/Dockerfile`) |

## 4. Runtime View

### Typical Nostr → agent path

```mermaid
sequenceDiagram
  participant Client as Nostr client
  participant Relay as nostr.neberg.de
  participant N8N as n8n.neberg.de
  participant App as ai.neberg.de
  participant Flow as flowise.neberg.de

  Client->>Relay: publish DM / mention
  N8N->>Relay: read via WSS (poll or trigger)
  N8N->>App: HTTP agent APIs (route / create-identity / …)
  opt Optional Flowise hop
    N8N->>Flow: HTTP invoke agent flow
    Flow-->>N8N: routing JSON
  end
  N8N->>Relay: write DM reply (optional)
```

Clarifies that **n8n** drives orchestration callbacks into the app; Flowise is optional depending on the workflow.

### Values for workflow scripts

Copy these into nodes, env files, or sticky notes — do not hardcode public relays when the AAE relay is intended.

```text
NOSTR_RELAY_HTTPS=https://nostr.neberg.de
NOSTR_RELAY_WSS=wss://nostr.neberg.de
N8N_BASE_URL=https://n8n.neberg.de
FLOWISE_BASE_URL=https://flowise.neberg.de
BACKEND_BASE_URL=https://ai.neberg.de
```

**n8n Nostr nodes** (community package `n8n-nodes-nostrobots`): set the relay field to `wss://nostr.neberg.de` (comma-separate additional public relays only if deliberately multi-homing).

**Think workflow callbacks:** hardcoded base `https://ai.neberg.de` in the imported JSON (for example `POST /api/agents/route-chat-message`, `POST /api/agents/create-identity`).

**Flowise HTTP nodes / webhooks from n8n:** base URL `https://flowise.neberg.de` plus the flow-specific prediction path from the Flowise UI.

**Inbound webhooks into n8n:** `https://n8n.neberg.de/webhook/...` (path from the Webhook node). Prefer production webhook URLs from the live instance, not localhost.

Example workflow JSON in-repo: [`agents/n8n-workflows/`](../agents/n8n-workflows/). Older examples may still list `wss://relay.damus.io`, `wss://nos.lol`, etc. — prefer `wss://nostr.neberg.de` for AAE-owned traffic.

## 5. Crosscutting Concepts

- **Configuration model:** Hostnames are stable; container image tags and community-node versions change in `infrastructure/*/Dockerfile`.
- **Auth:** n8n and Flowise use their own login / API keys. Nostr uses keypair credentials in n8n (`Nostrobots API`), not HTTP Basic on the relay URL.
- **HITL:** Approvals are backend-owned (`/api/agents/await-request-approval`, `/api/agents/resolve-request-approval`). Think workflows do not use n8n Wait nodes. See [`docs/process/human-in-the-loop.md`](process/human-in-the-loop.md).
- **Persistence:** Nostr relay expects a mounted SQLite volume (`./data` → `/usr/src/app/db` per Dockerfile comments). n8n/Flowise persistence is host/platform-managed outside this doc.
- **Observability:** Use each product’s UI (n8n executions, Flowise logs, relay process logs, app logs). No shared AAE metrics endpoint yet.

## 6. Risks and Limitations

- Example workflows may still point at third-party relays; AAE DM reliability depends on using `wss://nostr.neberg.de`.
- NIP-04 DMs leak metadata (sender/recipient public); see [`infrastructure/n8n/README.md`](../infrastructure/n8n/README.md).
- Flowise prediction paths and n8n webhook IDs are instance-specific — document them in the workflow sticky notes when created, not only here.
- Do not commit `nsec`, Flowise API keys, or n8n credentials.
- Some agent APIs (`await-request-approval`, `resolve-request-approval`, `execute-tool`) are mapped but not implemented yet in `Module.Agents`.

## 7. Glossary

| Term | Meaning |
|------|---------|
| Relay | Nostr WebSocket server that stores/forwards events |
| Event routing | n8n’s role as bus between interfaces and Flowise/app |
| Think workflow | Stateless n8n graph (`leo-think`, `helga-think`, `supervisor-think`, `specialist-think`) |
| HITL | Human-in-the-loop pause via backend approval APIs + UI |

## References

- Packaging: [`infrastructure/README.md`](../infrastructure/README.md)
- n8n setup: [`infrastructure/n8n/README.md`](../infrastructure/n8n/README.md)
- Topology overview: [`README.md`](../README.md)
- HITL process: [`docs/process/human-in-the-loop.md`](process/human-in-the-loop.md)
- Agent HTTP contracts: [`agents/workflow.md`](../agents/workflow.md)

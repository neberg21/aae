# Human-in-the-loop (HITL) approval flow

Path from agent work to human decision. n8n think workflows are **stateless** (no Wait nodes). Pause/resume belongs on the **.NET backend** and UI.

```text
               ┌─────────────────────────────────────────┐
               │ 1. DEVELOPMENT & LOCAL REVIEW           │
               └─────────────────────────────────────────┘
                 ┌────────────────┐       (Code / draft)
                 │ Specialist     │ ──────────────────►
                 │ (or nested     │ ◄──────────────────
                 │  supervisor)   │   (correction loop)
                 └────────────────┘

                                            ▼
                                  ┌──────────────────┐
                                  │   Supervisor     │
                                  │   (domain / nest)│
                                  └────────┬─────────┘
                                           │
                    (2. Outcome `done` → approval payload)
                                           │
               ┌───────────────────────────▼─────────────────────────────┐
               │ 3. BACKEND APPROVAL GATE                                │
               │ POST /api/agents/await-request-approval                 │
               │ (routed; handler not implemented yet)                   │
               │ Intended: hold context, surface draft to the UI         │
               └───────────────────────────┬─────────────────────────────┘
                                           │
                                           ▼
               ┌─────────────────────────────────────────┐
               │ 4. USER INTERVENTION (React UI / Nostr) │
               │ Interactive card with code / design     │
               └───────────────────┬─────────────────────┘
                                   │
                           [ YOUR DECISION ]
                                   │
               ┌───────────────────┴───────────────────┐
               │                                       │
     [ REQUEST CHANGES ]                        [ APPROVE ]
               │                                       │
               ▼                                       ▼
     POST /api/agents/resolve-request-approval   same endpoint
     (routed; not implemented yet)               then continue work /
     → feedback back into supervisor loop        merge / deploy path
```

### Phases

* **1–2 — Prep:** Agents stay inside their module paths. The supervisor packages the deliverable and chooses outcome `done` (see [`template_supervisor.md`](../../agents/identities/template_supervisor.md)). The `supervisor-think` workflow POSTs to the backend approval URL.
* **3 — Gate:** Backend owns the pause. Current code maps the route under `Module.Agents` but `AwaitRequestApproval` / `ResolveRequestApproval` still throw `NotImplementedException`.
* **4 — Decision:** UI (or Nostr-driven UI path) calls `resolve-request-approval` with approve or reject + feedback.
* **5 — After approve:** Intended follow-on is commit/merge and the multi-stage webapp image rebuild (Docker / Koyeb). That deploy wiring is packaging-side, not part of the n8n think graphs.

### Related contracts

* [`agents/workflow.md`](../../agents/workflow.md) — HTTP surface and four think workflows
* [`docs/deployed-services.md`](../deployed-services.md) — hostnames (`ai.neberg.de`, `n8n.neberg.de`, …)

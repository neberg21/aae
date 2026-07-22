### How do supervisors get their system prompt?

**Helga builds it.** When Leo (orchestrator) decides a new module or sub-team is needed (for example, “We need a Finance module”), he sends an `hr_request` to Helga to create the matching supervisor identity.

Helga fills a **fixed supervisor template** with domain-specific details and persists the identity via `POST /api/agents/create-identity`. The same path creates specialists and subordinate supervisors (`managerId` may be `leo` or another `supervisor-*`).

Canonical templates:

- [Supervisor template](../../agents/identities/template_supervisor.md)
- [Specialist template](../../agents/identities/template_specialist.md)
- [Helga (HR)](../../agents/identities/helga.md)
- [Leo (orchestrator)](../../agents/identities/leo.md)

## Runtime (n8n)

Identities are created through the Helga think workflow (stateless; no Wait nodes):

- Workflow: [`agents/n8n-workflows/helga-think.json`](../../agents/n8n-workflows/helga-think.json)
- Contracts: [`agents/workflow.md`](../../agents/workflow.md)

Typical ids:

- Supervisor: `supervisor-{domain}` (example: `supervisor-finance`)
- Specialist: `specialist-{role}` (example: `specialist-react`)
- Nested supervisor: still `supervisor-{domain-or-scope}`, with `managerId` pointing at the parent supervisor

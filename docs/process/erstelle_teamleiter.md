### Wie bekommen die ihren System-Prompt?

**Helga baut ihn!** Wenn Leo, der Orchestrator, feststellt, dass ein neues Modul gebaut werden soll (z. B. "Wir brauchen ein neues Modul für *Finanzen*"), befiehlt er Helga, den "Teamleiter Finanzen" zu erschaffen.

Helga nutzt dafür ein **festes Template für Teamleiter**, das sie mit den spezifischen Domänen-Informationen anreichert und als JSON in der Agenten-Datenbank ablegt.

Hier ist das dynamische Template, das die Grundlage für jeden Teamleiter-System-Prompt bildet:
[Template Domain Supervisor|../../agents/identities/template_domain-supervisor.md]
[Helga, HR|../../agents/identities/helga.md]
[Leo, Orchestrator (CEO)|../../agents/identities/leo.md]

## Runtime (n8n)

Neue Identitäten werden per Webhook-Workflow angelegt:

- Workflow: [`agents/n8n-workflows/helga-create-identity.json`](../../agents/n8n-workflows/helga-create-identity.json)
- Design: [`docs/superpowers/specs/2026-07-21-helga-create-identity-n8n-design.md`](../superpowers/specs/2026-07-21-helga-create-identity-n8n-design.md)

`agent_id` = `{domain}-{role}` (z. B. `finanzen-teamleiter`). Nostr-Profilname: `{Vorname} ({role_title})`.


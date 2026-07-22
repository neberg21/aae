# Leo / Helga fetch system prompt — Design

**Date:** 2026-07-22  
**Status:** Approved  
**Deliverable:** Update `leo-think.json` and `helga-think.json` so the AI Agent `systemMessage` comes from the backend identity APIs instead of hardcoded strings; document the change

## Goal

Keep Leo and Helga chat/think runtime behavior unchanged. Only the source of the system prompt changes: load it from the Agents module via search + get-by-id.

## Decisions (locked)

| Topic | Choice |
|-------|--------|
| Scope | `leo-think.json` and `helga-think.json` only — do not modify supervisor/specialist workflows |
| User chat flow | Unchanged (webhook → normalize → AI → parse → callbacks) |
| System prompt source | Backend identity, not sticky/hardcoded Agent `systemMessage` |
| Lookup | `GET /api/agents/search?department=Core&name=leo\|helga` then `GET /api/agents/{identityId}` |
| AI Agent binding | `systemMessage` = response `systemPrompt`; user `text` stays the existing Code-built prompt |
| Error path | If search returns no items or get-by-id fails, treat as workflow failure (existing failure → User route where present) |

## Architecture

```text
Webhook → Normalize Input (unchanged user prompt)
  → Search Agent   GET https://ai.neberg.de/api/agents/search?department=Core&name={leo|helga}
  → Resolve Id     Code: items[0].identityId
  → Get Prompt     GET https://ai.neberg.de/api/agents/{identityId}
  → AI Agent       systemMessage = {{ systemPrompt }}; text = {{ prompt }}
  → Parse → …      unchanged
```

## API contracts (backend as implemented)

### Search

`GET /api/agents/search?department=Core&name=leo`

Response (page wrapper):

```json
{
  "items": [
    {
      "identityId": "…",
      "name": "leo",
      "department": "Core",
      "jobTitle": "CEO"
    }
  ],
  "totalCount": 1,
  "pageSize": 1,
  "pageNumber": 1,
  "totalPages": 1
}
```

Name and department matching are case-insensitive on the backend.

### Get by id

`GET /api/agents/{identityId}`

Response includes `systemPrompt` (plus identity metadata). Workflow uses `systemPrompt` for the LangChain Agent `systemMessage`.

## Out of scope

- Changing supervisor-think / specialist-think
- Changing webhook input shapes, parse logic, or outbound `route-chat-message` / `create-identity` bodies
- Implementing or fixing backend search/get endpoints (assumed already available)
- Replacing the user-facing Code prompt preamble (runtime task framing stays as today)

## Docs to update

- `agents/workflow.md` — list the two GET endpoints; note Leo/Helga load `systemPrompt` before the AI Agent
- `docs/superpowers/specs/2026-07-22-aae-n8n-think-workflows-design.md` — Leo/Helga flow + shared contracts
- `agents/n8n-workflows/VERIFY.md` — expect search + get-by-id before the model call
- Sticky notes inside `leo-think.json` / `helga-think.json`
- `docs/deployed-services.md` — mention identity GET usage from think workflows

## Success criteria

1. Leo and Helga workflows contain no hardcoded role `systemMessage` string; they bind fetched `systemPrompt`
2. Supervisor and specialist workflow JSON are byte-unchanged (aside from accidental untouched files)
3. Docs describe search → get → AI Agent for Leo/Helga
4. Import + VERIFY steps still match the runnable graphs

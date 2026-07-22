# Verify AAE think-workflows

## Import

1. Open https://n8n.neberg.de
2. Import `leo-think.json`, `helga-think.json`, `supervisor-think.json`, `specialist-think.json`
3. Attach OpenAI credential on every Chat Model node
4. Attach GitHub credential on Supervisor tool nodes; set `OWNER/REPO` in tool URLs
5. Activate all four workflows

## Smoke (no Wait; fire-and-forget)

### Leo

`POST https://n8n.neberg.de/webhook/leo-think`

```json
{
  "threadId": "smoke-leo",
  "chatHistory": [],
  "userVision": "Build a Finanzen module with UI and API"
}
```

Expect: one or more POSTs to `https://ai.neberg.de/api/agents/route-chat-message` (camelCase).

### Helga clarify / create

`POST https://n8n.neberg.de/webhook/helga-think`

```json
{
  "threadId": "smoke-helga",
  "chatHistory": [],
  "delegationRequest": {
    "message": "Need a Finanzen supervisor",
    "moduleScope": "Module.Finanzen",
    "role": "supervisor"
  }
}
```

Expect: either `route-chat-message` to User OR `create-identity` with `tools` / `guardrails` / `managerId`.

### Supervisor

`POST https://n8n.neberg.de/webhook/supervisor-think`

```json
{
  "threadId": "smoke-sup",
  "chatHistory": [],
  "taskContext": "Deliver Finanzen MVP",
  "subordinatesList": ["specialist-react"],
  "senderAgentId": "supervisor-finanzen"
}
```

Expect: waiting | delegate route calls | `await-request-approval`.

### Specialist

`POST https://n8n.neberg.de/webhook/specialist-think`

```json
{
  "threadId": "smoke-spec",
  "chatHistory": [],
  "taskContext": "Scaffold React page",
  "allowedTools": ["GenerateCode"],
  "managerId": "supervisor-finanzen",
  "senderAgentId": "specialist-react"
}
```

Expect: optional `execute-tool` calls; final `route-chat-message` to `supervisor-finanzen`.

## Negatives

- No Wait nodes in any workflow
- No `teamleiter` agent ids in routing output (helpers reject them)
- Specialist has no filesystem tool nodes

## Local helper tests

```cmd
cd /d agents\n8n-workflows\think-helpers
npm test
```

## Structural validation

```cmd
node agents\n8n-workflows\validate-workflow.mjs agents\n8n-workflows\leo-think.json leo-think
node agents\n8n-workflows\validate-workflow.mjs agents\n8n-workflows\helga-think.json helga-think
node agents\n8n-workflows\validate-workflow.mjs agents\n8n-workflows\supervisor-think.json supervisor-think
node agents\n8n-workflows\validate-workflow.mjs agents\n8n-workflows\specialist-think.json specialist-think
```

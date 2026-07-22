# Leo Park-Delegation + HR Hire Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Leo n8n checks whether each `supervisor-*` exists before routing; if missing, backend parks the delegation and Helga creates the identity; after create, backend auto-routes parked work.

**Architecture:** Stateless Leo workflow calls `GET /api/agents/search?agentId=` then either `route-chat-message` or `park-delegation` + mechanical Helga `hr_request`. Backend stores parks in-memory like other Agent lists, persists explicit `AgentId` on create, resumes FIFO parks via `RouteChatMessageService`. Shared JS helpers own Leo action planning and Helga `agentId` mapping (unit-tested).

**Tech Stack:** .NET Module.Agents (in-memory `AppDbContext`), n8n workflow JSON, Node.js `node:test` helpers, camelCase HTTP DTOs to `https://ai.neberg.de`

**Spec:** [`docs/superpowers/specs/2026-07-22-leo-park-delegation-hr-design.md`](../specs/2026-07-22-leo-park-delegation-hr-design.md)

## Global Constraints

- Windows scripting: no PowerShell/bash scripts; use `cmd /c` for one-offs; multi-line commits via message file
- Backend base URL hardcoded: `https://ai.neberg.de`
- All outbound JSON bodies: camelCase
- Naming: `supervisor` only — never `teamleiter`
- No Wait nodes in n8n; fire-and-forget only
- C#: no primary constructors; do not inline object creation into method/ctor calls
- Response language / commit messages: English
- Scope: Leo + backend park/resume + Helga `agentId` pass-through; Supervisor reuse later (out of scope)

---

## File Structure

| File | Responsibility |
|------|----------------|
| `backend/src/Module.Agents/Persistence/ParkedDelegation.cs` | Parked row entity |
| `backend/src/Module.Agents/Persistence/AppDbContext.cs` | `ParkedDelegations` list |
| `backend/src/Module.Agents/Persistence/Agent.cs` | Explicit `AgentId` as `Id` |
| `backend/src/Module.Agents/DTOs/ParkDelegationRequest.cs` | Park POST body |
| `backend/src/Module.Agents/DTOs/ParkDelegationResponse.cs` | Park POST ack |
| `backend/src/Module.Agents/DTOs/CreateIdentityRequest.cs` | Add required `AgentId` |
| `backend/src/Module.Agents/AI/ParkDelegationService.cs` | Park + dequeue-by-target |
| `backend/src/Module.Agents/AI/CreateIdentityService.cs` | Create with `AgentId`, 409, resume parks |
| `backend/src/Module.Agents/AI/RouteChatMessageService.cs` | Pass `delegationRequest`/`content` on webhook wake |
| `backend/src/Module.Agents/AgentsModule.cs` | Map `park-delegation`; wire create 409 |
| `backend/tests/Service.Unit/Agents/ParkDelegationTests.cs` | Park + create resume integration tests |
| `backend/tests/Service.Unit/Agents/CreateIdentityTests.cs` | Update create payload with `AgentId` |
| `agents/n8n-workflows/think-helpers/helpers.mjs` | Leo action planner + Helga `agentId` mapping |
| `agents/n8n-workflows/think-helpers/helpers.test.mjs` | Unit tests for new helpers |
| `agents/n8n-workflows/leo-think.json` | Search → park/route branch |
| `agents/n8n-workflows/helga-think.json` | Pass `agentId` on create; honor HR JSON |
| `agents/workflow.md` | Document park + Leo branch |
| `agents/n8n-workflows/VERIFY.md` | Manual smoke for miss → park → hire → resume |

---

### Task 1: Think-helpers — Leo action planner + Helga agentId

**Files:**
- Modify: `agents/n8n-workflows/think-helpers/helpers.mjs`
- Modify: `agents/n8n-workflows/think-helpers/helpers.test.mjs`

**Interfaces:**
- Consumes: existing `parseLeoDelegations`, `mapHelgaIdentityToCreateRequest`, `parseHelgaDecision`
- Produces:
  - Extended Leo item fields: `intent`, `message`, `moduleScope` (in addition to route fields)
  - `buildHelgaHrRequestContent({ agentId, moduleScope, message }) → string` (JSON string)
  - `buildParkDelegationBody({ threadId, senderAgentId, targetAgentId, content }) → object`
  - `planLeoItemAction(item, exists) → { action: 'route', routeBody } | { action: 'park_and_hire', parkBody, helgaRouteBody }`
  - `mapHelgaIdentityToCreateRequest` includes `agentId: identity.agentId`
  - `parseHelgaDecision` create branch requires non-empty `identity.agentId` (no `teamleiter`)

- [ ] **Step 1: Write failing tests**

Append to `helpers.test.mjs`:

```js
import {
  // ...existing imports
  buildHelgaHrRequestContent,
  buildParkDelegationBody,
  planLeoItemAction,
} from './helpers.mjs';

describe('planLeoItemAction', () => {
  it('routes when supervisor exists', () => {
    const item = {
      threadId: 't1',
      senderAgentId: 'leo',
      targetAgentId: 'supervisor-finance',
      content: 'Own Finance (delegation) [scope=Module.Finance]',
      intent: 'delegation',
      message: 'Own Finance',
      moduleScope: 'Module.Finance',
    };
    const planned = planLeoItemAction(item, true);
    assert.equal(planned.action, 'route');
    assert.equal(planned.routeBody.targetAgentId, 'supervisor-finance');
    assert.equal(planned.routeBody.content, item.content);
  });

  it('parks and wakes helga when supervisor missing', () => {
    const item = {
      threadId: 't1',
      senderAgentId: 'leo',
      targetAgentId: 'supervisor-finance',
      content: 'Own Finance (delegation) [scope=Module.Finance]',
      intent: 'delegation',
      message: 'Own Finance',
      moduleScope: 'Module.Finance',
    };
    const planned = planLeoItemAction(item, false);
    assert.equal(planned.action, 'park_and_hire');
    assert.equal(planned.parkBody.targetAgentId, 'supervisor-finance');
    assert.equal(planned.parkBody.content, item.content);
    assert.equal(planned.helgaRouteBody.targetAgentId, 'helga');
    const hr = JSON.parse(planned.helgaRouteBody.content);
    assert.equal(hr.intent, 'hr_request');
    assert.equal(hr.agentId, 'supervisor-finance');
    assert.equal(hr.role, 'supervisor');
    assert.equal(hr.moduleScope, 'Module.Finance');
    assert.equal(hr.message, 'Own Finance');
  });

  it('routes helga items without park', () => {
    const item = {
      threadId: 't1',
      senderAgentId: 'leo',
      targetAgentId: 'helga',
      content: 'Need hire (hr_request)',
      intent: 'hr_request',
      message: 'Need hire',
      moduleScope: null,
    };
    const planned = planLeoItemAction(item, false);
    assert.equal(planned.action, 'route');
    assert.equal(planned.routeBody.targetAgentId, 'helga');
  });
});

describe('mapHelgaIdentityToCreateRequest', () => {
  it('includes agentId', () => {
    const body = mapHelgaIdentityToCreateRequest(
      {
        agentId: 'supervisor-finance',
        roleTitle: 'Supervisor Finance',
        department: 'Operations',
        systemPrompt: '…',
        tools: [],
        guardrails: [],
        managerId: 'leo',
      },
      'Hire finance supervisor',
    );
    assert.equal(body.agentId, 'supervisor-finance');
  });
});

describe('parseHelgaDecision', () => {
  it('rejects ready identity without agentId', () => {
    const r = parseHelgaDecision(
      JSON.stringify({
        status: 'ready',
        clarificationQuestions: null,
        identity: {
          roleTitle: 'Supervisor Finance',
          department: 'Operations',
          systemPrompt: '…',
          tools: [],
          guardrails: [],
          managerId: 'leo',
        },
      }),
      't1',
      'hire',
    );
    assert.equal(r.ok, false);
  });
});
```

Also update the existing `maps ready identity to create-identity body` test to assert `r.createBody.agentId === 'supervisor-finanzen'`.

- [ ] **Step 2: Run tests — expect FAIL**

Run:

```cmd
cd agents\n8n-workflows\think-helpers
npm test
```

Expected: FAIL (missing exports / missing `agentId`).

- [ ] **Step 3: Implement helpers**

In `helpers.mjs`, change `parseLeoDelegations` item push to:

```js
items.push({
  threadId,
  senderAgentId: 'leo',
  targetAgentId: d.targetAgentId,
  content: `${d.message ?? ''}${intent}${scope}`.trim(),
  intent: d.intent ?? null,
  message: d.message ?? '',
  moduleScope: d.moduleScope ?? null,
});
```

Add:

```js
export function buildHelgaHrRequestContent({ agentId, moduleScope, message }) {
  return JSON.stringify({
    intent: 'hr_request',
    agentId,
    role: 'supervisor',
    moduleScope: moduleScope ?? null,
    message: message ?? '',
  });
}

export function buildParkDelegationBody({
  threadId,
  senderAgentId,
  targetAgentId,
  content,
}) {
  return {
    threadId,
    senderAgentId,
    targetAgentId,
    content,
  };
}

export function planLeoItemAction(item, exists) {
  const isHelga = String(item.targetAgentId).toLowerCase() === 'helga';
  if (isHelga || exists) {
    return {
      action: 'route',
      routeBody: {
        threadId: item.threadId,
        senderAgentId: item.senderAgentId,
        targetAgentId: item.targetAgentId,
        content: item.content,
      },
    };
  }

  const parkBody = buildParkDelegationBody({
    threadId: item.threadId,
    senderAgentId: item.senderAgentId,
    targetAgentId: item.targetAgentId,
    content: item.content,
  });
  const hrContent = buildHelgaHrRequestContent({
    agentId: item.targetAgentId,
    moduleScope: item.moduleScope,
    message: item.message,
  });
  return {
    action: 'park_and_hire',
    parkBody,
    helgaRouteBody: {
      threadId: item.threadId,
      senderAgentId: item.senderAgentId,
      targetAgentId: 'helga',
      content: hrContent,
    },
  };
}
```

Update `mapHelgaIdentityToCreateRequest`:

```js
export function mapHelgaIdentityToCreateRequest(identity, jobDescription) {
  return {
    agentId: identity.agentId ?? '',
    jobTitle: identity.roleTitle ?? identity.jobTitle ?? '',
    jobDescription: jobDescription ?? identity.jobDescription ?? '',
    department: identity.department ?? '',
    managerId: identity.managerId ?? null,
    systemPrompt: identity.systemPrompt ?? '',
    guardrails: identity.guardrails ?? [],
    tools: identity.tools ?? identity.required_tools ?? [],
  };
}
```

In `parseHelgaDecision` ready branch, after manager check:

```js
if (!obj.identity.agentId || !assertNoTeamleiter(obj.identity.agentId)) {
  return {
    ok: false,
    error: 'invalid_agent_id',
    userMessage: 'Helga produced an invalid agentId.',
  };
}
```

- [ ] **Step 4: Run tests — expect PASS**

```cmd
cd agents\n8n-workflows\think-helpers
npm test
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```cmd
git add agents/n8n-workflows/think-helpers/helpers.mjs agents/n8n-workflows/think-helpers/helpers.test.mjs
```

Commit message:

```text
test: add Leo park/hire helper planning and Helga agentId mapping
```

---

### Task 2: Backend — park store + park-delegation endpoint

**Files:**
- Create: `backend/src/Module.Agents/Persistence/ParkedDelegation.cs`
- Create: `backend/src/Module.Agents/DTOs/ParkDelegationRequest.cs`
- Create: `backend/src/Module.Agents/DTOs/ParkDelegationResponse.cs`
- Create: `backend/src/Module.Agents/AI/ParkDelegationService.cs`
- Modify: `backend/src/Module.Agents/Persistence/AppDbContext.cs`
- Modify: `backend/src/Module.Agents/AgentsModule.cs`
- Create: `backend/tests/Service.Unit/Agents/ParkDelegationTests.cs`

**Interfaces:**
- Consumes: `AppDbContext`
- Produces:
  - `ParkDelegationService.Park(ParkDelegationRequest) → ParkDelegationResponse`
  - `ParkDelegationService.DequeueByTargetAgentId(string agentId) → IReadOnlyList<ParkedDelegation>` (FIFO, removes from store)
  - Endpoint: `POST /api/agents/park-delegation`

- [ ] **Step 1: Write failing test**

Create `ParkDelegationTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Module.Agents.DTOs;
using Xunit;

namespace Service.Unit.Agents;

public class ParkDelegationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ParkDelegationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostAsync_ParkDelegation_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var request = new ParkDelegationRequest
        {
            ThreadId = "thread-park-1",
            SenderAgentId = "leo",
            TargetAgentId = "supervisor-finance",
            Content = "Own Finance module"
        };

        var response = await client.PostAsJsonAsync("/api/agents/park-delegation", request);
        var body = await response.Content.ReadFromJsonAsync<ParkDelegationResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Ok);
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

```cmd
dotnet test backend/tests/Service.Unit/Service.Unit.csproj --filter "FullyQualifiedName~ParkDelegationTests"
```

Expected: FAIL (types/endpoint missing).

- [ ] **Step 3: Implement park types + service + endpoint**

`ParkedDelegation.cs`:

```csharp
namespace Module.Agents.Persistence;

public record ParkedDelegation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string ThreadId { get; set; }
    public required string SenderAgentId { get; set; }
    public required string TargetAgentId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

`ParkDelegationRequest.cs`:

```csharp
namespace Module.Agents.DTOs;

public record ParkDelegationRequest
{
    public required string ThreadId { get; init; }
    public required string SenderAgentId { get; init; }
    public required string TargetAgentId { get; init; }
    public required string Content { get; init; }
}
```

`ParkDelegationResponse.cs`:

```csharp
namespace Module.Agents.DTOs;

public record ParkDelegationResponse
{
    public required bool Ok { get; init; }
}
```

`ParkDelegationService.cs`:

```csharp
using Module.Agents.DTOs;
using Module.Agents.Persistence;

namespace Module.Agents.AI;

public class ParkDelegationService
{
    private readonly AppDbContext _dbContext;

    public ParkDelegationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParkDelegationResponse> Park(ParkDelegationRequest request)
    {
        var parked = new ParkedDelegation
        {
            ThreadId = request.ThreadId,
            SenderAgentId = request.SenderAgentId,
            TargetAgentId = request.TargetAgentId,
            Content = request.Content
        };
        _dbContext.ParkedDelegations.Add(parked);
        await _dbContext.SaveChangesAsync();

        var response = new ParkDelegationResponse
        {
            Ok = true
        };
        return response;
    }

    public IReadOnlyList<ParkedDelegation> DequeueByTargetAgentId(string agentId)
    {
        var matches = _dbContext.ParkedDelegations
            .Where(p => p.TargetAgentId.Equals(agentId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.CreatedAt)
            .ToList();

        foreach (var match in matches)
        {
            _dbContext.ParkedDelegations.Remove(match);
        }

        return matches;
    }
}
```

Add to `AppDbContext`:

```csharp
public List<ParkedDelegation> ParkedDelegations { get; set; } = [];
```

In `AgentsModule.RegisterServices`:

```csharp
services.AddScoped<ParkDelegationService>();
```

In `MapEndpoints`:

```csharp
endpoints.MapPost("park-delegation", ParkDelegation)
    .Accepts<ParkDelegationRequest>("application/json")
    .Produces<ParkDelegationResponse>();
```

Handler:

```csharp
private static async Task<IResult> ParkDelegation(
    [FromBody] ParkDelegationRequest request,
    ParkDelegationService parkDelegationService)
{
    var res = await parkDelegationService.Park(request);
    return Results.Ok(res);
}
```

- [ ] **Step 4: Run test — expect PASS**

```cmd
dotnet test backend/tests/Service.Unit/Service.Unit.csproj --filter "FullyQualifiedName~ParkDelegationTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```cmd
git add backend/src/Module.Agents backend/tests/Service.Unit/Agents/ParkDelegationTests.cs
```

Commit message:

```text
feat: add park-delegation endpoint and in-memory park store
```

---

### Task 3: Backend — explicit AgentId on create + 409 + resume parks

**Files:**
- Modify: `backend/src/Module.Agents/Persistence/Agent.cs`
- Modify: `backend/src/Module.Agents/DTOs/CreateIdentityRequest.cs`
- Modify: `backend/src/Module.Agents/AI/CreateIdentityService.cs`
- Modify: `backend/src/Module.Agents/AI/SeedCoreAgents.cs` (set `AgentId` for leo/helga)
- Modify: `backend/src/Module.Agents/AgentsModule.cs` (create returns 409)
- Modify: `backend/src/Module.Agents/AI/RouteChatMessageService.cs` (webhook payload includes content as `delegationRequest`)
- Modify: `backend/tests/Service.Unit/Agents/CreateIdentityTests.cs`
- Modify: `backend/tests/Service.Unit/Agents/ParkDelegationTests.cs`

**Interfaces:**
- Consumes: `ParkDelegationService.DequeueByTargetAgentId`, `RouteChatMessageService.RouteChatMessage`
- Produces: create with required `agentId`; on success resume parks; duplicate → 409

- [ ] **Step 1: Write failing tests**

Update `CreateIdentityTests` request to include `AgentId = "specialist-test-engineer"` (and assert response `AgentId` equals that).

Add to `ParkDelegationTests.cs`:

```csharp
[Fact]
public async Task PostAsync_CreateIdentity_ResumesParkedDelegation()
{
    var client = _factory.CreateClient();
    var parkRequest = new ParkDelegationRequest
    {
        ThreadId = "thread-resume-1",
        SenderAgentId = "leo",
        TargetAgentId = "supervisor-finance",
        Content = "Own Finance module"
    };
    var parkResponse = await client.PostAsJsonAsync("/api/agents/park-delegation", parkRequest);
    Assert.Equal(HttpStatusCode.OK, parkResponse.StatusCode);

    var createRequest = new CreateIdentityRequest
    {
        AgentId = "supervisor-finance",
        JobTitle = "Supervisor Finance",
        JobDescription = "Hire finance supervisor",
        SystemPrompt = "You supervise Finance.",
        Department = "Operations",
        ManagerId = "leo",
        Guardrails = [],
        Tools = []
    };
    var createResponse = await client.PostAsJsonAsync("/api/agents/create-identity", createRequest);
    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    var identity = await createResponse.Content.ReadFromJsonAsync<CreateIdentityResponse>();
    Assert.NotNull(identity);
    Assert.Equal("supervisor-finance", identity.AgentId);

    // Second create with same agentId must conflict
    var conflictResponse = await client.PostAsJsonAsync("/api/agents/create-identity", createRequest);
    Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
}

[Fact]
public async Task GetAsync_SearchByAgentId_FindsCreatedSupervisor()
{
    var client = _factory.CreateClient();
    var createRequest = new CreateIdentityRequest
    {
        AgentId = "supervisor-qa",
        JobTitle = "Supervisor QA",
        JobDescription = "QA lead",
        SystemPrompt = "You supervise QA.",
        Department = "QA",
        ManagerId = "leo",
        Guardrails = [],
        Tools = []
    };
    var createResponse = await client.PostAsJsonAsync("/api/agents/create-identity", createRequest);
    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    var searchResponse = await client.GetAsync("/api/agents/search?agentId=supervisor-qa");
    Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
    var page = await searchResponse.Content.ReadFromJsonAsync<GetAgentsResponse>();
    Assert.NotNull(page);
    Assert.Contains(page.Items, i => i.AgentId == "supervisor-qa");
}
```

Note: resume assertion that a chat message was written can check via another route or by verifying create returns OK after park (full webhook wake may hit n8n — prefer asserting parks cleared by attempting that create does not throw, and optionally expose nothing; if `RouteChatMessage` fails on unknown host, inject/test double or catch HTTP errors in resume so create still succeeds). **Required behavior:** create succeeds even if outbound n8n wake fails; parks must still be dequeued before wake attempts.

- [ ] **Step 2: Run tests — expect FAIL**

```cmd
dotnet test backend/tests/Service.Unit/Service.Unit.csproj --filter "FullyQualifiedName~Agents"
```

Expected: FAIL on missing `AgentId` / 409 / search miss.

- [ ] **Step 3: Implement AgentId + create resume**

Change `Agent.cs` to store explicit id:

```csharp
namespace Module.Agents.Persistence;

public record Agent
{
    public string Id => AgentId;
    public required string AgentId { get; set; }
    public required string Name { get; set; }
    // ... remaining properties unchanged
}
```

Add to `CreateIdentityRequest`:

```csharp
public required string AgentId { get; init; }
```

In `SeedCoreAgents`, when constructing leo/helga agents, set `AgentId = "leo"` / `AgentId = "helga"`.

`CreateIdentityService` — inject `ParkDelegationService` and `RouteChatMessageService`. Method body:

```csharp
public async Task<CreateIdentityResponse?> CreateIdentity(CreateIdentityRequest request)
{
    var existing = _dbContext.Agents.FirstOrDefault(a =>
        a.AgentId.Equals(request.AgentId, StringComparison.OrdinalIgnoreCase));
    if (existing is not null)
    {
        return null; // module maps null → 409
    }

    var keyPair = NostrKeyPair.GenerateKeyPair();
    var firstName = _faker.Person.FirstName;
    var profile = await _profileGenerator.CreateProfileAsync(keyPair, firstName);
    var agent = await CreateAgent(profile, keyPair, request);

    var parked = _parkDelegationService.DequeueByTargetAgentId(agent.AgentId);
    foreach (var item in parked)
    {
        var routeRequest = new RouteChatMessageRequest
        {
            ThreadId = item.ThreadId,
            SenderAgentId = item.SenderAgentId,
            TargetAgentId = item.TargetAgentId,
            Content = item.Content
        };
        try
        {
            await _routeChatMessageService.RouteChatMessage(routeRequest);
        }
        catch
        {
            // create already committed; wake best-effort
        }
    }

    var res = new CreateIdentityResponse
    {
        AgentId = agent.AgentId,
        Name = profile.Name
    };
    return res;
}
```

`CreateAgent` sets `AgentId = request.AgentId`.

`AgentsModule.CreateIdentity` handler:

```csharp
var res = await createIdentityService.CreateIdentity(request);
if (res is null)
{
    return Results.Conflict();
}
return Results.Ok(res);
```

Update `RouteChatMessageService.ExecuteAgentWebhook` to include the routed content for Helga (and others):

```csharp
private async Task ExecuteAgentWebhook(string agentId, string threadId, string content)
{
    var history = _db.ChatMessages
        .Where(m => m.ThreadId == threadId)
        .OrderBy(m => m.CreatedAt)
        .Select(m => new { role = m.Sender == "User" ? "user" : "assistant", content = m.Content })
        .ToList();

    var webhookUrl = agentId.Split('-')[0].ToLower().Trim() switch
    {
        "leo" => "https://n8n.neberg.de/webhook/leo-think",
        "helga" => "https://n8n.neberg.de/webhook/helga-think",
        "supervisor" => "https://n8n.neberg.de/webhook/supervisor-think",
        "specialist" => "https://n8n.neberg.de/webhook/specialist-think",
        _ => throw new ArgumentException($"Unbekannter Agent: {agentId}")
    };

    var payload = new
    {
        threadId,
        chatHistory = history,
        History = history,
        delegationRequest = content,
        taskContext = content
    };
    await _httpClient.PostAsJsonAsync(webhookUrl, payload);
}
```

Pass `request.Content` from `HandleTarget` into `ExecuteAgentWebhook`.

- [ ] **Step 4: Run tests — expect PASS**

```cmd
dotnet test backend/tests/Service.Unit/Service.Unit.csproj --filter "FullyQualifiedName~Agents"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```cmd
git add backend/src/Module.Agents backend/tests/Service.Unit/Agents
```

Commit message:

```text
feat: persist agentId on create, conflict on duplicate, resume parks
```

---

### Task 4: Update leo-think.json

**Files:**
- Modify: `agents/n8n-workflows/leo-think.json`
- Modify: `agents/n8n-workflows/validate-workflow.mjs` only if new checks are added (optional)

**Interfaces:**
- Consumes: helper logic inlined into Code nodes (same as today — workflows embed JS; keep in sync with `helpers.mjs`)
- Produces: after Parse, per-item Search → route OR park+helga route

- [ ] **Step 1: Update Parse Delegations Code**

Embed the extended `parseLeoDelegations` (with `intent`/`message`/`moduleScope`) and after OK path return `items` unchanged for Split.

- [ ] **Step 2: Replace single Route after Split with existence branch**

After **Split Items**, add nodes (suggested names/positions right of Split):

1. **Is Supervisor?** IF node: `{{ $json.targetAgentId.startsWith('supervisor-') }}`
   - false → **Route Chat Message** (existing helga/explicit path)
   - true → **Search Target** HTTP GET `=https://ai.neberg.de/api/agents/search?agentId={{ $json.targetAgentId }}`

2. **Merge Search Context** Code: combine Split item (`$('Split Items').item.json`) with search page; compute `exists = (items||[]).length > 0`; call inlined `planLeoItemAction(item, exists)`; return planned JSON.

3. **Needs Park?** IF: `{{ $json.action === 'park_and_hire' }}`
   - true → **Park Delegation** HTTP POST `https://ai.neberg.de/api/agents/park-delegation` body `={{ JSON.stringify($json.parkBody) }}` → **Route Helga HR** HTTP POST route-chat-message with `helgaRouteBody`
   - false → **Route Chat Message** with `routeBody`

Keep failure path to User unchanged. Sticky note: document search → park → helga.

Ensure no Wait nodes.

- [ ] **Step 3: Validate workflow JSON**

```cmd
node agents\n8n-workflows\validate-workflow.mjs agents\n8n-workflows\leo-think.json leo-think
```

Expected: `leo-think ok …`

Also confirm string contains `park-delegation` and `search?agentId=`:

```cmd
node -e "const fs=require('fs');const s=fs.readFileSync('agents/n8n-workflows/leo-think.json','utf8');if(!s.includes('park-delegation'))process.exit(1);if(!s.includes('agentId='))process.exit(2);if(/teamleiter/i.test(s))process.exit(3);console.log('leo park ok');"
```

- [ ] **Step 4: Commit**

```cmd
git add agents/n8n-workflows/leo-think.json
```

Commit message:

```text
feat: Leo n8n parks missing supervisors and wakes Helga
```

---

### Task 5: Update helga-think.json

**Files:**
- Modify: `agents/n8n-workflows/helga-think.json`

**Interfaces:**
- Consumes: Leo HR JSON in `delegationRequest` / content
- Produces: `create-identity` body including `agentId`

- [ ] **Step 1: Update Normalize Input**

When `delegationRequest` is a string, try `JSON.parse`; if object with `message`/`agentId`, keep full object for prompt. Set `jobDescription` from `message` or stringify. Prompt line: require `identity.agentId` to match intended id from request when present.

- [ ] **Step 2: Update Parse Decision Code**

Inline updated `mapHelgaIdentityToCreateRequest` / `parseHelgaDecision` (require `agentId`). Ensure create HTTP body uses `createBody` which includes `agentId`.

- [ ] **Step 3: Validate**

```cmd
node agents\n8n-workflows\validate-workflow.mjs agents\n8n-workflows\helga-think.json helga-think
```

```cmd
node -e "const fs=require('fs');const s=fs.readFileSync('agents/n8n-workflows/helga-think.json','utf8');if(!s.includes('agentId'))process.exit(1);if(/teamleiter/i.test(s))process.exit(2);console.log('helga agentId ok');"
```

- [ ] **Step 4: Commit**

```cmd
git add agents/n8n-workflows/helga-think.json
```

Commit message:

```text
feat: Helga create-identity passes required agentId
```

---

### Task 6: Docs — workflow.md + VERIFY.md

**Files:**
- Modify: `agents/workflow.md`
- Modify: `agents/n8n-workflows/VERIFY.md`

- [ ] **Step 1: Update workflow.md table**

Add row:

| **POST** `/api/agents/park-delegation` | n8n (Leo) | Park `{ threadId, senderAgentId, targetAgentId, content }` until identity exists |

Update Leo bullet: existence search → route or park + Helga HR JSON; create resumes parks.

Update create-identity note: required `agentId`; duplicate → 409; resumes parks.

- [ ] **Step 2: Update VERIFY.md Leo section**

Document smoke:

1. Vision that needs a new `supervisor-*`
2. Expect `GET .../search?agentId=supervisor-...` empty
3. Expect `POST .../park-delegation`
4. Expect `POST .../route-chat-message` to `helga` with HR JSON content
5. After Helga create with that `agentId`, expect parked content routed to supervisor

- [ ] **Step 3: Commit**

```cmd
git add agents/workflow.md agents/n8n-workflows/VERIFY.md
```

Commit message:

```text
docs: document Leo park-delegation and resume-on-create
```

---

## Self-Review (plan vs spec)

| Spec requirement | Task |
|------------------|------|
| Search existence before route | Task 4 |
| Park API + store | Task 2 |
| Mechanical Helga HR JSON | Tasks 1, 4 |
| Match on `targetAgentId` / `agentId` | Tasks 2–3 |
| Required `agentId` on create | Tasks 1, 3, 5 |
| Resume parks after create | Task 3 |
| Clarify leaves parks | Task 3 (no dequeue until create) |
| Duplicate create 409 | Task 3 |
| Leo-only scope; reusable park API | Tasks 2–4 (Supervisor out of scope) |
| Helpers unit tests | Task 1 |
| Docs / VERIFY | Task 6 |
| No Wait nodes | Tasks 4–5 validate |
| Helga gets HR payload | Task 3 webhook `delegationRequest` |

No TBD placeholders. Types aligned: `ParkDelegationRequest`, `CreateIdentityRequest.AgentId`, `planLeoItemAction`, `park_and_hire`.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-07-22-leo-park-delegation-hr.md`. Two execution options:

**1. Subagent-Driven (recommended)** — dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** — execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?

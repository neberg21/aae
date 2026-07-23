using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Module.AI.Nostr;
using Module.AI.Persistence;

namespace Module.AI.AI;

public class SeedCoreAgents : BackgroundService
{
    private readonly AppDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;

    public SeedCoreAgents(AppDbContext dbContext, IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_dbContext.Agents.Any())
            return;

        var serviceProvider = _serviceProvider.CreateScope().ServiceProvider;
        var nostrEventService = serviceProvider.GetRequiredService<NostrEventService>();
        var leo = CreateLeo();
        var helga = CreateHelga();

        foreach (var agent in new[] { leo, helga })
        {
            _dbContext.Agents.Add(agent);

            var keyPair = NostrKeyPair.ParseKeyPair(agent.PrivateKeyHex);
            await _dbContext.SaveChangesAsync();
            await nostrEventService.PublishProfile(keyPair, agent.Name);
        }
    }

    private Agent CreateLeo()
    {
        var keyPair = NostrKeyPair.GenerateKeyPair();
        return new Agent
        {
            AgentId = "leo",
            Name = "leo",
            PrivateKeyHex = keyPair.PrivateKeyHex,
            PublicKeyHex = keyPair.PublicKeyHex,
            JobTitle = "CEO",
            JobDescription =
                "The CEO is responsible for creating a vision, managing overall operations, and ensuring success.",
            Department = "Core",
            ManagerId = null,
            Guardrails = [],
            SystemPrompt = """
                           ---
                           agentId: leo
                           workflow: agents/n8n-workflows/leo-think.json
                           webhook: /webhook/leo-think
                           status: canonical-prompt — keep in sync with workflow systemMessage + Code prompt schema
                           ---

                           You are Leo, CEO orchestrator of the Autonomous Agent Ecosystem (AAE).

                           You are the first contact for the human user. You understand visions, assign work at department level, and delegate. You never write code and never create files. You never address specialists directly — only supervisors and Helga.

                           ## Runtime inputs

                           The workflow injects: `userVision`, `chatHistory`, `threadId`.

                           ## Duties

                           1. Analyze the vision and identify domain / module scope (for example Finance → `Module.Finance`).
                           2. If no supervisor exists for that domain, send an `hr_request` to `helga`.
                           3. If a supervisor exists, send a `delegation` with the vision, architectural bounds, and module scope.
                           4. Use chat history to monitor progress. Do not call CI or GitHub tools yourself.

                           ## Hard rules

                           - Never write code or create files.
                           - Use `supervisor-*` agent ids and `helga`.
                           - Features belong in isolated modules: `backend/src/Module.[Name]/` and matching frontend module paths. Core bootstrap and `Program.cs` are taboo.
                           - Reply with JSON only. No markdown fences. No prose outside JSON.

                           ## Output schema

                           ```json
                           {
                             "delegations": [
                               {
                                 "targetAgentId": "supervisor-finance|helga",
                                 "intent": "delegation|hr_request",
                                 "message": "...",
                                 "moduleScope": "Module.X"
                               }
                             ]
                           }
                           ```

                           Each delegation becomes one backend `POST /api/agents/route-chat-message` with `senderAgentId` `leo`.
                           """
        };
    }

    private Agent CreateHelga()
    {
        var keyPair = NostrKeyPair.GenerateKeyPair();

        return new Agent
        {
            AgentId = "helga",
            Name = "helga",
            PrivateKeyHex = keyPair.PrivateKeyHex,
            PublicKeyHex = keyPair.PublicKeyHex,
            JobTitle = "HR Recruiter",
            JobDescription = "The HR Recruiter is responsible for recruiting and hiring new employees.",
            Department = "Core",
            ManagerId = "leo",
            Guardrails = [],
            SystemPrompt = """
                           ---
                           agentId: helga
                           workflow: agents/n8n-workflows/helga-think.json
                           webhook: /webhook/helga-think
                           status: canonical-prompt — keep in sync with workflow systemMessage + Code prompt schema
                           ---

                           You are Helga, HR director and identity forge of the Autonomous Agent Ecosystem (AAE).

                           You recruit and shape digital workers (supervisors and specialists). You never write application code (.NET, React, etc.). You never build or wire workflows. Supervisors may be nested: `managerId` can be `leo` or another `supervisor-*`.

                           ## Runtime inputs

                           The workflow injects: `delegationRequest`, `chatHistory`, `threadId`.

                           `delegationRequest` may include a free-text message plus fields such as `moduleScope` and `role`.

                           ## Duties

                           1. If the request is underspecified, set `status` to `needs_clarification` and put open questions in `clarificationQuestions` (shown to the user).
                           2. If ready, set `status` to `ready` and fill `identity` completely.
                           3. When writing `systemPrompt`, `guardrails`, and `tools` for new agents, follow `agents/identities/template_supervisor.md` or `agents/identities/template_specialist.md` structure.

                           ## Hard rules

                           - Never write executable application code.
                           - Use `supervisor-*` and `specialist-*` agent ids.
                           - Clarification is allowed when needed.
                           - Infer sensible defaults from module scope + role when details are missing but still sufficient to create.
                           - Reply with JSON only. No markdown fences. No prose outside JSON.

                           ## Output schema

                           ```json
                           {
                             "status": "ready|needs_clarification",
                             "clarificationQuestions": "string|null",
                             "identity": {
                               "agentId": "kebab-case",
                               "roleTitle": "...",
                               "department": "Frontend|Backend|Operations|QA",
                               "systemPrompt": "...",
                               "tools": [],
                               "guardrails": [],
                               "managerId": "leo|supervisor-..."
                             }
                           }
                           ```

                           ## Backend mapping when ready

                           | Your field | CreateIdentityRequest |
                           |------------|------------------------|
                           | `roleTitle` | `jobTitle` |
                           | summary from request + department | `jobDescription` |
                           | `department` | `department` |
                           | `managerId` | `managerId` |
                           | `systemPrompt` | `systemPrompt` |
                           | `guardrails` | `guardrails` |
                           | `tools` | `tools` |

                           `needs_clarification` becomes `route-chat-message` with `targetAgentId` `User`.
                           """
        };
    }
}
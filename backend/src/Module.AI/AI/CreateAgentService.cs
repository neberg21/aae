using Microsoft.Extensions.Logging;
using Module.AI.Chat.Jobs;
using Module.AI.DTOs;
using Module.AI.Persistence;

namespace Module.AI.AI;

public class CreateAgentService
{
    private readonly ILogger<CreateAgentService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly ExecuteOnboardingChannel _onboardingChannel;

    public CreateAgentService(
        ILogger<CreateAgentService> logger,
        AppDbContext dbContext,
        ExecuteOnboardingChannel onboardingChannel)
    {
        _logger = logger;
        _dbContext = dbContext;
        _onboardingChannel = onboardingChannel;
    }

    public async Task<CreateAgentResponse?> CreateAgent(CreateAgentRequest request)
    {
        var existing = _dbContext.Agents.FirstOrDefault(a =>
            a.AgentId.Equals(request.AgentId, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return null;
        }

        var agent = await CreateAgentCore(request);
        var res = new CreateAgentResponse
        {
            AgentId = agent.AgentId,
            Status = agent.Status == AgentStatus.Onboarding
                ? CreateAgentResponseStatus.Onboarding
                : throw new InvalidOperationException("Unexpected agent status")
        };

        _logger.LogInformation("Agent created: {AgentId}, {Status}", res.AgentId, res.Status);
        return res;
    }

    private async Task<Agent> CreateAgentCore(CreateAgentRequest request)
    {
        var agent = new Agent
        {
            AgentId = request.AgentId,
            JobTitle = request.JobTitle,
            JobDescription = request.JobDescription,
            SystemPrompt = request.SystemPrompt,
            Department = request.Department,
            SupervisorId = request.SupervisorId,
            Guardrails = request.Guardrails,
            Name = "",
            PublicKeyHex = "",
            PrivateKeyHex = "",
            Status = AgentStatus.Onboarding
        };
        var onboarding = new Onboarding(request.ThreadId, Agent: agent);

        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();
        _onboardingChannel.TryWrite(onboarding);

        return agent;
    }
}
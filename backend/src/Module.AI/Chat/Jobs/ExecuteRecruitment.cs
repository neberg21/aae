using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.AI.AI;
using Module.AI.DTOs;

namespace Module.AI.Chat.Jobs;

public class ExecuteRecruitment : ExecuteJob<Recruitment>
{
    private readonly ILogger<ExecuteJob<Recruitment>> _logger;

    public ExecuteRecruitment(
        ILogger<ExecuteJob<Recruitment>> logger,
        IServiceProvider serviceProvider,
        ExecuteRecruitmentChannel channel) : base(logger, serviceProvider, channel)
    {
        _logger = logger;
    }

    protected override async Task HandleItem(ExecuteJobContext<Recruitment> context,
        CancellationToken cancellationToken)
    {
        var recruitment = context.Item;
        var agentService = context.Services.GetRequiredService<CreateAgentService>();

        _logger.LogInformation("Processing recruitment: {Recruitment}", recruitment);

        if (recruitment.Status == RecruitingStatus.Ready)
        {
            await CreateNewAgent(recruitment, agentService);
        }
        else
        {
            _logger.LogWarning("Recruiting incomplete. status: {Status}, thread {ThreadId}, {AgentId}",
                recruitment.Status,
                recruitment.ThreadId,
                recruitment.AgentToRecruit.AgentId);
        }
    }

    private async Task CreateNewAgent(Recruitment recruitment, CreateAgentService agentService)
    {
        var agentToRecruit = recruitment.AgentToRecruit;
        var createAgentRequest = new CreateAgentRequest
        {
            ThreadId = recruitment.ThreadId,
            AgentId = agentToRecruit.AgentId,
            JobTitle = agentToRecruit.JobTitle,
            JobDescription = agentToRecruit.JobDescription,
            Department = agentToRecruit.Department.ToString(),
            SupervisorId = agentToRecruit.SupervisorId,
            SystemPrompt = agentToRecruit.SystemPrompt,
            Guardrails = agentToRecruit.Guardrails,
            Tools = []
        };
        await agentService.CreateAgent(createAgentRequest);

        _logger.LogInformation("Recruited agent: {AgentId} in {ThreadId}",
            agentToRecruit.AgentId,
            recruitment.ThreadId);
    }
}
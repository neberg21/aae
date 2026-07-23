using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Module.AI.AI;
using Module.AI.DTOs;
using Module.AI.Persistence;

namespace Module.AI.Chat;

public class RecruitingJob : BackgroundService
{
    private readonly ILogger<RecruitingJob> _logger;
    private readonly Channel<Vision> _channel;
    private readonly IServiceProvider _serviceProvider;

    public RecruitingJob(ILogger<RecruitingJob> logger, Channel<Vision> channel,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _channel = channel;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await _channel.Reader.WaitToReadAsync(stoppingToken))
                {
                    var serviceProvider = _serviceProvider.CreateScope().ServiceProvider;
                    var chatService = serviceProvider.GetRequiredService<ChatService>();
                    var agentService = serviceProvider.GetRequiredService<CreateAgentService>();
                    var vision = await _channel.Reader.ReadAsync(stoppingToken);
                    await HandleVision(vision, chatService, agentService);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error in RecruitingJob");
            }
        }
    }

    private async Task HandleVision(Vision vision, ChatService chatService, CreateAgentService agentService)
    {
        _logger.LogInformation("Processing vision: {Vision}", vision);

        foreach (var visionScope in vision.Scopes)
        {
            var recruitEmployeeRequest = new RecruitEmployeeRequest(
                vision.ThreadId,
                vision.AgentId,
                visionScope.SupervisorId,
                visionScope.Message);
            var result = await chatService.RecruitEmployee(recruitEmployeeRequest);

            if (result.Recruited is null)
            {
                _logger.LogWarning("Recruiting failed for scope: {Scope}, no response in {ThreadId}",
                    visionScope, vision.ThreadId);
                continue;
            }

            var recruited = result.Recruited;

            if (recruited.Status == RecruitingStatus.Ready)
            {
                await CreateNewAgent(vision, agentService, recruited);
            }
            else
            {
                _logger.LogWarning("Recruiting failed for scope: {Scope}, status: {Status}", visionScope,
                    recruited.Status);
            }
        }
    }

    private async Task CreateNewAgent(Vision vision, CreateAgentService agentService, RecruitingResponse recruited)
    {
        var recruitingAgent = recruited.Agent;
        var createAgentRequest = new CreateAgentRequest
        {
            ThreadId = recruited.ThreadId,
            AgentId = recruitingAgent.AgentId,
            JobTitle = recruitingAgent.JobTitle,
            JobDescription = recruitingAgent.JobDescription,
            Department = recruitingAgent.Department,
            SupervisorId = recruitingAgent.SupervisorId,
            SystemPrompt = recruitingAgent.SystemPrompt,
            Guardrails = [],
            Tools = []
        };
        await agentService.CreateAgent(createAgentRequest);

        _logger.LogInformation("Recruited agent: {AgentId} in {ThreadId}", recruitingAgent.AgentId,
            vision.ThreadId);
    }
}
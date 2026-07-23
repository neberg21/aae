using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Module.AI.AI;
using Module.AI.DTOs;

namespace Module.AI.Chat.Jobs;

public class ExecuteRecruiting : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<RecruitingResponse> _channel;
    private readonly ILogger<ExecuteRecruiting> _logger;


    public ExecuteRecruiting(
        ILogger<ExecuteRecruiting> logger,
        IServiceProvider serviceProvider,
        Channel<RecruitingResponse> channel)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _channel = channel;
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
                    var agentService = serviceProvider.GetRequiredService<CreateAgentService>();
                    var vision = await _channel.Reader.ReadAsync(stoppingToken);
                    await HandleRecruiting(vision, agentService);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error in ExecuteRecruiting");
            }
        }
    }

    private async Task HandleRecruiting(
        RecruitingResponse recruited,
        CreateAgentService agentService)
    {
        if (recruited.Status == RecruitingStatus.Ready)
        {
            await CreateNewAgent(recruited, agentService);
        }
        else
        {
            _logger.LogWarning("Recruiting incomplete. status: {Status}, thread {ThreadId}, {AgentId}",
                recruited.Status,
                recruited.ThreadId,
                recruited.Agent.AgentId);
        }
    }

    private async Task CreateNewAgent(RecruitingResponse recruited, CreateAgentService agentService)
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

        _logger.LogInformation("Recruited agent: {AgentId} in {ThreadId}",
            recruitingAgent.AgentId,
            recruited.ThreadId);
    }
}
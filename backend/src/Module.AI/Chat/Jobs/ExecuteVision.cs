using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Module.AI.DTOs;

namespace Module.AI.Chat.Jobs;

public class RecruitingJob : BackgroundService
{
    private readonly ILogger<RecruitingJob> _logger;
    private readonly Channel<Vision> _channel;
    private readonly IServiceProvider _serviceProvider;

    public RecruitingJob(
        ILogger<RecruitingJob> logger,
        Channel<Vision> channel,
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
                    var vision = await _channel.Reader.ReadAsync(stoppingToken);
                    await HandleVision(vision, chatService);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error in ExecuteVision");
            }
        }
    }

    private async Task HandleVision(Vision vision, ChatService chatService)
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
            }
        }
    }
}
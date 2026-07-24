using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.AI.DTOs;

namespace Module.AI.Chat.Jobs;

public class RecruitingJob : ExecuteJob<Vision>
{
    private readonly ILogger<RecruitingJob> _logger;

    public RecruitingJob(
        ILogger<RecruitingJob> logger,
        IServiceProvider serviceProvider,
        ExecuteVisionChannel channel) : base(logger, serviceProvider, channel)
    {
        _logger = logger;
    }

    protected override async Task HandleItem(ExecuteJobContext<Vision> context, CancellationToken cancellationToken)
    {
        var vision = context.Item;
        var chatService = context.Services.GetRequiredService<ChatService>();

        _logger.LogInformation("Processing vision: {Vision}", vision);

        foreach (var scope in vision.Scopes)
        {
            var recruitEmployeeRequest = new RecruitEmployeeRequest(
                vision.ThreadId,
                vision.AgentId,
                scope.SupervisorId,
                scope.Message);
            var result = await chatService.RecruitEmployee(recruitEmployeeRequest);

            if (result.Recruited is null)
            {
                _logger.LogWarning("Recruiting failed for scope: {Scope}, no response in {ThreadId}",
                    scope, vision.ThreadId);
            }
        }
    }
}
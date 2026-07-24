using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.AI.DTOs;
using Module.AI.Nostr;
using Module.AI.Persistence;

namespace Module.AI.Chat.Jobs;

public class ExecuteOnboarding : ExecuteJob<Onboarding>
{
    private readonly ILogger<ExecuteJob<Onboarding>> _logger;

    public ExecuteOnboarding(
        ILogger<ExecuteJob<Onboarding>> logger,
        IServiceProvider serviceProvider,
        ExecuteOnboardingChannel channel) : base(logger, serviceProvider, channel)
    {
        _logger = logger;
    }

    protected override async Task HandleItem(ExecuteJobContext<Onboarding> context, CancellationToken cancellationToken)
    {
        var onboarding = context.Item;
        _logger.LogInformation("Onboarding agent: {Agent} in thread {ThreadId}", onboarding.Agent, onboarding.ThreadId);

        if (onboarding.Agent.Id.StartsWith("supervisor-"))
        {
            await OnboardSupervisor(context);
        }
        else if (onboarding.Agent.Id.StartsWith("specialist-"))
        {
            await OnboardSpecialist(context);
        }
        else
        {
            _logger.LogError(
                new NotSupportedException("Invalid agent type"),
                "Invalid agent type: {AgentId} in thread {ThreadId}",
                onboarding.Agent.Id,
                onboarding.ThreadId);
        }
    }

    private static async Task OnboardSupervisor(ExecuteJobContext<Onboarding> context)
    {
        var onboarding = context.Item;
        var defineEmployeesRequest = new DefineEmployeesRequest(onboarding.ThreadId, onboarding.Agent);
        var chatService = context.Services.GetRequiredService<ChatService>();
        var response = await chatService.DefineEmployees(defineEmployeesRequest);

        foreach (var employee in response.Team)
        {
            var recruitEmployeeRequest = new RecruitEmployeeRequest(
                response.ThreadId,
                onboarding.Agent.Id,
                employee.AgentId,
                employee.Message);
            await chatService.RecruitEmployee(recruitEmployeeRequest);
        }

        await SetAgentInfo(context);
    }

    private async Task OnboardSpecialist(ExecuteJobContext<Onboarding> context)
    {
        await SetAgentInfo(context);
    }

    private static async Task SetAgentInfo(ExecuteJobContext<Onboarding> context)
    {
        var agent = context.Item.Agent;
        var faker = context.Services.GetRequiredService<Faker>();
        var profileGenerator = context.Services.GetRequiredService<ProfileGenerator>();
        var keyPair = NostrKeyPair.GenerateKeyPair();
        var firstName = faker.Person.FirstName;
        var profile = await profileGenerator.CreateProfileAsync(keyPair, firstName);

        agent.Name = profile.Name;
        agent.PublicKeyHex = keyPair.PublicKeyHex;
        agent.PrivateKeyHex = keyPair.PrivateKeyHex;
        agent.Status = AgentStatus.Working;

        var dbContext = context.Services.GetRequiredService<AppDbContext>();
        await dbContext.SaveChangesAsync();
    }
}
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.AI.Nostr;
using Module.AI.Persistence;

namespace Module.AI.Chat.Jobs;

public class ExecuteOnboarding : ExecuteJob<Agent>
{
    private readonly ILogger<ExecuteJob<Agent>> _logger;

    public ExecuteOnboarding(
        ILogger<ExecuteJob<Agent>> logger,
        IServiceProvider serviceProvider,
        ExecuteOnboardingChannel channel) : base(logger, serviceProvider, channel)
    {
        _logger = logger;
    }

    protected override async Task HandleItem(ExecuteJobContext<Agent> context, CancellationToken cancellationToken)
    {
        var agent = context.Item;
        _logger.LogInformation("Onboarding agent: {Agent}", agent);

        if (agent.Id.StartsWith("supervisor-"))
        {
            await OnboardSupervisor(context);
        }
        else if (agent.Id.StartsWith("specialist-"))
        {
            await OnboardSpecialist(context);
        }
        else
        {
            _logger.LogError(
                new NotSupportedException("Invalid agent type"),
                "Invalid agent type: {AgentId}",
                agent.Id);
        }
    }

    private static async Task OnboardSupervisor(ExecuteJobContext<Agent> context)
    {
        var dbContext = context.Services.GetRequiredService<AppDbContext>();
        await SetAgentInfo(context, dbContext);
    }

    private async Task OnboardSpecialist(ExecuteJobContext<Agent> context)
    {
        var dbContext = context.Services.GetRequiredService<AppDbContext>();
        await SetAgentInfo(context, dbContext);
    }

    private static async Task SetAgentInfo(ExecuteJobContext<Agent> context, AppDbContext dbContext)
    {
        var agent = context.Item;
        var faker = context.Services.GetRequiredService<Faker>();
        var profileGenerator = context.Services.GetRequiredService<ProfileGenerator>();
        var keyPair = NostrKeyPair.GenerateKeyPair();
        var firstName = faker.Person.FirstName;
        var profile = await profileGenerator.CreateProfileAsync(keyPair, firstName);

        agent.Name = profile.Name;
        agent.PublicKeyHex = keyPair.PublicKeyHex;
        agent.PrivateKeyHex = keyPair.PrivateKeyHex;
        agent.Status = AgentStatus.Working;

        await dbContext.SaveChangesAsync();
    }
}
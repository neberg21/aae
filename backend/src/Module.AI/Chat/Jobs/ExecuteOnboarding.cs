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
        var dbContext = context.Services.GetRequiredService<AppDbContext>();

        _logger.LogInformation("Onboarding agent: {Agent}", agent);

        var faker = context.Services.GetRequiredService<Faker>();
        var profileGenerator = context.Services.GetRequiredService<ProfileGenerator>();
        var keyPair = NostrKeyPair.GenerateKeyPair();
        var firstName = faker.Person.FirstName;
        var profile = await profileGenerator.CreateProfileAsync(keyPair, firstName);

        agent.Name = profile.Name;
        agent.PublicKeyHex = keyPair.PublicKeyHex;
        agent.PrivateKeyHex = keyPair.PrivateKeyHex;

        await dbContext.SaveChangesAsync();
    }
}
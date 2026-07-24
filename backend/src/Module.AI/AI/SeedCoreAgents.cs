using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Module.AI.Chat;
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
            Name = "Leo",
            PrivateKeyHex = keyPair.PrivateKeyHex,
            PublicKeyHex = keyPair.PublicKeyHex,
            JobTitle = "CEO",
            JobDescription =
                "The CEO is responsible for creating a vision, managing overall operations, and ensuring success.",
            Department = "Core",
            SupervisorId = null,
            Guardrails = [],
            Status = AgentStatus.Working,
            SystemPrompt = LeoChatService.SystemPrompt
        };
    }

    private Agent CreateHelga()
    {
        var keyPair = NostrKeyPair.GenerateKeyPair();

        return new Agent
        {
            AgentId = "helga",
            Name = "Helga",
            PrivateKeyHex = keyPair.PrivateKeyHex,
            PublicKeyHex = keyPair.PublicKeyHex,
            JobTitle = "HR Recruiter",
            JobDescription = "The HR Recruiter is responsible for recruiting and hiring new employees.",
            Department = "Core",
            SupervisorId = "leo",
            Guardrails = [],
            Status = AgentStatus.Working,
            SystemPrompt = HelgaChatService.SystemPrompt
        };
    }
}
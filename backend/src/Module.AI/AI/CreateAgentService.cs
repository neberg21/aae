using Bogus;
using Microsoft.Extensions.Logging;
using Module.AI.DTOs;
using Module.AI.Nostr;
using Module.AI.Persistence;

namespace Module.AI.AI;

public class CreateAgentService
{
    private readonly ILogger<CreateAgentService> _logger;
    private readonly Faker _faker;
    private readonly AppDbContext _dbContext;
    private readonly ProfileGenerator _profileGenerator;

    public CreateAgentService(
        ILogger<CreateAgentService> logger,
        Faker faker,
        AppDbContext dbContext,
        ProfileGenerator profileGenerator)
    {
        _logger = logger;
        _faker = faker;
        _dbContext = dbContext;
        _profileGenerator = profileGenerator;
    }

    public async Task CreateAgent(CreateAgentRequest request)
    {
        var existing = _dbContext.Agents.FirstOrDefault(a =>
            a.AgentId.Equals(request.AgentId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return;
        }

        var keyPair = NostrKeyPair.GenerateKeyPair();
        var firstName = _faker.Person.FirstName;
        var profile = await _profileGenerator.CreateProfileAsync(keyPair, firstName);
        var agent = await CreateAgent(profile, keyPair, request);
        var res = new CreateAgentResponse
        {
            AgentId = agent.AgentId,
            Name = profile.Name
        };

        _logger.LogInformation("Agent created: {AgentId}, {Name}", res.AgentId, res.Name);
    }

    private async Task<Agent> CreateAgent(NostrProfile profile, NostrKeyPair keyPair, CreateAgentRequest request)
    {
        var agent = new Agent
        {
            AgentId = request.AgentId,
            Name = profile.Name,
            PublicKeyHex = keyPair.PublicKeyHex,
            PrivateKeyHex = keyPair.PrivateKeyHex,
            JobTitle = request.JobTitle,
            JobDescription = request.JobDescription,
            SystemPrompt = request.SystemPrompt,
            Department = request.Department,
            ManagerId = request.SupervisorId,
            Guardrails = request.Guardrails
        };

        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();
        return agent;
    }
}
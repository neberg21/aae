using Bogus;
using Microsoft.Extensions.Logging;
using Module.AI.DTOs;
using Module.AI.Nostr;
using Module.AI.Persistence;

namespace Module.AI.AI;

public class CreateIdentityService
{
    private readonly ILogger<CreateIdentityService> _logger;
    private readonly Faker _faker;
    private readonly AppDbContext _dbContext;
    private readonly ProfileGenerator _profileGenerator;
    private readonly ParkDelegationService _parkDelegationService;
    private readonly RouteChatMessageService _routeChatMessageService;

    public CreateIdentityService(
        ILogger<CreateIdentityService> logger,
        Faker faker,
        AppDbContext dbContext,
        ProfileGenerator profileGenerator,
        ParkDelegationService parkDelegationService,
        RouteChatMessageService routeChatMessageService)
    {
        _logger = logger;
        _faker = faker;
        _dbContext = dbContext;
        _profileGenerator = profileGenerator;
        _parkDelegationService = parkDelegationService;
        _routeChatMessageService = routeChatMessageService;
    }

    public async Task<CreateAgentResponse?> CreateIdentity(CreateAgentRequest request)
    {
        var existing = _dbContext.Agents.FirstOrDefault(a =>
            a.AgentId.Equals(request.AgentId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return null;
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

        _logger.LogInformation("Identity created: {AgentId}, {Name}", res.AgentId, res.Name);
        await AddResponseMessage(request.ThreadId, res);
        await ExecuteParkedEntries(agent);

        return res;
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
            ManagerId = request.ManagerId,
            Guardrails = request.Guardrails
        };

        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();
        return agent;
    }

    private async Task AddResponseMessage(string threadId, CreateAgentResponse res)
    {
        var message = new ChatMessage
        {
            Receiver = "leo",
            Sender = "helga",
            ThreadId = threadId,
            Content = $"Identity created successfully. AgentId: {res.AgentId}, Name: {res.Name}"
        };
        _dbContext.ChatMessages.Add(message);
        await _dbContext.SaveChangesAsync();
    }

    private async Task ExecuteParkedEntries(Agent agent)
    {
        var parked = _parkDelegationService.DequeueByTargetAgentId(agent.AgentId);
        foreach (var item in parked)
        {
            var routeRequest = new RouteChatMessageRequest
            {
                ThreadId = item.ThreadId,
                SenderAgentId = item.SenderAgentId,
                TargetAgentId = item.TargetAgentId,
                Content = item.Content
            };

            await _routeChatMessageService.RouteChatMessage(routeRequest);
            _logger.LogInformation(
                "Parked entry executed: {ThreadId}, {SenderAgentId}, {TargetAgentId}",
                item.ThreadId,
                item.SenderAgentId,
                item.TargetAgentId);
        }
    }
}
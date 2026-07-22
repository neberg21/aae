using Bogus;
using Module.Agents.DTOs;
using Module.Agents.Nostr;
using Module.Agents.Persistence;

namespace Module.Agents.AI;

public class CreateIdentityService
{
    private readonly Faker _faker;
    private readonly AppDbContext _dbContext;
    private readonly ProfileGenerator _profileGenerator;
    private readonly ParkDelegationService _parkDelegationService;
    private readonly RouteChatMessageService _routeChatMessageService;

    public CreateIdentityService(
        Faker faker,
        AppDbContext dbContext,
        ProfileGenerator profileGenerator,
        ParkDelegationService parkDelegationService,
        RouteChatMessageService routeChatMessageService)
    {
        _faker = faker;
        _dbContext = dbContext;
        _profileGenerator = profileGenerator;
        _parkDelegationService = parkDelegationService;
        _routeChatMessageService = routeChatMessageService;
    }

    public async Task<CreateIdentityResponse?> CreateIdentity(CreateIdentityRequest request)
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
            try
            {
                await _routeChatMessageService.RouteChatMessage(routeRequest);
            }
            catch
            {
                // create already committed; wake best-effort
            }
        }

        var res = new CreateIdentityResponse
        {
            AgentId = agent.AgentId,
            Name = profile.Name
        };

        return res;
    }

    private async Task<Agent> CreateAgent(NostrProfile profile, NostrKeyPair keyPair, CreateIdentityRequest request)
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
}

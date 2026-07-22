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

    public CreateIdentityService(Faker faker, AppDbContext dbContext, ProfileGenerator profileGenerator)
    {
        _faker = faker;
        _dbContext = dbContext;
        _profileGenerator = profileGenerator;
    }

    public async Task<CreateIdentityResponse> CreateIdentity(CreateIdentityRequest request)
    {
        var keyPair = NostrKeyPair.GenerateKeyPair();
        var firstName = _faker.Person.FirstName;
        var profile = await _profileGenerator.CreateProfileAsync(keyPair, firstName);
        var agent = await CreateAgent(profile, keyPair, request);
        var res = new CreateIdentityResponse
        {
            AgentId = agent.Id,
            Name = profile.Name
        };

        return res;
    }

    private async Task<Agent> CreateAgent(NostrProfile profile, NostrKeyPair keyPair, CreateIdentityRequest request)
    {
        var agent = new Agent
        {
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
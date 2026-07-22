using Bogus;
using Module.Agents.DTOs;
using Module.Agents.Nostr;
using Module.Agents.Persistence;

namespace Module.Agents.AI;

public class CreateIdentityService
{
    private readonly Faker _faker;
    private readonly AppDbContext _dbContext;

    public CreateIdentityService(Faker faker, AppDbContext dbContext)
    {
        _faker = faker;
        _dbContext = dbContext;
    }

    public async Task<CreateIdentityResponse> CreateIdentity(CreateIdentityRequest request)
    {
        var firstName = _faker.Person.FirstName;
        var profile = await ProfileGenerator.CreateProfileAsync(firstName);
        var res = new CreateIdentityResponse
        {
            Name = profile.Name,
            PublicKeyHex = profile.PublicKeyHex
        };

        await CreateAgent(request, profile);

        return res;
    }

    private async Task CreateAgent(CreateIdentityRequest request, NostrProfile profile)
    {
        var agent = new Agent
        {
            Name = profile.Name,
            PublicKeyHex = profile.PublicKeyHex,
            PrivateKeyHex = profile.PrivateKeyHex,
            JobTitle = request.JobTitle,
            JobDescription = request.JobDescription,
            SystemPrompt = request.SystemPrompt
        };

        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();
    }
}
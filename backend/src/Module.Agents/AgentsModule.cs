using Bogus;
using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.Agents.Nostr;

namespace Module.Agents;

public class AgentsModule : IModule
{
    public string Name => "agents";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddHostedService<ListenOnMessages>();

        services.AddScoped<Faker>(_ => new Faker("de"));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("agents");

        api.MapPost("route-chat-message", RouteChatMessage);
        api.MapPost("create-identity", CreateIdentity);
        api.MapPost("await-request-approval", AwaitRequestApproval);
        api.MapPost("resolve-request-approval", ResolveRequestApproval);
    }

    private Task RouteChatMessage(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task<IResult> CreateIdentity(Faker faker)
    {
        var firstName = faker.Person.FirstName;
        var profile = await ProfileGenerator.CreateProfileAsync(firstName);

        return Results.Ok(profile);
    }

    private async Task<IResult> AwaitRequestApproval()
    {
        throw new NotImplementedException();
    }

    private Task ResolveRequestApproval(HttpContext context)
    {
        throw new NotImplementedException();
    }
}
using Bogus;
using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.Agents.DTOs;
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
        endpoints.MapPost("route-chat-message", RouteChatMessage);
        endpoints.MapPost("create-identity", CreateIdentity);
        endpoints.MapPost("await-request-approval", AwaitRequestApproval);
        endpoints.MapPost("resolve-request-approval", ResolveRequestApproval);
    }

    private Task RouteChatMessage(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task<IResult> CreateIdentity(Faker faker)
    {
        var firstName = faker.Person.FirstName;
        var profile = await ProfileGenerator.CreateProfileAsync(firstName);
        var res = new CreateIdentityResponse
        {
            Name = profile.Name,
            PublicKeyHex = profile.PublicKeyHex
        };

        return Results.Ok(res);
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
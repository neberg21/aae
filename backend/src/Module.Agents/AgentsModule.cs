using Bogus;
using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.Agents.AI;
using Module.Agents.DTOs;
using Module.Agents.Nostr;
using Module.Agents.Persistence;

namespace Module.Agents;

public class AgentsModule : IModule
{
    public string Name => "agents";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddHostedService<ListenOnMessages>();
        services.AddSingleton<AppDbContext>();

        services.AddScoped<ChatHub>();
        services.AddScoped<AgentOrchestrationService>();
        services.AddScoped<Faker>(_ => new Faker("de"));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("create-identity", CreateIdentity);
        endpoints.MapPost("route-chat-message", RouteChatMessage);
        endpoints.MapPost("await-request-approval", AwaitRequestApproval);
        endpoints.MapPost("resolve-request-approval", ResolveRequestApproval);
    }

    private static async Task<IResult> CreateIdentity(
        [FromBody] CreateIdentityRequest request, Faker faker, AppDbContext dbContext)
    {
        var firstName = faker.Person.FirstName;
        var profile = await ProfileGenerator.CreateProfileAsync(firstName);
        var agent = new Agent
        {
            Name = profile.Name,
            PublicKeyHex = profile.PublicKeyHex,
            PrivateKeyHex = profile.PrivateKeyHex,
            JobTitle = request.JobTitle,
            JobDescription = request.JobDescription,
            SystemPrompt = request.SystemPrompt
        };

        dbContext.Agents.Add(agent);
        await dbContext.SaveChangesAsync();

        var res = new CreateIdentityResponse
        {
            Name = profile.Name,
            PublicKeyHex = profile.PublicKeyHex
        };

        return Results.Ok(res);
    }

    private static async Task<IResult> RouteChatMessage(
        [FromBody] RouteChatMessageRequest request,
        AgentOrchestrationService agentOrchestrationService)
    {
        await agentOrchestrationService.ProcessAgentMessageAsync(request);
        throw new NotImplementedException();
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
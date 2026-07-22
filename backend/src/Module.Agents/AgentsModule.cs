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

namespace Module.Agents;

public class AgentsModule : IModule
{
    public string Name => "agents";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddHostedService<ListenOnMessages>();
        services.AddSingleton<AppDbContext>();
        services.AddScoped<ChatHub>();
        services.AddScoped<NostrEventService>();

        services.AddScoped<CreateIdentityService>();
        services.AddScoped<ProfileGenerator>();
        services.AddScoped<Faker>(_ => new Faker("de"));
        services.AddHttpClient<RouteChatMessageService>();
        services.AddScoped<RouteChatMessageService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("create-identity", CreateIdentity);
        endpoints.MapPost("route-chat-message", RouteChatMessage);
        endpoints.MapPost("await-request-approval", AwaitRequestApproval);
        endpoints.MapPost("resolve-request-approval", ResolveRequestApproval);
        endpoints.MapPost("execute-tool", ExecuteTool);
    }

    private static async Task<IResult> CreateIdentity(
        [FromBody] CreateIdentityRequest request,
        CreateIdentityService createIdentityService)
    {
        var res = await createIdentityService.CreateIdentity(request);
        return Results.Ok(res);
    }

    private static async Task<IResult> RouteChatMessage(
        [FromBody] RouteChatMessageRequest request,
        RouteChatMessageService routeChatMessageService)
    {
        var res = await routeChatMessageService.RouteChatMessage(request);
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

    private Task ExecuteTool(HttpContext context)
    {
        throw new NotImplementedException();
    }
}
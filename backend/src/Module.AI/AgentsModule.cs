using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.AI;
using Module.AI.DTOs;
using Module.AI.Nostr;

namespace Module.AI;

public class AgentsModule : IModule
{
    public string GroupName => "ai";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddHostedService<SeedCoreAgents>();
        services.AddHostedService<ListenOnMessages>();

        services.AddSingleton<CoreAgentService>();

        services.AddScoped<GetAgentByIdService>();
        services.AddScoped<SearchIdentityService>();
        services.AddScoped<CreateAgentService>();
        services.AddScoped<ParkDelegationService>();
        services.AddHttpClient<RouteChatMessageService>();
        services.AddScoped<RouteChatMessageService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints = endpoints.MapGroup("agents");

        endpoints.MapGet("", GetAgents)
            .Produces<GetAgentsResponse>();
        endpoints.MapGet("{agentId}", GetAgent)
            .Produces<GetAgentByIdResponse>();
        endpoints.MapGet("search", SearchAgents7)
            .Produces<GetAgentsResponse>();

        var actions = endpoints.MapGroup("actions");

        actions.MapPost("park-delegation", ParkDelegation)
            .Accepts<ParkDelegationRequest>("application/json")
            .Produces<ParkDelegationResponse>();
        actions.MapPost("route-chat-message", RouteChatMessage)
            .Accepts<RouteChatMessageRequest>("application/json")
            .Produces<RouteChatMessageResponse>();
        actions.MapPost("await-request-approval", AwaitRequestApproval);
        actions.MapPost("resolve-request-approval", ResolveRequestApproval);
        actions.MapPost("execute-tool", ExecuteTool);
    }

    private IResult GetAgents(SearchIdentityService searchIdentityService)
    {
        var page = searchIdentityService.GetAgents();
        return Results.Ok(page);
    }

    private IResult GetAgent(string agentId, GetAgentByIdService agentService)
    {
        var result = agentService.GetById(agentId);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private IResult SearchAgents7(
        [FromQuery] string? agentId,
        [FromQuery] string? name,
        [FromQuery] string? department,
        [FromQuery] string? jobTitle,
        SearchIdentityService searchIdentityService)
    {
        var page = searchIdentityService.SearchIdentities(
            agentId, name, department, jobTitle);

        return Results.Ok(page);
    }

    private static async Task<IResult> ParkDelegation(
        [FromBody] ParkDelegationRequest request,
        ParkDelegationService parkDelegationService)
    {
        var res = await parkDelegationService.Park(request);
        return Results.Ok(res);
    }

    private static async Task<IResult> RouteChatMessage(
        [FromBody] RouteChatMessageRequest request,
        RouteChatMessageService routeChatMessageService)
    {
        var res = await routeChatMessageService.RouteChatMessage(request);
        return Results.Ok(res);
    }

    private IResult AwaitRequestApproval()
    {
        throw new NotImplementedException();
    }

    private IResult ResolveRequestApproval()
    {
        throw new NotImplementedException();
    }

    private IResult ExecuteTool()
    {
        throw new NotImplementedException();
    }
}
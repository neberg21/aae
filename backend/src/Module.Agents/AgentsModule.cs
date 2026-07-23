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
    public string GroupName => "agents";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddHostedService<SeedCoreAgents>();
        services.AddHostedService<ListenOnMessages>();

        services.AddScoped<GetAgentByIdService>();
        services.AddScoped<SearchIdentityService>();
        services.AddScoped<CreateIdentityService>();
        services.AddScoped<ParkDelegationService>();
        services.AddScoped<ProfileGenerator>();
        services.AddHttpClient<RouteChatMessageService>();
        services.AddScoped<RouteChatMessageService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("", GetAgents)
            .Produces<GetAgentsResponse>();
        endpoints.MapPost("", CreateAgent)
            .Accepts<CreateAgentRequest>("application/json")
            .Produces<CreateAgentResponse>();
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

    private static async Task<IResult> CreateAgent(
        [FromBody] CreateAgentRequest request,
        CreateIdentityService createIdentityService)
    {
        var res = await createIdentityService.CreateIdentity(request);
        if (res is null)
        {
            return Results.Conflict();
        }

        return Results.Ok(res);
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
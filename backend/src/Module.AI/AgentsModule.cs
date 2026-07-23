using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.AI;
using Module.AI.DTOs;

namespace Module.AI;

public class AgentsModule : IModule
{
    public string GroupName => "ai";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddHostedService<SeedCoreAgents>();

        services.AddSingleton<CoreAgentService>();

        services.AddScoped<GetAgentByIdService>();
        services.AddScoped<SearchIdentityService>();
        services.AddScoped<CreateAgentService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints = endpoints.MapGroup("agents");

        endpoints.MapGet("", GetAgents)
            .Produces<GetAgentsResponse>();
        endpoints.MapGet("{agentId}", GetAgent)
            .Produces<GetAgentByIdResponse>();
        endpoints.MapGet("search", SearchAgents)
            .Produces<GetAgentsResponse>();

        var actions = endpoints.MapGroup("actions");

        actions.MapPost("await-request-approval", AwaitRequestApproval);
        actions.MapPost("resolve-request-approval", ResolveRequestApproval);
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

    private IResult SearchAgents(
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

    private IResult AwaitRequestApproval()
    {
        throw new NotImplementedException();
    }

    private IResult ResolveRequestApproval()
    {
        throw new NotImplementedException();
    }
}
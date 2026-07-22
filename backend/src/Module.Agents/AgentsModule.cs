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
        services.AddHostedService<SeedCoreAgents>();
        services.AddHostedService<ListenOnMessages>();

        services.AddSingleton<AppDbContext>();
        services.AddScoped<ChatHub>();
        services.AddScoped<NostrEventService>();

        services.AddScoped<SearchIdentityService>();
        services.AddScoped<CreateIdentityService>();
        services.AddScoped<ProfileGenerator>();
        services.AddScoped<Faker>(_ => new Faker("de"));
        services.AddHttpClient<RouteChatMessageService>();
        services.AddScoped<RouteChatMessageService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ChatHub>("/chat");

        endpoints.MapGet("", GetIdentities)
            .Produces<GetAgentsResponse>();
        endpoints.MapGet("{agentId}", GetIdentity)
            .Produces<GetAgentByIdResponse>();
        endpoints.MapGet("search", SearchIdentities)
            .Produces<GetAgentsResponse>();

        endpoints.MapPost("create-identity", CreateIdentity)
            .Accepts<CreateIdentityRequest>("application/json")
            .Produces<CreateIdentityResponse>();
        endpoints.MapPost("route-chat-message", RouteChatMessage)
            .Accepts<RouteChatMessageRequest>("application/json")
            .Produces<RouteChatMessageResponse>();
        endpoints.MapPost("await-request-approval", AwaitRequestApproval);
        endpoints.MapPost("resolve-request-approval", ResolveRequestApproval);
        endpoints.MapPost("execute-tool", ExecuteTool);
    }

    private IResult GetIdentities(AppDbContext dbContext)
    {
        var agents = dbContext.Agents.ToArray();
        var page = GetAgentsResponse(agents);

        return Results.Ok(page);
    }

    private IResult GetIdentity(string agentId, AppDbContext dbContext)
    {
        var agent = dbContext.Agents.FirstOrDefault(a => a.Id == agentId);
        return agent is null
            ? Results.NotFound()
            : Results.Ok(new GetAgentByIdResponse(
                agent.PublicKeyHex,
                agent.Name,
                agent.Department,
                agent.JobTitle,
                agent.SystemPrompt));
    }

    private IResult SearchIdentities(
        [FromQuery] string? agentId,
        [FromQuery] string? name,
        [FromQuery] string? department,
        [FromQuery] string? jobTitle,
        SearchIdentityService searchIdentityService)
    {
        var items = searchIdentityService.SearchIdentities(
            agentId, name, department, jobTitle);
        var page = GetAgentsResponse(items);

        return Results.Ok(page);
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

    private static GetAgentsResponse GetAgentsResponse(IReadOnlyCollection<Agent> agents)
    {
        var page = new GetAgentsResponse
        {
            Items = agents.Select(a => new AgentDto(a.Id, a.Name, a.Department, a.JobTitle)).ToArray(),
            TotalCount = agents.Count,
            PageSize = agents.Count,
            PageNumber = 1,
            TotalPages = (int)Math.Ceiling((double)agents.Count / agents.Count)
        };
        return page;
    }
}
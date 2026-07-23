using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.Agents.AI;
using Module.Agents.DTOs;
using Module.Agents.Threads;

namespace Module.Agents;

public class ThreadsModule : IModule
{
    public string GroupName => "agents";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<GetThreadsService>();
        services.AddScoped<GetThreadByIdService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("", GetThreads)
            .Produces<GetThreadsResponse>();
        endpoints.MapGet("{threadId}", GetThread)
            .Produces<GetThreadResponse>();
    }

    private static IResult GetThread(string threadId, GetThreadByIdService threadService)
    {
        var thread = threadService.GetById(threadId);
        return thread is null ? Results.NotFound() : Results.Ok(thread);
    }

    private static IResult GetThreads(GetThreadsService threadService)
    {
        var page = threadService.GetThreads();
        return Results.Ok(page);
    }
}
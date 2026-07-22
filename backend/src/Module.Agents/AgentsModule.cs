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

    private Task CreateIdentity(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private Task AwaitRequestApproval(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private Task ResolveRequestApproval(HttpContext context)
    {
        throw new NotImplementedException();
    }
}
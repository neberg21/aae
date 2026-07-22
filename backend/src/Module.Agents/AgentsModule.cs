using Core;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Module.Agents;

public class AgentsModule : IModule
{
    public string Name => "agents";

    public void RegisterServices(IServiceCollection services)
    {
        throw new NotImplementedException();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        throw new NotImplementedException();
    }
}
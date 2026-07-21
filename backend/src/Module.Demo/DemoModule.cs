using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Module.Demo;

public sealed class DemoModule : IModule
{
    public string Name => "demo";

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/ping", () => "pong");
    }
}

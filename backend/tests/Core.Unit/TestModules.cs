using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Unit;

public sealed class ValidTestModule : IModule
{
    public string GroupName => "test";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<ValidTestModuleMarker>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/test/ping", () => "ok");
    }
}

public sealed class ValidTestModuleMarker
{
}

public abstract class AbstractTestModule : IModule
{
    public string GroupName => "abstract";

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}

public sealed class BrokenTestModule : IModule
{
    public BrokenTestModule(string required)
    {
        _ = required;
    }

    public string GroupName => "broken";

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}

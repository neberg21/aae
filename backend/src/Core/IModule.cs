using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public interface IModule
{
    string GroupName { get; }

    void RegisterServices(IServiceCollection services);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class ModuleEndpointRouteBuilderExtensions
{
    public static WebApplication MapModules(this WebApplication app)
    {
        app.MapControllers();

        var modules = app.Services.GetRequiredService<IModuleCollection>();
        foreach (var module in modules)
        {
            module.MapEndpoints(app);
        }

        return app;
    }
}

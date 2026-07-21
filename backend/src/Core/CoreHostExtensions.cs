using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Core;

public static class CoreHostExtensions
{
    /// <summary>
    /// One-time host wiring: OpenAPI documents + controllers + all discovered modules.
    /// Program.cs calls this once; adding a Module.* never changes Program.cs.
    /// </summary>
    public static WebApplication UseCoreHost(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        var modules = app.Services.GetRequiredService<IModuleCollection>();
        var api = app.MapGroup("/api");
        foreach (var module in modules)
        {
            var moduleApi = api.MapGroup($"/{module.Name}");
            module.MapEndpoints(moduleApi);
            app.MapScalarApiReference(
                $"/scalar/{module.Name}",
                options =>
                {
                    options.Title = $"Scalar API Reference for {module.Name}";
                    options.OpenApiRoutePattern = $"/{module.Name}/openapi.json";
                });
        }

        return app;
    }
}
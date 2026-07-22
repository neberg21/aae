using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        app.MapOpenApi();
        app.Use(async (context, next) =>
        {
            var requestId = Guid.NewGuid().ToString("N");
            var logger = context.RequestServices.GetRequiredService<ILogger<ICoreMarker>>();
            logger.LogInformation("Request {RequestId}: {RequestPath} ", requestId, context.Request.Path);
            await next(context);
            logger.LogInformation("Response {RequestId}: {StatusCode} {RequestPath}",
                requestId,
                context.Response.StatusCode,
                context.Request.Path);
        });

        var modules = app.Services.GetRequiredService<IModuleCollection>();
        var api = app.MapGroup("/api");
        foreach (var module in modules)
        {
            var moduleApi = api.MapGroup($"/{module.Name}");
            module.MapEndpoints(moduleApi);
            var moduleName = module.Name;
            app.MapScalarApiReference(
                $"/scalar/{moduleName}",
                options =>
                {
                    options.Title = $"Scalar API Reference for {moduleName}";
                    options.AddDocument(moduleName);
                });
        }

        return app;
    }
}
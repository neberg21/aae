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
        foreach (var module in modules.GroupBy(m => m.GroupName))
        {
            var groupName = module.Key;
            var api = app.MapGroup($"/{ApiGroupName.Build(groupName)}");

            foreach (var m in module)
            {
                m.MapEndpoints(api);
            }

            app.MapScalarApiReference(
                $"/scalar/{groupName}",
                options =>
                {
                    options.Title = $"Scalar API Reference for {groupName}";
                    options.AddDocument(groupName);
                });
        }

        return app;
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        app.MapControllers();

        var modules = app.Services.GetRequiredService<IModuleCollection>();
        foreach (var module in modules)
        {
            module.MapEndpoints(app);
        }

        return app;
    }
}

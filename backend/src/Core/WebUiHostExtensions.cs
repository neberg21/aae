using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Core;

public static class WebUiHostExtensions
{
    public static WebApplication UseWebUi(this WebApplication app)
    {
        if (!HasWebRoot(app))
        {
            return app;
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();
        return app;
    }

    public static WebApplication MapWebUiFallback(this WebApplication app)
    {
        if (!HasWebRoot(app))
        {
            return app;
        }

        var webRootPath = app.Environment.WebRootPath;
        var indexPath = Path.Combine(webRootPath, "index.html");
        if (!File.Exists(indexPath))
        {
            return app;
        }

        app.MapFallback(async context =>
        {
            if (!IsSpaFallbackCandidate(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(indexPath);
        });

        return app;
    }

    private static bool IsSpaFallbackCandidate(PathString path)
    {
        if (path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool HasWebRoot(WebApplication app)
    {
        if (string.IsNullOrWhiteSpace(app.Environment.WebRootPath) && app.Environment.IsDevelopment())
        {
            var config = app.Configuration;
            var path = config["WebRootPath"];

            if (!string.IsNullOrWhiteSpace(path))
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var backendDir = Path.GetDirectoryName(assemblyPath);
                if (!string.IsNullOrWhiteSpace(backendDir))
                {
                    var frontendDataDir = Path.Combine(backendDir, path);
                    if (!Directory.Exists(frontendDataDir))
                        throw new InvalidOperationException($"WebRootPath '{frontendDataDir}' does not exist.");
                    app.Environment.WebRootPath = frontendDataDir;
                }
            }
        }


        var webRootPath = app.Environment.WebRootPath;
        return !string.IsNullOrWhiteSpace(webRootPath) && Directory.Exists(webRootPath);
    }
}
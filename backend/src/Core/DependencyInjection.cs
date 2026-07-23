using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        var modules = ModuleDiscovery.DiscoverLoadedModules();
        EnsureDistinctModuleNames(modules);

        foreach (var group in modules.GroupBy(m => m.GroupName))
        {
            foreach (var module in group)
            {
                module.RegisterServices(services);
            }

            services.RegisterModuleOpenApi(group.Key);
        }

        var moduleCollection = new ModuleCollection(modules);
        services.AddSingleton<IModuleCollection>(moduleCollection);

        var mvcBuilder = services
            .AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.ConfigureJsonSerialization());
        mvcBuilder.ConfigureApplicationPartManager(manager =>
        {
            foreach (var part in GetModuleParts(modules))
            {
                var alreadyAdded = manager.ApplicationParts
                    .OfType<AssemblyPart>()
                    .Any(existing => existing.Assembly == part.Assembly);

                if (!alreadyAdded)
                {
                    manager.ApplicationParts.Add(part);
                }
            }
        });

        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.ConfigureJsonSerialization());
        services.AddSignalR();

        return services;
    }

    private static void EnsureDistinctModuleNames(IReadOnlyList<IModule> modules)
    {
        var blankNames = modules
            .Where(static module => string.IsNullOrWhiteSpace(module.GroupName))
            .Select(static module => module.GetType().FullName ?? module.GetType().Name)
            .ToList();

        if (blankNames.Count == 0)
        {
            return;
        }

        var details = new List<string>();
        if (blankNames.Count > 0)
        {
            details.Add($"blank Name on: {string.Join(", ", blankNames)}");
        }

        throw new InvalidOperationException(
            $"Module names must be non-empty and unique. {string.Join("; ", details)}.");
    }

    private static void RegisterModuleOpenApi(this IServiceCollection services, string groupName)
    {
        var pathPrefix = ApiGroupName.Build(groupName);

        services.AddOpenApi(groupName, options =>
        {
            options.ShouldInclude = description =>
                description.RelativePath is not null
                && description.RelativePath.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static IEnumerable<AssemblyPart> GetModuleParts(IReadOnlyList<IModule> modules)
    {
        foreach (var assembly in modules
                     .Select(static module => module.GetType().Assembly)
                     .Distinct())
        {
            var part = new AssemblyPart(assembly);
            yield return part;
        }
    }
}
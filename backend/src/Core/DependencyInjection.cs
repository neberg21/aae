using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        var modules = ModuleDiscovery.DiscoverLoadedModules();
        EnsureDistinctModuleNames(modules);

        foreach (var module in modules)
        {
            module.RegisterServices(services);
            RegisterModuleOpenApi(services, module);
        }

        var moduleCollection = new ModuleCollection(modules);
        services.AddSingleton<IModuleCollection>(moduleCollection);

        var mvcBuilder = services.AddControllers();
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

        return services;
    }

    private static void EnsureDistinctModuleNames(IReadOnlyList<IModule> modules)
    {
        var blankNames = modules
            .Where(static module => string.IsNullOrWhiteSpace(module.Name))
            .Select(static module => module.GetType().FullName ?? module.GetType().Name)
            .ToList();

        var duplicateNames = modules
            .Where(static module => !string.IsNullOrWhiteSpace(module.Name))
            .GroupBy(static module => module.Name, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToList();

        if (blankNames.Count == 0 && duplicateNames.Count == 0)
        {
            return;
        }

        var details = new List<string>();
        if (blankNames.Count > 0)
        {
            details.Add($"blank Name on: {string.Join(", ", blankNames)}");
        }

        if (duplicateNames.Count > 0)
        {
            details.Add($"duplicates: {string.Join(", ", duplicateNames.Select(static name => $"'{name}'"))}");
        }

        throw new InvalidOperationException(
            $"Module names must be non-empty and unique. {string.Join("; ", details)}.");
    }

    private static void RegisterModuleOpenApi(IServiceCollection services, IModule module)
    {
        var name = module.Name;
        var pathPrefix = $"api/{name}";

        services.AddOpenApi(name, options =>
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

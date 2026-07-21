using System.Reflection;

namespace Core;

public static class ModuleDiscovery
{
    public static IReadOnlyList<IModule> DiscoverLoadedModules()
    {
        EnsureReferencedModuleAssembliesLoaded();

        var moduleAssemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(static assembly =>
            {
                var name = assembly.GetName().Name;
                return name is not null
                       && name.StartsWith("Module.", StringComparison.Ordinal);
            });

        return Discover(moduleAssemblies);
    }

    public static IReadOnlyList<IModule> Discover(IEnumerable<Assembly> assemblies)
    {
        var moduleTypes = assemblies
            .Distinct()
            .SelectMany(static assembly => assembly.GetTypes())
            .Where(static type =>
                typeof(IModule).IsAssignableFrom(type)
                && type is { IsInterface: false, IsAbstract: false });

        return DiscoverTypes(moduleTypes);
    }

    public static IReadOnlyList<IModule> DiscoverTypes(IEnumerable<Type> moduleTypes)
    {
        var modules = new List<IModule>();

        foreach (var moduleType in moduleTypes.Distinct())
        {
            if (!typeof(IModule).IsAssignableFrom(moduleType)
                || moduleType.IsInterface
                || moduleType.IsAbstract)
            {
                continue;
            }

            object? instance;
            try
            {
                instance = Activator.CreateInstance(moduleType);
            }
            catch (Exception exception)
            {
                var message =
                    $"Failed to activate module type '{moduleType.FullName}'. " +
                    "Modules must have a public parameterless constructor.";
                throw new InvalidOperationException(message, exception);
            }

            if (instance is not IModule module)
            {
                continue;
            }

            modules.Add(module);
        }

        return modules;
    }

    private static void EnsureReferencedModuleAssembliesLoaded()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is null)
        {
            return;
        }

        foreach (var referenced in entryAssembly.GetReferencedAssemblies())
        {
            var name = referenced.Name;
            if (name is null || !name.StartsWith("Module.", StringComparison.Ordinal))
            {
                continue;
            }

            Assembly.Load(referenced);
        }
    }
}

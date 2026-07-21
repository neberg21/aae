using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddSingleton<IModuleCollection, ModuleCollection>();

        return services;
    }

    public static IServiceCollection AddModule<TModule>(this IServiceCollection services) where TModule : class, IModule
    {
        services.AddSingleton<IModule, TModule>();

        return services;
    }
}
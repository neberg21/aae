using Bogus;
using Core;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.Nostr;
using Module.AI.Persistence;

namespace Module.AI;

public class CoreModule : IModule
{
    public string GroupName => "ai";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<AppDbContext>();

        AddNostrServices(services);
    }

    private static void AddNostrServices(IServiceCollection services)
    {
        services.AddHostedService<ListenOnMessages>();
        
        services.AddScoped<NostrEventService>();
        services.AddScoped<ProfileGenerator>();
        services.AddScoped<Faker>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
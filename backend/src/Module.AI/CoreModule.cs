using Bogus;
using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.Chat;
using Module.AI.Nostr;
using Module.AI.Persistence;

namespace Module.AI;

public class CoreModule : IModule
{
    public string GroupName => "ai";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<AppDbContext>();
        services.AddScoped<ChatHub>();

        services.AddScoped<NostrEventService>();
        services.AddScoped<ProfileGenerator>();

        services.AddScoped<Faker>(_ => new Faker("de"));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ChatHub>("/chat");
    }
}
using Bogus;
using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.Agents.AI;
using Module.Agents.Chat;
using Module.Agents.Nostr;
using Module.Agents.Persistence;

namespace Module.Agents;

public class CoreModule : IModule
{
    public string GroupName => "agents";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<AppDbContext>();
        services.AddScoped<ChatHub>();
        services.AddScoped<NostrEventService>();

        services.AddScoped<Faker>(_ => new Faker("de"));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ChatHub>("/chat");
    }
}
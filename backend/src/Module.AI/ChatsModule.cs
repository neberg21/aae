using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.Chat;

namespace Module.AI;

public class ChatsModule : IModule
{
    public string GroupName => "ai";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ChatHub>();
        services.AddScoped<LeoChatService>();
        services.AddScoped<HelgaChatService>();

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.nano-gpt.com/v1/")
        };
        services.AddOpenAIChatClient(
            "gpt-4o",
            "sk-nano-1b0ff19f-026f-4775-a946-46254d6f8ebd",
            httpClient: httpClient
        );
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ChatHub>("/chat");
    }
}
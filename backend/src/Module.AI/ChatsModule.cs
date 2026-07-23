using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.Chat;
using Module.AI.DTOs;

namespace Module.AI;

public class ChatsModule : IModule
{
    public string GroupName => "ai";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ChatHub>();
        services.AddScoped<ChatService>();
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
        endpoints.MapHub<ChatHub>("/hubs/chat");

        endpoints = endpoints.MapGroup("chats");

        endpoints.MapPost("/actions/create-vision", CreateVision)
            .Produces<CreateVisionResponse>();
        endpoints.MapPost("/actions/recruit-employee", RecruitEmployee)
            .Produces<RecruitEmployeeResponse>();
    }

    private async Task<IResult> CreateVision(
        [FromBody] CreateVisionRequest request, ChatService chatService)
    {
        var chat = await chatService.CreateVision(request);
        return Results.Ok(chat);
    }
    private async Task<IResult> RecruitEmployee(
        [FromBody] RecruitEmployeeRequest request, ChatService chatService)
    {
        var chat = await chatService.RecruitEmployee(request);
        return Results.Ok(chat);
    }
}
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.AI.Personas;
using Module.AI.Chat;
using Xunit;

namespace Service.Integration;

public class HelgaWorkflows : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly LeoChatService _leoChatService;
    private readonly HelgaChatService _helgaChatService;

    public HelgaWorkflows(WebApplicationFactory<Program> factory)
    {
        var serviceProvider = factory.Services.CreateScope().ServiceProvider;
        _leoChatService = serviceProvider.GetRequiredService<LeoChatService>();
        _helgaChatService = serviceProvider.GetRequiredService<HelgaChatService>();
    }

    [Fact]
    public async Task GetResponse_SingleScope_ReturnsSingleScope()
    {
        var chatHistory = await _leoChatService.InitiateChat(
            "Yo moin, ich hätt gerne ein neues DnD Storyteller tool. " +
            "STELLE KEINE RÜCKFRAGEN ZUR VISION! DENK DIR EINE VISION AUS WENN DU OFFENE FRAGEN HAST.");

        Assert.True(_leoChatService.TryGetResponse(chatHistory, out var response));

        foreach (var scope in response.Scopes)
        {
            var request = new Helga.Request(response.ThreadId, response.AgentId, scope.SupervisorId, scope.Message);
            var history = await _helgaChatService.Recruit(request);

            Assert.True(_helgaChatService.TryGetResponse(history, out var recruiting));
            Assert.Equal(Helga.HelgaStatus.Ready, recruiting.Status);
            
        }
    }
}
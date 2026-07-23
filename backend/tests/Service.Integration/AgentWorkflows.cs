using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.AI;
using Module.AI.AI.Personas;
using Xunit;

namespace Service.Integration;

public class AgentWorkflows : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AgentWorkflows(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateEmployeeAsync()
    {
        var agentService = _factory.Services.GetRequiredService<CoreAgentService>();
        var leo = await agentService.GetLeo();
        var leoPrompt = leo.SystemPrompt;
        var chatClient = _factory.Services.GetRequiredService<IChatClient>();

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, leoPrompt),
            new(ChatRole.User, "Yo moin, ich hätt gerne ein neues DnD Storyteller tool")
        };
        var response = await chatClient.GetResponseAsync(chatMessages);
        var responseContent = response.Messages.Last().Text;
        var json = responseContent.Replace("```json", "").Replace("```", "");
        var leoResponse = JsonSerializer.Deserialize<Leo.Response>(json);
        
        
    }
}
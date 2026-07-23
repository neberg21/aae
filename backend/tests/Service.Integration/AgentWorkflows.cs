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
    public async Task DeserializeLeoResponse()
    {
        const string example =
            """
            {
              "threadId" : "019f8e6ed90b",
              "delegations" : [ {
                "targetAgentId" : "helga",
                "intent" : "HR_REQUEST",
                "message" : "We need a supervisor for the Gaming module to handle a new DnD Storyteller tool vision.",
                "moduleScope" : "Module.Gaming"
              } ]
            }
            """;
        var options = new JsonSerializerOptions().ConfigureJsonSerialization();
        var leoResponse = JsonSerializer.Deserialize<Leo.Response>(example, options);
        Assert.NotNull(leoResponse);
    }

    [Fact]
    public async Task CreateEmployeeAsync()
    {
        var agentService = _factory.Services.GetRequiredService<CoreAgentService>();
        var leo = await agentService.GetLeo();
        var leoPrompt = leo.SystemPrompt;
        var chatClient = _factory.Services.GetRequiredService<IChatClient>();
        var threadId = Guid.CreateVersion7().ToString("N")[..12];
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, leoPrompt),
            new(ChatRole.Assistant, $"This is the thread id: {threadId}"),
            new(ChatRole.User, "Yo moin, ich hätt gerne ein neues DnD Storyteller tool")
        };
        var response = await chatClient.GetResponseAsync(chatMessages);
        var responseContent = response.Messages.Last().Text;
        var json = responseContent.Replace("```json", "").Replace("```", "");
        var options = new JsonSerializerOptions().ConfigureJsonSerialization();
        var leoResponse = JsonSerializer.Deserialize<Leo.Response>(json, options);

        Assert.NotNull(leoResponse);
    }
}
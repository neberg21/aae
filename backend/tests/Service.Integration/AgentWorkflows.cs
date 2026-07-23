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
    public void Deserialize_HrRequest_ReturnsLeoResponse()
    {
        const string example =
            """
            {
              "threadId": "019f8e7e13c9",
              "userVision": "Yo moin, ich hätt gerne ein neues DnD Storyteller tool",
              "scopes": [
                {
                  "supervisor": "supervisor-gaming",
                  "message": "The user would like to create a new DnD Storyteller tool. Please consider how to approach building a tool that supports storytelling within the Dungeons and Dragons gaming context, focusing on features that enhance user experience and creativity."
                }
              ]
            }
            """;
        var options = new JsonSerializerOptions().ConfigureJsonSerialization();
        var leoResponse = JsonSerializer.Deserialize<Leo.Response>(example, options);
        Assert.NotNull(leoResponse);
    }

    [Fact]
    public async Task CreateClearVision()
    {
        var response = await InitiateChat("Yo moin, ich hätt gerne ein neues DnD Storyteller tool");
        var responseContent = response.Messages.Last().Text;
        var json = responseContent.Replace("```json", "").Replace("```", "");
        var options = new JsonSerializerOptions().ConfigureJsonSerialization();
        var leoResponse = JsonSerializer.Deserialize<Leo.Response>(json, options);

        Assert.NotNull(leoResponse);
        Assert.Single(leoResponse.Scopes);
    }

    [Fact]
    public async Task MultipleClearVisions()
    {
        var response = await InitiateChat(
            "Yo moin, ich hätt gerne " +
            "ein neues DnD Storyteller tool und " +
            "etwas zum rasen mäßen aber auch " +
            "eine eisverkaufs-homepage");
        var responseContent = response.Messages.Last().Text;
        var json = responseContent.Replace("```json", "").Replace("```", "");
        var options = new JsonSerializerOptions().ConfigureJsonSerialization();
        var leoResponse = JsonSerializer.Deserialize<Leo.Response>(json, options);

        Assert.NotNull(leoResponse);
        Assert.InRange(leoResponse.Scopes.Count, 2, 10);
    }

    [Fact]
    public async Task NoVision()
    {
        var response = await InitiateChat("Ich will was neues...aber was?");
        var responseContent = response.Messages.Last().Text;
        
        Assert.Contains("Ich habe keine Vision", responseContent);
    }

    private async Task<ChatResponse> InitiateChat(string initialMessage)
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
            new(ChatRole.User, initialMessage)
        };
        var response = await chatClient.GetResponseAsync(chatMessages);

        return response;
    }
}
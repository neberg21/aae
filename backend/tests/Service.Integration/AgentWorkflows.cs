using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.AI;
using Module.AI.AI.Personas;
using Xunit;

namespace Service.Integration;

public class ChatHistory
{
    private readonly List<ChatMessage> _messages = [];

    public ChatHistory(IEnumerable<ChatMessage> chatMessages, ChatResponse response)
    {
        _messages.AddRange(chatMessages);
        _messages.AddRange(response.Messages);
    }

    public ChatMessage CurrentMessage => _messages.Last();

    public ChatHistory AddChatResponse(ChatResponse response)
    {
        _messages.AddRange(response.Messages);
        return this;
    }

    public IEnumerable<ChatMessage> AddMessage(ChatMessage chatMessage)
    {
        _messages.Add(chatMessage);
        return _messages;
    }
}

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
        var response = await InitiateChat(
            "Yo moin, ich hätt gerne ein neues DnD Storyteller tool. " +
            "STELLE KEINE RÜCKFRAGEN ZUR VISION! DENK DIR EINE VISION AUS WENN DU OFFENE FRAGEN HAST.");
        var leoResponse = ExtractResponse(response);

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
            "eine eisverkaufs-homepage." +
            "STELLE KEINE RÜCKFRAGEN ZUR VISION! DENK DIR EINE VISION AUS WENN DU OFFENE FRAGEN HAST.");
        var leoResponse = ExtractResponse(response);

        Assert.NotNull(leoResponse);
        Assert.InRange(leoResponse.Scopes.Count, 2, 10);
    }

    [Fact]
    public async Task NoVision()
    {
        var response = await InitiateChat("Ich will was neues...aber was?");
        response = await AnswerQuestions(response, "Ich will eine eisverkaufs-homepage.");
        response = await AnswerQuestions(response,
            "Ich will straciatella eis verkaufen und über die DHL versenden." +
            "STELLE KEINE RÜCKFRAGEN ZUR VISION! DENK DIR EINE VISION AUS WENN DU OFFENE FRAGEN HAST.");
        var leoResponse = ExtractResponse(response);

        Assert.NotNull(leoResponse);
    }

    private async Task<ChatHistory> InitiateChat(string initialMessage)
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

        return new ChatHistory(chatMessages, response);
    }

    private async Task<ChatHistory> AnswerQuestions(ChatHistory history, string answer)
    {
        var chatClient = _factory.Services.GetRequiredService<IChatClient>();
        var chatMessage = new ChatMessage(ChatRole.User, answer);
        var messages = history.AddMessage(chatMessage);
        var response = await chatClient.GetResponseAsync(messages);
        return history.AddChatResponse(response);
    }

    private static Leo.Response? ExtractResponse(ChatHistory history)
    {
        var responseContent = history.CurrentMessage.Text;
        var json = responseContent.Replace("```json", "").Replace("```", "");
        var options = new JsonSerializerOptions().ConfigureJsonSerialization();
        var leoResponse = JsonSerializer.Deserialize<Leo.Response>(json, options);
        return leoResponse;
    }
}
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.Chat;
using Xunit;

namespace Service.Integration;

public class LeoWorkflows : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly LeoChatService _leoChatService;

    public LeoWorkflows(WebApplicationFactory<Program> factory)
    {
        var serviceProvider = factory.Services.CreateScope().ServiceProvider;
        _leoChatService = serviceProvider.GetRequiredService<LeoChatService>();
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
        var leoResponse = JsonSerializer.Deserialize<Vision>(example, options);
        Assert.NotNull(leoResponse);
    }

    [Fact]
    public async Task GetResponse_SingleScope_ReturnsSingleScope()
    {
        var chatHistory = await _leoChatService.CreateVision(
            "Yo moin, ich hätt gerne ein neues DnD Storyteller tool. " +
            "STELLE KEINE RÜCKFRAGEN ZUR VISION! DENK DIR EINE VISION AUS WENN DU OFFENE FRAGEN HAST.");

        Assert.True(_leoChatService.TryGetResponse(chatHistory, out var response));
        Assert.Single(response.Scopes);
    }

    [Fact]
    public async Task GetResponse_MultipleScopes_ReturnsMultipleScopes()
    {
        var chatHistory = await _leoChatService.CreateVision(
            "Yo moin, ich hätt gerne " +
            "ein neues DnD Storyteller tool und " +
            "etwas zum rasen mäßen aber auch " +
            "eine eisverkaufs-homepage." +
            "STELLE KEINE RÜCKFRAGEN ZUR VISION! DENK DIR EINE VISION AUS WENN DU OFFENE FRAGEN HAST.");

        Assert.True(_leoChatService.TryGetResponse(chatHistory, out var response));
        Assert.InRange(response.Scopes.Count, 2, 10);
    }

    [Fact]
    public async Task GetResponse_NoVision_ConversationResultsInVision()
    {
        var chatHistory = await _leoChatService.CreateVision("Ich will was neues...aber was?");
        chatHistory = await _leoChatService.AnswerQuestions(chatHistory, "Ich will eine eisverkaufs-homepage.");
        chatHistory = await _leoChatService.AnswerQuestions(chatHistory,
            "Ich will straciatella eis verkaufen und über die DHL versenden." +
            "STELLE KEINE RÜCKFRAGEN ZUR VISION! DENK DIR EINE VISION AUS WENN DU OFFENE FRAGEN HAST.");

        Assert.True(_leoChatService.TryGetResponse(chatHistory, out _));
    }
}
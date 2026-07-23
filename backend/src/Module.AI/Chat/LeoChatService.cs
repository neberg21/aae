using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Module.AI.AI;
using Module.AI.AI.Personas;

namespace Module.AI.Chat;

public class LeoChatService
{
    private readonly CoreAgentService _coreAgentService;
    private readonly IChatClient _chatClient;

    public LeoChatService(CoreAgentService coreAgentService, IChatClient chatClient)
    {
        _coreAgentService = coreAgentService;
        _chatClient = chatClient;
    }

    public async Task<ChatHistory> InitiateChat(string initialMessage)
    {
        var leo = await _coreAgentService.GetLeo();
        var leoPrompt = leo.SystemPrompt;
        var threadId = Guid.CreateVersion7().ToString("N")[..12];
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, leoPrompt),
            new(ChatRole.Assistant, $"This is the thread id: {threadId}"),
            new(ChatRole.User, initialMessage)
        };
        var response = await _chatClient.GetResponseAsync(chatMessages);

        return new ChatHistory(chatMessages, response);
    }

    public async Task<ChatHistory> AnswerQuestions(ChatHistory history, string answer)
    {
        var chatMessage = new ChatMessage(ChatRole.User, answer);
        var messages = history.AddMessage(chatMessage);
        var response = await _chatClient.GetResponseAsync(messages);
        return history.AddChatResponse(response);
    }

    public bool TryGetResponse(ChatHistory history, [NotNullWhen(true)] out Leo.Response? response)
    {
        try
        {
            var responseContent = history.CurrentMessage.Text;
            var json = responseContent.Replace("```json", "").Replace("```", "");
            var options = new JsonSerializerOptions().ConfigureJsonSerialization();
            response = JsonSerializer.Deserialize<Leo.Response>(json, options);
            return response is not null;
        }
        catch
        {
            response = null;
            return false;
        }
    }
}
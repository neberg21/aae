using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Module.AI.AI;
using Module.AI.AI.Personas;

namespace Module.AI.Chat;

public class HelgaChatService
{
    private readonly CoreAgentService _coreAgentService;
    private readonly IChatClient _chatClient;

    public HelgaChatService(CoreAgentService coreAgentService, IChatClient chatClient)
    {
        _coreAgentService = coreAgentService;
        _chatClient = chatClient;
    }

    public async Task<ChatHistory> Recruit(Helga.Request request)
    {
        var helga = await _coreAgentService.GetHelga();
        var leoPrompt = helga.SystemPrompt;
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, leoPrompt),
            new(ChatRole.Assistant, $"This is the thread id: {request.ThreadId}"),
            new(ChatRole.Assistant, $"This is the supervisor id: {request.SupervisorId}"),
            new(ChatRole.Assistant, $"This is the agent id: {request.AgentId}"),
            new(ChatRole.User, request.Message)
        };
        var response = await _chatClient.GetResponseAsync(chatMessages);

        return new ChatHistory(chatMessages, response);
    }

    public bool TryGetResponse(ChatHistory history, [NotNullWhen(true)] out Helga.Response? response)
    {
        try
        {
            var responseContent = history.CurrentMessage.Text;
            var json = responseContent.Replace("```json", "").Replace("```", "");
            var options = new JsonSerializerOptions().ConfigureJsonSerialization();
            response = JsonSerializer.Deserialize<Helga.Response>(json, options);
            return response is not null;
        }
        catch
        {
            response = null;
            return false;
        }
    }
}
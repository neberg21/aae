using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Module.AI.AI;

namespace Module.AI.Chat;

public partial class HelgaChatService
{
    private readonly CoreAgentService _coreAgentService;
    private readonly IChatClient _chatClient;

    public HelgaChatService(CoreAgentService coreAgentService, IChatClient chatClient)
    {
        _coreAgentService = coreAgentService;
        _chatClient = chatClient;
    }

    public async Task<ChatHistory> Recruit(RecruitingRequest recruitingRequest)
    {
        var helga = await _coreAgentService.GetHelga();
        var leoPrompt = helga.SystemPrompt;
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, leoPrompt),
            new(ChatRole.Assistant, $"This is the thread id: {recruitingRequest.ThreadId}"),
            new(ChatRole.Assistant, $"This is the supervisor id: {recruitingRequest.SupervisorId}"),
            new(ChatRole.Assistant, $"This is the agent id: {recruitingRequest.AgentId}"),
            new(ChatRole.User, recruitingRequest.Message)
        };
        var response = await _chatClient.GetResponseAsync(chatMessages);
        return new ChatHistory(recruitingRequest.ThreadId, chatMessages, response);
    }

    public bool TryGetResponse(ChatHistory history, [NotNullWhen(true)] out RecruitingResponse? response)
    {
        try
        {
            var responseContent = history.CurrentMessage;
            var json = responseContent.Replace("```json", "").Replace("```", "");
            var options = new JsonSerializerOptions().ConfigureJsonSerialization();
            response = JsonSerializer.Deserialize<RecruitingResponse>(json, options);
            return response is not null;
        }
        catch
        {
            response = null;
            return false;
        }
    }
}
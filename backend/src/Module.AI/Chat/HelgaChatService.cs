using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Module.AI.AI;
using Module.AI.Persistence;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Module.AI.Chat;

public partial class HelgaChatService
{
    private readonly CoreAgentService _coreAgentService;
    private readonly IChatClient _chatClient;
    private readonly AppDbContext _dbContext;

    public HelgaChatService(CoreAgentService coreAgentService, IChatClient chatClient, AppDbContext dbContext)
    {
        _coreAgentService = coreAgentService;
        _chatClient = chatClient;
        _dbContext = dbContext;
    }

    public async Task<ChatHistory> Recruit(JobApplication jobApplication)
    {
        var helga = await _coreAgentService.GetHelga();
        var systemPrompt = helga.SystemPrompt;
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.System, $"This is the thread id: {jobApplication.ThreadId}"),
            new(ChatRole.System, $"This is the supervisor id: {jobApplication.SupervisorId}"),
            new(ChatRole.System, $"This is the agent id: {jobApplication.AgentId}"),
            new(ChatRole.User, jobApplication.Message)
        };
        var response = await _chatClient.GetResponseAsync(chatMessages);
        var chatHistory = new ChatHistory(
            jobApplication.ThreadId,
            jobApplication.SupervisorId,
            helga.Id,
            chatMessages,
            response);
        _dbContext.ChatHistories.Add(chatHistory);
        await _dbContext.SaveChangesAsync();
        return chatHistory;
    }

    public bool TryGetResponse(ChatHistory history, [NotNullWhen(true)] out Recruitment? response)
    {
        try
        {
            var responseContent = history.CurrentMessage;
            var json = responseContent.Replace("```json", "").Replace("```", "");
            var options = new JsonSerializerOptions().ConfigureJsonSerialization();
            response = JsonSerializer.Deserialize<Recruitment>(json, options);
            return response is not null;
        }
        catch
        {
            response = null;
            return false;
        }
    }
}
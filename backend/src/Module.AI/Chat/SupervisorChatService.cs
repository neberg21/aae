using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Module.AI.DTOs;
using Module.AI.Persistence;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Module.AI.Chat;

public partial class SupervisorChatService
{
    private readonly IChatClient _chatClient;
    private readonly AppDbContext _dbContext;

    public SupervisorChatService(IChatClient chatClient, AppDbContext dbContext)
    {
        _chatClient = chatClient;
        _dbContext = dbContext;
    }

    public async Task<ChatHistory> DefineEmployees(AnalyzeTask define)
    {
        var threadId = define.ThreadId;
        var supervisorId = define.SupervisorId;
        var agentId = define.AgentId;
        var systemPrompt = define.SystemPrompt;
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.System, $"This is the thread id: {threadId}"),
            new(ChatRole.System, $"This is the supervisor id: {supervisorId}"),
            new(ChatRole.System, $"This is the agent id: {agentId}"),
            new(ChatRole.User, InitialMessage)
        };
        var response = await _chatClient.GetResponseAsync(chatMessages);
        var chatHistory = new ChatHistory(
            threadId,
            supervisorId,
            agentId,
            chatMessages,
            response);
        _dbContext.ChatHistories.Add(chatHistory);
        await _dbContext.SaveChangesAsync();
        return chatHistory;
    }

    public bool TryGetResponse(ChatHistory history, [NotNullWhen(true)] out CreateAgentResponse[]? response)
    {
        try
        {
            var responseContent = history.CurrentMessage;
            var json = responseContent.Replace("```json", "").Replace("```", "");
            var options = new JsonSerializerOptions().ConfigureJsonSerialization();
            response = JsonSerializer.Deserialize<CreateAgentResponse[]>(json, options);
            return response is not null;
        }
        catch
        {
            response = null;
            return false;
        }
    }
}
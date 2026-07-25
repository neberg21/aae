using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
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
        var systemPrompt = define.SupervisorTasks;
        var supervisor = _dbContext.Agents.First(a => a.SupervisorId == supervisorId).Level++;
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.System, $"This is the thread id: {threadId}"),
            new(ChatRole.System, $"This is the supervisor id: {supervisorId}"),
            new(ChatRole.System, $"This is the agent id: {agentId}"),
            new(ChatRole.System, $"This is the hierarchy current level: {supervisor + 1}"),
            new(ChatRole.User, "Definiere deine Teammitglieder anhand deiner Aufgaben: " + systemPrompt),
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

    public bool TryGetResponse(ChatHistory history, [NotNullWhen(true)] out Employee[]? response)
    {
        response = null;
        if (!ChatResponseJsonParser.TryDeserialize<Employees>(history.CurrentMessage, out var employees))
            return false;

        response = employees.Team;
        return true;
    }
}
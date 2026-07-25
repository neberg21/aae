using System.Diagnostics.CodeAnalysis;
using System.Text;
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
        var chatMessages = new List<ChatMessage>
        {
            CreateSystemPrompt(define),
            new(ChatRole.User, "Definiere deine Teammitglieder anhand deiner Aufgaben: " + define.SupervisorTasks),
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

    private ChatMessage CreateSystemPrompt(AnalyzeTask define)
    {
        var message = new StringBuilder(SystemPrompt);
        var threadId = define.ThreadId;
        var supervisorId = define.SupervisorId;
        var agentId = define.AgentId;
        var supervisor = _dbContext.Agents.First(a => a.SupervisorId == supervisorId).Level++;

        message.AppendLine($"This is the thread id: {threadId}");
        message.AppendLine($"This is the supervisor id: {supervisorId}");
        message.AppendLine($"This is the agent id: {agentId}");
        message.AppendLine($"This is the hierarchy current level: {supervisor}");

        return new ChatMessage(ChatRole.System, message.ToString());
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
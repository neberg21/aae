using System.Diagnostics.CodeAnalysis;
using System.Text;
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
        var systemPrompt = helga.AgentTask;
        var chatMessages = new List<ChatMessage>
        {
            CreateSystemPrompt(systemPrompt, jobApplication),
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

    private ChatMessage CreateSystemPrompt(string systemPrompt, JobApplication jobApplication)
    {
        var message = new StringBuilder();
        message.AppendLine(systemPrompt);
        message.AppendLine($"This is the thread id: {jobApplication.ThreadId}");
        message.AppendLine($"This is the supervisor id: {jobApplication.SupervisorId}");
        message.AppendLine($"This is the agent id: {jobApplication.AgentId}");

        return new(ChatRole.System, message.ToString());
    }

    public bool TryGetResponse(ChatHistory history, [NotNullWhen(true)] out Recruitment? response) =>
        ChatResponseJsonParser.TryDeserialize(history.CurrentMessage, out response);
}
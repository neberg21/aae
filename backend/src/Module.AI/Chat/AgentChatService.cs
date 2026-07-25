using System.Text;
using Microsoft.Extensions.AI;
using Module.AI.DTOs;
using Module.AI.Persistence;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Module.AI.Chat;

public class AgentChatService
{
    private readonly IChatClient _chatClient;
    private readonly AppDbContext _dbContext;

    public AgentChatService(IChatClient chatClient, AppDbContext dbContext)
    {
        _chatClient = chatClient;
        _dbContext = dbContext;
    }

    public ChatHistory CreateChatHistory(CreateAgentChatRequest request)
    {
        var threadId = Guid.CreateVersion7().ToString("N")[..12];
        var chatHistory = new ChatHistory(threadId, "User", request.AgentId, []);
        var agent = _dbContext.Agents.First(a => a.AgentId == chatHistory.ChattingWith);
        chatHistory.AddMessage(CreateSystemPrompt(agent));
        _dbContext.ChatHistories.Add(chatHistory);

        return chatHistory;
    }

    private ChatMessage CreateSystemPrompt(Agent agent)
    {
        var message = new StringBuilder();
        message.AppendLine("Your name is " + agent.Name);
        message.AppendLine(
            "If someone asks you about yourself, tell them who you are and what you do. " +
            "Do not answer generically. Answer with extracted details from your system prompts. " +
            "Answer questions always in the context of the thread in your identified system prompt.");
        message.AppendLine(agent.SystemPrompt);
        return new ChatMessage(ChatRole.System, message.ToString());
    }

    public async Task<ChatHistory> AddChatMessage(ChatHistory chatHistory, string message)
    {
        var messages = chatHistory.AddMessage(new ChatMessage(ChatRole.User, message));
        var response = await _chatClient.GetResponseAsync(messages);
        chatHistory.AddChatResponse(response);
        await _dbContext.SaveChangesAsync();
        return chatHistory;
    }
}
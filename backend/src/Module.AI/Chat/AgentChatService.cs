using Microsoft.Extensions.AI;
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

    public async Task<ChatHistory> CreateChatMessage(ChatHistory chatHistory, string message)
    {
        if (!chatHistory.Messages.Any())
        {
            var agent = _dbContext.Agents.First(a => a.AgentId == chatHistory.ChattingWith);
            chatHistory.AddMessage(new ChatMessage(ChatRole.System, agent.SystemPrompt));
            _dbContext.ChatHistories.Add(chatHistory);
        }

        chatHistory.AddMessage(new ChatMessage(ChatRole.User, message));
        var response = await _chatClient.GetResponseAsync(chatHistory.Messages);
        chatHistory.AddChatResponse(response);
        await _dbContext.SaveChangesAsync();
        return chatHistory;
    }
}
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
            chatHistory.AddMessage(new ChatMessage(ChatRole.System, "Dein Name ist: " + agent.Name));
            chatHistory.AddMessage(new ChatMessage(ChatRole.System,
                "Wenn dich jemand etwas über sich fragt, sag ihm, wer du bist und was du machst. Antworte nicht genrisch. Antworte mit extrahierten Details aus deinen Systemprompts."));

            _dbContext.ChatHistories.Add(chatHistory);
        }

        chatHistory.AddMessage(new ChatMessage(ChatRole.User, message));
        var response = await _chatClient.GetResponseAsync(chatHistory.Messages);
        chatHistory.AddChatResponse(response);
        await _dbContext.SaveChangesAsync();
        return chatHistory;
    }
}
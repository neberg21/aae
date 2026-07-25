using Microsoft.Extensions.AI;

namespace Module.AI.Chat;

public class AgentChatService
{
    private readonly IChatClient _chatClient;

    public AgentChatService(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public ChatHistory CreateChatMessage(ChatHistory chatHistory, string requestMessage)
    {
        throw new NotImplementedException();
    }
}
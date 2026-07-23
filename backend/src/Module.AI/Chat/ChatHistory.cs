using Microsoft.Extensions.AI;

namespace Module.AI.Chat;

public class ChatHistory
{
    private readonly List<ChatMessage> _messages = [];

    public ChatHistory(IEnumerable<ChatMessage> chatMessages, ChatResponse response)
    {
        _messages.AddRange(chatMessages);
        _messages.AddRange(response.Messages);
    }

    public ChatMessage CurrentMessage => _messages.Last();

    public ChatHistory AddChatResponse(ChatResponse response)
    {
        _messages.AddRange(response.Messages);
        return this;
    }

    public IEnumerable<ChatMessage> AddMessage(ChatMessage chatMessage)
    {
        _messages.Add(chatMessage);
        return _messages;
    }
}
using Microsoft.Extensions.AI;

namespace Module.AI.Chat;

public class ChatHistory
{
    private readonly List<ChatMessage> _messages = [];

    public ChatHistory(string threadId, IEnumerable<ChatMessage> chatMessages, ChatResponse response)
    {
        ThreadId = threadId;
        _messages.AddRange(chatMessages);
        _messages.AddRange(response.Messages);
    }

    public string ThreadId { get; }

    public string CurrentMessage => _messages.Last().Text;

    public IEnumerable<ChatMessage> Messages =>
        _messages.Where(m => m.Role == ChatRole.User || m.Role == ChatRole.Assistant);

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
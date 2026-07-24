using Microsoft.Extensions.AI;

namespace Module.AI.Chat;

public class ChatHistory
{
    private readonly List<ChatMessage> _messages = [];

    public ChatHistory(string threadId, string sender, IEnumerable<ChatMessage> chatMessages, ChatResponse response)
    {
        ThreadId = threadId;
        Sender = sender;

        _messages.AddRange(chatMessages);
        _messages.AddRange(response.Messages);
    }

    public string ThreadId { get; }
    public string Sender { get; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public string CurrentMessage => _messages.Last().Text;

    public IEnumerable<ChatMessage> Messages =>
        _messages.Where(m => m.Role == ChatRole.User || m.Role == ChatRole.Assistant);

    public ChatHistory AddChatResponse(ChatResponse response)
    {
        _messages.AddRange(response.Messages);
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    public IEnumerable<ChatMessage> AddMessage(ChatMessage chatMessage)
    {
        _messages.Add(chatMessage);
        UpdatedAt = DateTime.UtcNow;
        return _messages;
    }
}
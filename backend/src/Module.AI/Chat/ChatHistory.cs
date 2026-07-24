using Microsoft.Extensions.AI;

namespace Module.AI.Chat;

public class ChatHistory
{
    private readonly List<ChatMessage> _messages = [];
    private readonly string _agentId;
    private readonly string _chatPartner;

    public ChatHistory(
        string threadId,
        string agentId,
        string chatPartner,
        IEnumerable<ChatMessage> chatMessages,
        ChatResponse response)
    {
        ThreadId = threadId;
        _agentId = agentId;
        _chatPartner = chatPartner;

        _messages.AddRange(chatMessages);
        _messages.AddRange(response.Messages);
    }

    public string ThreadId { get; }
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

    public string GetSender(ChatMessage message) => message.Role == ChatRole.User ? _chatPartner : _agentId;

    public string GetReceiver(ChatMessage message) => message.Role == ChatRole.User ? _agentId : _chatPartner;
}
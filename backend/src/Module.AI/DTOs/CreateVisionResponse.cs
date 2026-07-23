using Module.AI.Chat;

namespace Module.AI.DTOs;

public record CreateVisionResponse(string ThreadId, string Content)
{
    public Vision? Vision { get; init; }
    public ChatMessageDto[] ChatMessages { get; init; }
}
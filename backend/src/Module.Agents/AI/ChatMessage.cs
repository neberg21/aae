namespace Module.Agents.AI;

public record ChatMessage
{
    public required string ThreadId { get; init; }
    public required string Sender { get; init; }
    public required string? Receiver { get; init; }
    public required string Content { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
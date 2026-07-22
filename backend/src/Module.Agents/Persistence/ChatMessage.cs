namespace Module.Agents.Persistence;

public record ChatMessage
{
    public required string ThreadId { get; set; }
    public required string Sender { get; set; }
    public required string? Receiver { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
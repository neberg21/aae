namespace Module.AI.Persistence;

public record ParkedDelegation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string ThreadId { get; set; }
    public required string SenderAgentId { get; set; }
    public required string TargetAgentId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

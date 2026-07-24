namespace Module.AI.Persistence;

public record Agent
{
    public string Id => AgentId;
    public required string AgentId { get; set; }
    public required string Name { get; set; }
    public required string PublicKeyHex { get; set; }
    public required string JobTitle { get; set; }
    public required string JobDescription { get; set; }
    public required string SystemPrompt { get; set; }
    public required string PrivateKeyHex { get; set; }
    public required string Department { get; set; }
    public required string? SupervisorId { get; set; }
    public required string[] Guardrails { get; set; }
    public required AgentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
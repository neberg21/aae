namespace Module.Agents.Persistence;

public record Agent
{
    public string Id => $"{Name}:{Department}:{JobTitle}".ToLower().Replace(" ", "-");
    public required string Name { get; set; }
    public required string PublicKeyHex { get; set; }
    public required string JobTitle { get; set; }
    public required string JobDescription { get; set; }
    public required string SystemPrompt { get; set; }
    public required string PrivateKeyHex { get; set; }
    public required string Department { get; set; }
    public required string? ManagerId { get; set; }
    public required string[] Guardrails { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
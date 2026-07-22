namespace Module.Agents.Persistence;

public record Agent
{
    public required string Name { get; set; }
    public required string PublicKeyHex { get; set; }
    public required string JobTitle { get; set; }
    public required string JobDescription { get; set; }
    public required string SystemPrompt { get; set; }
    public required string PrivateKeyHex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
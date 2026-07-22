namespace Module.Agents.DTOs;

public record CreateIdentityRequest
{
    public required string JobTitle { get; init; }
    public required string JobDescription { get; init; }
    public required string SystemPrompt { get; init; }
}
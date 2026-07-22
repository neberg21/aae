namespace Module.Agents.DTOs;

public record CreateIdentityRequest
{
    public required string JobTitle { get; init; }
    public required string JobDescription { get; init; }
    public required string Department { get; set; }
    public required string? ManagerId { get; set; }
    public required string SystemPrompt { get; init; }
    public required string[] Guardrails { get; init; }
    public required string[] Tools { get; init; }
}
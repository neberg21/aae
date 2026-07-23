namespace Module.AI.DTOs;

public record CreateAgentRequest
{
    public required string ThreadId { get; init; }
    public required string AgentId { get; init; }
    public required string JobTitle { get; init; }
    public required string JobDescription { get; init; }
    public required string Department { get; set; }
    public required string? SupervisorId { get; set; }
    public required string SystemPrompt { get; init; }
    public required string[] Guardrails { get; init; }
    public required string[] Tools { get; init; }
}
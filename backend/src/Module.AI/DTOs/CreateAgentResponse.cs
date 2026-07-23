namespace Module.AI.DTOs;

public record CreateAgentResponse
{
    public required string AgentId { get; init; }
    public required string Name { get; init; }
}
namespace Module.Agents.DTOs;

public record CreateIdentityResponse
{
    public required string AgentId { get; init; }
    public required string Name { get; init; }
}
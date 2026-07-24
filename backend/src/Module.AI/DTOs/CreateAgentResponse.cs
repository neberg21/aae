namespace Module.AI.DTOs;

public record CreateAgentResponse
{
    public required string AgentId { get; init; }
    public required CreateAgentResponseStatus Status { get; init; }
}

public enum CreateAgentResponseStatus
{
    Onboarding
}
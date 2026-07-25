namespace Module.AI.DTOs;

public record CreateAgentChatRequest
{
    public string? ThreadId { get; init; }
    public required string AgentId { get; init; }
    public required string Message { get; init; }
}
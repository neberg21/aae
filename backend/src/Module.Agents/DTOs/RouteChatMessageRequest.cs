namespace Module.Agents.DTOs;

public record RouteChatMessageRequest
{
    public required string ThreadId { get; init; }
    public required string SenderAgentId { get; init; }
    public required string[]? TargetAgentIds { get; init; }
    public required string Content { get; init; }
}
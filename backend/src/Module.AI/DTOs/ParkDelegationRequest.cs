namespace Module.Agents.DTOs;

public record ParkDelegationRequest
{
    public required string ThreadId { get; init; }
    public required string SenderAgentId { get; init; }
    public required string TargetAgentId { get; init; }
    public required string Content { get; init; }
}

namespace Module.Agents.DTOs;

public record CreateIdentityResponse
{
    public required string IdentityId { get; init; }
    public required string Name { get; init; }
}
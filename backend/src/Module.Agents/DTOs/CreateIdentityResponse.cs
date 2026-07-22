namespace Module.Agents.DTOs;

public record CreateIdentityResponse
{
    public required string Name { get; init; }
    public required string PublicKeyHex { get; init; }
}
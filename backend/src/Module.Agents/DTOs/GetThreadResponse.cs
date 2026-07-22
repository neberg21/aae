namespace Module.Agents.DTOs;

public record GetThreadResponse
{
    public required string ThreadId { get; init; }
    public required ChatMessageDto[] Messages { get; init; }
}

public record ChatMessageDto
{
    public required string Sender { get; set; }
    public required string? Receiver { get; set; }
    public required string Content { get; set; }
    public required DateTime CreatedAt { get; set; }
}
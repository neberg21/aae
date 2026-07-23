using System.Text.Json;

namespace Module.AI.DTOs;

public record CreateVisionResponse(string ThreadId, string Content)
{
    public JsonElement? Object { get; init; }
}
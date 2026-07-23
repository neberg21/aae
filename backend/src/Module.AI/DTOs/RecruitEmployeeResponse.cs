using System.Text.Json;

namespace Module.AI.DTOs;

public record RecruitEmployeeResponse(string ThreadId, string Content)
{
    public JsonElement? Object { get; init; }
}
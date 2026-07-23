using Module.AI.Chat;

namespace Module.AI.DTOs;

public record RecruitEmployeeResponse(string ThreadId, string Content)
{
    public RecruitingResponse? Recruited { get; init; }
}
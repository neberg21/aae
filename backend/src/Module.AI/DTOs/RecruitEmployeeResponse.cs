using Module.AI.Chat;

namespace Module.AI.DTOs;

public record RecruitEmployeeResponse(string ThreadId, string Content)
{
    public Recruitment? Recruited { get; init; }
}
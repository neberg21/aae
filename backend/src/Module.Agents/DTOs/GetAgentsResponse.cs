using Core;

namespace Module.Agents.DTOs;

public record GetAgentsResponse : PageDto<AgentDto>;

public record AgentDto(string Id, string Name, string Department, string JobTitle);

public record AgentByIdResponse(string Id, string Name, string Department, string JobTitle, string SystemPrompt)
    : AgentDto(Id, Name, Department, JobTitle);
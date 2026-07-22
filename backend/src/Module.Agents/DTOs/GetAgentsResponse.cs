using Core;

namespace Module.Agents.DTOs;

public record GetAgentsResponse : PageDto<AgentDto>;

public record AgentDto(string IdentityId, string Name, string Department, string JobTitle);

public record GetAgentByIdResponse(string IdentityId, string Name, string Department, string JobTitle, string SystemPrompt)
    : AgentDto(IdentityId, Name, Department, JobTitle);
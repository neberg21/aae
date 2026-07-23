using Core;

namespace Module.AI.DTOs;

public record GetAgentsResponse : PageDto<AgentDto>;

public record AgentDto(string AgentId, string Name, string Department, string JobTitle);

public record GetAgentByIdResponse(string AgentId, string Name, string Department, string JobTitle, string SystemPrompt)
    : AgentDto(AgentId, Name, Department, JobTitle);
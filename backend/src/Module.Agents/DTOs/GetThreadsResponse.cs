using Core;

namespace Module.Agents.DTOs;

public record GetThreadsResponse : PageDto<ThreadDto>;

public record ThreadDto(string ThreadId, DateTime CreatedAt, DateTime UpdatedAt, int MessageCount);
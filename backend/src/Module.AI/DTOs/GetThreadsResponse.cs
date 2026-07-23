using Core;

namespace Module.AI.DTOs;

public record GetThreadsResponse : PageDto<ThreadDto>;

public record ThreadDto(string ThreadId, DateTime CreatedAt, DateTime UpdatedAt, int MessageCount);
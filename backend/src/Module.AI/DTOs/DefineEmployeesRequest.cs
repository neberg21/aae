using Module.AI.Persistence;

namespace Module.AI.DTOs;

public record DefineEmployeesRequest(string ThreadId, Agent Supervisor);
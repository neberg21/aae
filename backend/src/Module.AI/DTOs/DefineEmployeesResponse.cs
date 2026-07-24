namespace Module.AI.DTOs;

public record DefineEmployeesResponse(string ThreadId, IEnumerable<CreateAgentResponse> Employees);
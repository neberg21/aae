using Module.AI.Chat;

namespace Module.AI.DTOs;

public record DefineEmployeesResponse(string ThreadId, IEnumerable<Employee> Team);
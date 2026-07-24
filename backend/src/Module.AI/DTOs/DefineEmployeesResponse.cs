using Module.AI.Persistence;

namespace Module.AI.DTOs;

public record DefineEmployeesResponse()
{
    public IEnumerable<Agent> Employees { get; set; }
    public string ThreadId { get; set; }
}
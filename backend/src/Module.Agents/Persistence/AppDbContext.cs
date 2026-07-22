using Module.Agents.AI;
using Module.Agents.Persistence;

namespace Module.Agents;

public class AppDbContext
{
    public List<Agent> Agents { get; set; } = [];

    public List<ChatMessage> ChatMessages { get; set; } = [];

    public Task SaveChangesAsync()
    {
        // dummy method
        return Task.CompletedTask;
    }
}
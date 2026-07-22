namespace Module.Agents.AI;

public class AppDbContext
{
    public List<ChatMessage> ChatMessages { get; set; } = [];

    public Task SaveChangesAsync()
    {
        // dummy method
        return Task.CompletedTask;
    }
}
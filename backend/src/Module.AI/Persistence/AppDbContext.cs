namespace Module.AI.Persistence;

public class AppDbContext
{
    public List<Agent> Agents { get; set; } = [];

    public List<ChatMessage> ChatMessages { get; set; } = [];

    public List<ParkedDelegation> ParkedDelegations { get; set; } = [];

    public Task SaveChangesAsync()
    {
        // dummy method
        return Task.CompletedTask;
    }
}
using Module.AI.Persistence;

namespace Module.AI.AI;

public class CoreAgentService
{
    private readonly AppDbContext _dbContext;

    public CoreAgentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Agent> GetLeo() => await GetCoreAgent("leo");
    public async Task<Agent> GetHelga() => await GetCoreAgent("helga");

    private async Task<Agent> GetCoreAgent(string id)
    {
        var count = 0;
        var agent = _dbContext.Agents.FirstOrDefault(a => a.Id == id);

        while (agent is null)
        {
            if (count++ > 10)
            {
                throw new NotSupportedException(
                    $"{id} agent not found, initial seed failed. See {nameof(SeedCoreAgents)}");
            }

            await Task.Delay(1000);
            agent = _dbContext.Agents.FirstOrDefault(a => a.Id == id);
        }

        return agent;
    }
}
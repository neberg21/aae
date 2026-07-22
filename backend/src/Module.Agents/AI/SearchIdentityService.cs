using Module.Agents.Persistence;

namespace Module.Agents.AI;

public class SearchIdentityService
{
    private readonly AppDbContext _dbContext;

    public SearchIdentityService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyCollection<Agent> SearchIdentities(
        string? agentId, string? name, string? department, string? jobTitle)
    {
        var query = _dbContext.Agents.AsEnumerable();

        if (!string.IsNullOrEmpty(agentId))
            query = query.Where(a => a.Id.Equals(agentId, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(name))
            query = query.Where(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(department))
            query = query.Where(a => a.Department.Equals(department, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(jobTitle))
            query = query.Where(a => a.JobTitle.Equals(jobTitle, StringComparison.OrdinalIgnoreCase));

        return query.ToArray();
    }
}
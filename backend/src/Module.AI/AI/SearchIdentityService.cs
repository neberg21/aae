using Module.Agents.DTOs;
using Module.Agents.Persistence;

namespace Module.Agents.AI;

public class SearchIdentityService
{
    private readonly AppDbContext _dbContext;

    public SearchIdentityService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public GetAgentsResponse SearchIdentities(
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

        var items = query.ToArray();
        var page = GetAgentsResponse(items);

        return page;
    }

    public GetAgentsResponse GetAgents()
    {
        var items = _dbContext.Agents.ToArray();
        var page = GetAgentsResponse(items);

        return page;
    }

    private static GetAgentsResponse GetAgentsResponse(IReadOnlyCollection<Agent> agents)
    {
        var page = new GetAgentsResponse
        {
            Items = agents.Select(a => new AgentDto(a.Id, a.Name, a.Department, a.JobTitle)).ToArray(),
            TotalCount = agents.Count,
            PageSize = agents.Count,
            PageNumber = 1,
            TotalPages = (int)Math.Ceiling((double)agents.Count / agents.Count)
        };
        return page;
    }
}
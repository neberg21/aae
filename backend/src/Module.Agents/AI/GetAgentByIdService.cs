using Module.Agents.DTOs;
using Module.Agents.Persistence;

namespace Module.Agents.AI;

public class GetAgentByIdService
{
    private readonly AppDbContext _dbContext;

    public GetAgentByIdService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public GetAgentByIdResponse? GetById(string agentId)
    {
        var agent = _dbContext.Agents.FirstOrDefault(a => a.Id == agentId);

        if (agent is null)
            return null;

        return new GetAgentByIdResponse(
            agent.Id,
            agent.Name,
            agent.Department,
            agent.JobTitle,
            agent.SystemPrompt);
    }
}
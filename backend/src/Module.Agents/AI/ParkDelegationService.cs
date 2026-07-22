using Module.Agents.DTOs;
using Module.Agents.Persistence;

namespace Module.Agents.AI;

public class ParkDelegationService
{
    private readonly AppDbContext _dbContext;

    public ParkDelegationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParkDelegationResponse> Park(ParkDelegationRequest request)
    {
        var parked = new ParkedDelegation
        {
            ThreadId = request.ThreadId,
            SenderAgentId = request.SenderAgentId,
            TargetAgentId = request.TargetAgentId,
            Content = request.Content
        };
        _dbContext.ParkedDelegations.Add(parked);
        await _dbContext.SaveChangesAsync();

        var response = new ParkDelegationResponse
        {
            Ok = true
        };
        return response;
    }

    public IReadOnlyList<ParkedDelegation> DequeueByTargetAgentId(string agentId)
    {
        var matches = _dbContext.ParkedDelegations
            .Where(p => p.TargetAgentId.Equals(agentId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.CreatedAt)
            .ToList();

        foreach (var match in matches)
        {
            _dbContext.ParkedDelegations.Remove(match);
        }

        return matches;
    }
}

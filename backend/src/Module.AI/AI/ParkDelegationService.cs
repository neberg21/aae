using Microsoft.Extensions.Logging;
using Module.AI.DTOs;
using Module.AI.Persistence;

namespace Module.AI.AI;

public class ParkDelegationService
{
    private readonly ILogger<ParkDelegationService> _logger;
    private readonly AppDbContext _dbContext;

    public ParkDelegationService(ILogger<ParkDelegationService> logger, AppDbContext dbContext)
    {
        _logger = logger;
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
        _logger.LogInformation(
            "Delegation parked: {ThreadId}, {SenderAgentId}, {TargetAgentId}",
            request.ThreadId,
            request.SenderAgentId,
            request.TargetAgentId);

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
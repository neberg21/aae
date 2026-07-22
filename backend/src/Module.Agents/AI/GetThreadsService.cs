using Module.Agents.DTOs;
using Module.Agents.Persistence;

namespace Module.Agents.AI;

public class GetThreadsService
{
    private readonly AppDbContext _dbContext;

    public GetThreadsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public GetThreadsResponse GetThreads()
    {
        var threads = _dbContext.ChatMessages
            .GroupBy(c => c.ThreadId)
            .Select(g => new ThreadDto(g.Key, g.First().CreatedAt, g.Last().CreatedAt, g.Count()))
            .ToArray();
        var page = new GetThreadsResponse
        {
            Items = threads,
            TotalCount = threads.Length,
            PageSize = threads.Length,
            PageNumber = 1,
            TotalPages = (int)Math.Ceiling((double)threads.Length / threads.Length)
        };

        return page;
    }
}
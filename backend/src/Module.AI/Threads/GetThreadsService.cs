using Module.AI.DTOs;
using Module.AI.Persistence;

namespace Module.AI.Threads;

public class GetThreadsService
{
    private readonly AppDbContext _dbContext;

    public GetThreadsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public GetThreadsResponse GetThreads()
    {
        var threads = _dbContext.ChatHistories
            .GroupBy(c => c.ThreadId)
            .Select(g =>
                new ThreadDto(g.Key, g.First().CreatedAt, g.Last().CreatedAt, g.SelectMany(h => h.Messages).Count()))
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
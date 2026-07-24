using Module.AI.Chat;
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
            .Select(CreateThread)
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

    private static ThreadDto CreateThread(IGrouping<string, ChatHistory> thread)
    {
        var first = thread.First();
        var latest = thread.Last();
        var messageCount = thread.SelectMany(h => h.Messages).Count();
        return new ThreadDto(thread.Key, first.CreatedAt, latest.CreatedAt, messageCount);
    }
}
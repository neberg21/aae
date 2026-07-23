using Module.AI.DTOs;
using Module.AI.Persistence;

namespace Module.AI.Threads;

public class GetThreadByIdService
{
    private readonly AppDbContext _dbContext;

    public GetThreadByIdService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public GetThreadResponse? GetById(string threadId)
    {
        var chatMessages = _dbContext.ChatMessages
            .Where(m => m.ThreadId == threadId)
            .Select(m => new ChatMessageDto
            {
                Sender = _dbContext.Agents.FirstOrDefault(a => a.Id == m.Sender)?.Name ?? m.Sender,
                Receiver = _dbContext.Agents.FirstOrDefault(a => a.Id == m.Receiver)?.Name ?? m.Receiver,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToArray();
        var parking = _dbContext.ParkedDelegations
            .Where(p => p.ThreadId == threadId)
            .Select(p => new ChatMessageDto
            {
                Sender = _dbContext.Agents.FirstOrDefault(a => a.Id == p.SenderAgentId)?.Name ?? p.SenderAgentId,
                Receiver = _dbContext.Agents.FirstOrDefault(a => a.Id == p.TargetAgentId)?.Name ?? p.TargetAgentId,
                Content = p.Content,
                CreatedAt = p.CreatedAt
            })
            .ToArray();

        var allMessages = chatMessages.Concat(parking).OrderBy(m => m.CreatedAt).ToArray();

        if (allMessages.Length == 0)
        {
            return null;
        }

        var result = new GetThreadResponse
        {
            ThreadId = threadId,
            Messages = allMessages
        };

        return result;
    }
}
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
        var histories = _dbContext.ChatHistories
            .Where(m => m.ThreadId == threadId);
        var allMessages = new List<ChatMessageDto>();
        foreach (var history in histories)
        {
            foreach (var message in history.Messages)
            {
                var sender = history.GetSender(message);
                var receiver = history.GetReceiver(message);

                sender = _dbContext.Agents.FirstOrDefault(a => a.Id == sender)?.Name ?? sender;
                receiver = _dbContext.Agents.FirstOrDefault(a => a.Id == receiver)?.Name ?? receiver;

                var dto = new ChatMessageDto
                {
                    Sender = sender,
                    Receiver = receiver,
                    Content = message.Text,
                    CreatedAt = message.CreatedAt?.DateTime ?? DateTime.UtcNow
                };

                allMessages.Add(dto);
            }
        }

        if (allMessages.Count == 0)
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
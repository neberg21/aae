using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Module.AI.AI;
using Module.AI.Persistence;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Module.AI.Chat;

public partial class LeoChatService
{
    private readonly CoreAgentService _coreAgentService;
    private readonly IChatClient _chatClient;
    private readonly AppDbContext _dbContext;

    public LeoChatService(CoreAgentService coreAgentService, IChatClient chatClient, AppDbContext dbContext)
    {
        _coreAgentService = coreAgentService;
        _chatClient = chatClient;
        _dbContext = dbContext;
    }

    public async Task<ChatHistory> CreateVision(string initialMessage)
    {
        var leo = await _coreAgentService.GetLeo();
        var leoPrompt = leo.SystemPrompt;
        var threadId = Guid.CreateVersion7().ToString("N")[..12];
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, leoPrompt),
            new(ChatRole.System, $"This is the thread id: {threadId}"),
            new(ChatRole.User, initialMessage)
        };
        var response = await _chatClient.GetResponseAsync(chatMessages);
        var hostory = new ChatHistory(threadId, chatMessages, response);
        _dbContext.ChatHistories.Add(hostory);
        await _dbContext.SaveChangesAsync();
        return hostory;
    }

    public async Task<ChatHistory> AnswerQuestions(ChatHistory history, string answer)
    {
        var chatMessage = new ChatMessage(ChatRole.User, answer);
        var messages = history.AddMessage(chatMessage);
        var response = await _chatClient.GetResponseAsync(messages);
        return history.AddChatResponse(response);
    }

    public bool TryGetResponse(ChatHistory history, [NotNullWhen(true)] out Vision? response)
    {
        try
        {
            var responseContent = history.CurrentMessage;
            var json = responseContent.Replace("```json", "").Replace("```", "");
            var options = new JsonSerializerOptions().ConfigureJsonSerialization();
            response = JsonSerializer.Deserialize<Vision>(json, options);
            return response is not null;
        }
        catch
        {
            response = null;
            return false;
        }
    }
}
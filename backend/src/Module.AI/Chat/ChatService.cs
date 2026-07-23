using System.Text.Json;
using Microsoft.Extensions.AI;
using Module.AI.DTOs;
using Module.AI.Persistence;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Module.AI.Chat;

public class ChatService
{
    private readonly AppDbContext _dbContext;
    private readonly LeoChatService _leoChatService;
    private readonly HelgaChatService _helgaChatService;

    public ChatService(AppDbContext dbContext, LeoChatService leoChatService, HelgaChatService helgaChatService)
    {
        _dbContext = dbContext;
        _leoChatService = leoChatService;
        _helgaChatService = helgaChatService;
    }

    public async Task<CreateVisionResponse> CreateVision(CreateVisionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ThreadId))
        {
            var vision = await _leoChatService.CreateVision(request.Content);
            return CreateResponse(vision);
        }

        var chatHistory = GetChatHistory(request.ThreadId);
        var answer = await _leoChatService.AnswerQuestions(chatHistory, request.Content);
        return CreateResponse(answer);

        CreateVisionResponse CreateResponse(ChatHistory vision)
        {
            if (!_leoChatService.TryGetResponse(vision, out var response))
                return new CreateVisionResponse(vision.ThreadId, vision.CurrentMessage);

            var finalMessage = new ChatMessage(ChatRole.Assistant, $"Created vision: {response.UserVision}");
            vision.AddMessage(finalMessage);
            return new CreateVisionResponse(vision.ThreadId, vision.CurrentMessage)
            {
                Object = response.ToJsonElement()
            };
        }
    }

    public async Task<RecruitEmployeeResponse> RecruitEmployee(RecruitEmployeeRequest request)
    {
        var chatHistory = GetChatHistory(request.ThreadId);
        var recruitingRequest = new RecruitingRequest(
            chatHistory.ThreadId,
            request.SupervisorId,
            request.AgentId,
            request.Content);
        chatHistory = await _helgaChatService.Recruit(recruitingRequest);

        if (!_helgaChatService.TryGetResponse(chatHistory, out var response))
            return new RecruitEmployeeResponse(chatHistory.ThreadId, chatHistory.CurrentMessage);

        var finalMessage = new ChatMessage(ChatRole.Assistant, $"Created agent: {response.Agent.AgentId}");
        chatHistory.AddMessage(finalMessage);
        return new RecruitEmployeeResponse(response.ThreadId, chatHistory.CurrentMessage)
        {
            Object = response.ToJsonElement()
        };
    }

    private ChatHistory GetChatHistory(string threadId)
    {
        var chatHistory = _dbContext.ChatHistories.FirstOrDefault(c => c.ThreadId == threadId);

        return chatHistory ?? throw new NotSupportedException($"Thread not found: {threadId}");
    }
}
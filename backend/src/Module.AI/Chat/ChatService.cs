using Microsoft.Extensions.AI;
using Module.AI.Chat.Jobs;
using Module.AI.DTOs;
using Module.AI.Persistence;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Module.AI.Chat;

public class ChatService
{
    private readonly AppDbContext _dbContext;
    private readonly LeoChatService _leoChatService;
    private readonly HelgaChatService _helgaChatService;
    private readonly SupervisorChatService _supervisorChatService;
    private readonly ExecuteVisionChannel _visionChannel;
    private readonly ExecuteRecruitmentChannel _recruitmentChannel;

    public ChatService(
        AppDbContext dbContext,
        LeoChatService leoChatService,
        HelgaChatService helgaChatService,
        ExecuteVisionChannel visionChannel,
        ExecuteRecruitmentChannel recruitmentChannel, 
        SupervisorChatService supervisorChatService)
    {
        _dbContext = dbContext;
        _leoChatService = leoChatService;
        _helgaChatService = helgaChatService;
        _visionChannel = visionChannel;
        _recruitmentChannel = recruitmentChannel;
        _supervisorChatService = supervisorChatService;
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
            {
                return new CreateVisionResponse(vision.ThreadId, vision.CurrentMessage)
                {
                    ChatMessages = vision.Messages.Select(m => new ChatMessageDto
                    {
                        Content = m.Text,
                        Sender = m.Role.ToString(),
                        Receiver = m.Role == ChatRole.Assistant ? "User" : "Assistant",
                        CreatedAt = m.CreatedAt.GetValueOrDefault().DateTime
                    }).ToArray()
                };
            }

            var finalMessage = new ChatMessage(ChatRole.Assistant, $"Created vision: {response.UserVision}");
            vision.AddMessage(finalMessage);
            _visionChannel.TryWrite(response);
            return new CreateVisionResponse(vision.ThreadId, vision.CurrentMessage)
            {
                Vision = response,
                ChatMessages = vision.Messages.Select(m => new ChatMessageDto
                {
                    Content = m.Text,
                    Sender = m.Role.ToString(),
                    Receiver = m.Role == ChatRole.Assistant ? "User" : "Assistant",
                    CreatedAt = m.CreatedAt.GetValueOrDefault().DateTime
                }).ToArray()
            };
        }
    }

    public async Task<RecruitEmployeeResponse> RecruitEmployee(RecruitEmployeeRequest request)
    {
        var chatHistory = GetChatHistory(request.ThreadId);
        var recruitingRequest = new JobApplication(
            chatHistory.ThreadId,
            request.SupervisorId,
            request.AgentId,
            request.Content);
        chatHistory = await _helgaChatService.Recruit(recruitingRequest);

        if (!_helgaChatService.TryGetResponse(chatHistory, out var response))
            return new RecruitEmployeeResponse(chatHistory.ThreadId, chatHistory.CurrentMessage);

        var finalMessage = new ChatMessage(ChatRole.Assistant, $"Created agent: {response.AgentToRecruit.AgentId}");
        chatHistory.AddMessage(finalMessage);
        _recruitmentChannel.TryWrite(response);
        return new RecruitEmployeeResponse(response.ThreadId, chatHistory.CurrentMessage)
        {
            Recruited = response
        };
    }

    private ChatHistory GetChatHistory(string threadId)
    {
        var chatHistory = _dbContext.ChatHistories.FirstOrDefault(c => c.ThreadId == threadId);
        return chatHistory ?? throw new NotSupportedException($"Thread not found: {threadId}");
    }
}
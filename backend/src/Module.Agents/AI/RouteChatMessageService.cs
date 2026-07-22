using Module.Agents.DTOs;
using Microsoft.AspNetCore.SignalR;
using System.Net.Http.Json;
using Module.Agents.Persistence;

namespace Module.Agents.AI;

public class RouteChatMessageService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IHubContext<ChatHub> _signalRHub;

    public RouteChatMessageService(AppDbContext db, HttpClient httpClient, IHubContext<ChatHub> signalRHub)
    {
        _db = db;
        _httpClient = httpClient;
        _signalRHub = signalRHub;
    }

    public async Task<RouteChatMessageResponse> RouteChatMessage(RouteChatMessageRequest request)
    {
        await HandleTarget(request);

        var res = new RouteChatMessageResponse
        {
            ThreadId = request.ThreadId
        };

        return res;
    }

    private async Task HandleTarget(RouteChatMessageRequest request)
    {
        var message = new ChatMessage
        {
            ThreadId = request.ThreadId,
            Sender = request.SenderAgentId,
            Receiver = request.TargetAgentId,
            Content = request.Content
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        await _signalRHub.Clients.Group(request.ThreadId)
            .SendAsync("ReceiveMessage", message);

        if (string.IsNullOrWhiteSpace(request.TargetAgentId) || request.TargetAgentId == "User")
        {
            return;
        }

        await ExecuteAgentWebhook(request.TargetAgentId, request.ThreadId, request.Content);
    }

    private async Task ExecuteAgentWebhook(string agentId, string threadId, string content)
    {
        var history = _db.ChatMessages
            .Where(m => m.ThreadId == threadId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new { role = m.Sender == "User" ? "user" : "assistant", content = m.Content })
            .ToList();

        var webhookUrl = agentId.Split('-')[0].ToLower().Trim() switch
        {
            "leo" => "https://n8n.neberg.de/webhook/leo-think",
            "helga" => "https://n8n.neberg.de/webhook/helga-think",
            "supervisor" => "https://n8n.neberg.de/webhook/supervisor-think",
            "specialist" => "https://n8n.neberg.de/webhook/specialist-think",
            _ => throw new ArgumentException($"Unbekannter Agent: {agentId}")
        };

        var payload = new
        {
            threadId,
            chatHistory = history,
            History = history,
            delegationRequest = content,
            taskContext = content
        };
        await _httpClient.PostAsJsonAsync(webhookUrl, payload);
    }
}

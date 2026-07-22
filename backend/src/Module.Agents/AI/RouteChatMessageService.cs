using Module.Agents.DTOs;
using Microsoft.AspNetCore.SignalR;
using System.Net.Http.Json;
using Module.Agents.Persistence;

namespace Module.Agents.AI;

public class RouteChatMessageService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IHubContext<ChatHub> _signalRHub; // Für Echtzeit-Updates ans React Frontend

    public RouteChatMessageService(AppDbContext db, HttpClient httpClient, IHubContext<ChatHub> signalRHub)
    {
        _db = db;
        _httpClient = httpClient;
        _signalRHub = signalRHub;
    }

    public async Task<RouteChatMessageResponse> RouteChatMessage(RouteChatMessageRequest request)
    {
        var targets = GetTargets(request);

        foreach (var target in targets)
        {
            await HandleTarget(request, target);
        }

        var res = new RouteChatMessageResponse
        {
            ThreadId = request.ThreadId
        };

        return res;
    }

    private async Task HandleTarget(RouteChatMessageRequest request, string? target)
    {
        // 1. Nachricht in der DB speichern (Das "Gedächtnis")
        var message = new ChatMessage
        {
            ThreadId = request.ThreadId,
            Sender = request.SenderAgentId,
            Receiver = target,
            Content = request.Content
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        // 2. Echtzeit-Update an dein React-Frontend (falls du mitlesen willst)
        await _signalRHub.Clients.Group(request.ThreadId)
            .SendAsync("ReceiveMessage", message);

        // 3. Routing: Wen müssen wir aufwecken?
        if (string.IsNullOrWhiteSpace(target) || target == "User")
        {
            // Agent hat eine Rückfrage an DICH. Wir machen nichts weiter. 
            // Der Prozess pausiert hier. n8n ist beendet.
            // Sobald du im Frontend antwortest, geht es weiter.
            return;
        }

        // Wenn der CEO an Helga delegiert, wecken wir Helga in n8n auf
        await ExecuteAgentWebhook(target, request.ThreadId);
    }

    private static List<string?> GetTargets(RouteChatMessageRequest request)
    {
        var targets = new List<string?>();

        if (request.TargetAgentIds != null)
        {
            targets.AddRange(request.TargetAgentIds);
        }
        else targets.Add(null);

        return targets;
    }

    private async Task ExecuteAgentWebhook(string agentId, string threadId)
    {
        // Hole die bisherige Chat-Historie aus der DB, damit der Agent Kontext hat
        var history = _db.ChatMessages
            .Where(m => m.ThreadId == threadId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new { role = m.Sender == "User" ? "user" : "assistant", content = m.Content })
            .ToList();

        // Jeder Agent hat seinen eigenen n8n Webhook-Eingang
        var webhookUrl = agentId.ToLower().Trim() switch
        {
            "leo" => "https://n8n.neberg.de/webhook/leo-input",
            "helga" => "https://n8n.neberg.de/webhook/helga-input",
            _ => throw new ArgumentException($"Unbekannter Agent: {agentId}")
        };

        // Fire & Forget Call an n8n. Wir warten NICHT auf die Antwort des Agenten!
        // Der Agent wird seine Antwort später wieder an unseren Controller schicken.
        var payload = new { ThreadId = threadId, History = history };
        await _httpClient.PostAsJsonAsync(webhookUrl, payload);
    }
}
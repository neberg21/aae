namespace Module.AI.Chat;

public record AnalyzeTask(string ThreadId, string SupervisorId, string AgentId, string SystemPrompt);

public partial class SupervisorChatService
{
    private const string InitialMessage = "";
}
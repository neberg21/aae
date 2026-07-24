using Module.AI.DTOs;

namespace Module.AI.Chat;

public record AnalyzeTask(string ThreadId, string SupervisorId, string SystemPrompt);

public class SupervisorChatService
{
    public Task<IReadOnlyCollection<CreateAgentResponse>> DefineEmployees(AnalyzeTask define)
    {
        throw new NotImplementedException();
    }
}
namespace Module.AI.Chat;

public record AnalyzeTask(string ThreadId, string SupervisorId, string AgentId, string SystemPrompt);

public record Employees(string ThreadId, Employee[] Team);

public record Employee(string AgentId, string Message);

public partial class SupervisorChatService
{
    private const string InitialMessage =
        """
        Create the JSON based on your provided task.

        ## Runtime inputs
        
        The workflow injects: `chatHistory`, `threadId`, `supervisorId`, `agentId`.
        
        ## Duties
        1. You analyze the task and identify the scope of the task
        2. You create a team of employees to work on your provided task
        3. For each employee, you create a message that explains what they are expected to do.
        
        ## Hard rules
        
        - The team should not be larger than 6 employees.
        - If a task feels too big for a single employee, create a supervisor for that task.
        - Use `supervisor-*` and `specialist-*` agent ids.
        - Infer sensible defaults from module scope + role when details are missing but still sufficient to create.
        - Reply with JSON only. No markdown fences. No prose outside JSON.
        
        ## Output schema
        
        ```json
        {
            "threadId": "...",
            "team": [{
                "agentId": "supervisor-*|specialist-*",
                "message": "..."
            }]
        }
        ```
        """;
}
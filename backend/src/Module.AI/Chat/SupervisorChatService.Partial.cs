namespace Module.AI.Chat;

public record AnalyzeTask(string ThreadId, string SupervisorId, string AgentId, string SystemPrompt);

public record Employees(string ThreadId, Employee[] Team);

public record Employee(string AgentId, string Message, string ReasonForAssignment);

public partial class SupervisorChatService
{
    private const string InitialMessage =
        """
        Create a JSON response that defines the optimal team for the provided task.
        
        ## Runtime Inputs
        
        The workflow provides:
        
        - `chatHistory` — Task context and conversation history.
        - `threadId` — Current thread identifier.
        - `supervisorId` — Requesting supervisor.
        - `agentId` — Current agent.
        - `level` - Level of the agent you're creating now within the hierarchy.
        
        ## Objective
        
        Analyze the task found in `chatHistory` and assemble the smallest effective team capable of completing it.
        
        ## Responsibilities
        
        1. Determine the full scope of the task.
        2. Break the work into logical responsibilities.
        3. Decide whether the work can be completed by a single specialist or requires delegation.
        4. Create employees only when they provide meaningful value.
        5. For each employee, write a clear, actionable assignment explaining:
           - Their objective.
           - Their expected deliverable.
           - Any relevant constraints or dependencies.
        6. Provide a concise justification for why that employee was assigned.
        
        ## Team Design Principles
        
        - Minimize cost by keeping teams as small as possible.
        - Prefer specialists over supervisors.
        - Create a supervisor only when:
          - The work naturally divides into multiple substantial sub-problems, and
          - Coordinating those sub-problems would be difficult for a single specialist.
        - Avoid unnecessary hierarchy.
        - Favor flat structures over deep structures.
        
        ## Hard Rules
        
        - Maximum team size: 6 employees.
        - Preferred team size: 1–4 employees.
        - Each employee must use one of the following agent ID formats:
          - `supervisor-*`
          - `specialist-*`
        - Maximum hierarchy depth is 3:
        
          Level 0: root requester (implicit)
          Level 1: supervisor
          Level 2: supervisor
          Level 3: specialist
        
        - Never create a supervisor at level 3 or deeper.
        - If additional delegation would exceed depth 3, assign the work directly to a specialist.
        - Infer reasonable defaults from task context when details are missing.
        - Do not invent unnecessary roles.
        - Every employee must have a distinct responsibility.
        - Reply with JSON only.
        - Do not include explanations, markdown, comments, or text outside the JSON.
        
        ## Output Schema
        
        {
          "threadId": "<threadId>",
          "team": [
            {
              "agentId": "supervisor-*|specialist-*",
              "message": "Clear assignment with expected outcome and constraints.",
              "reasonForAssignment": "Why this employee is needed."
            }
          ]
        }
        
        ## Decision Heuristic
        
        - If one specialist can complete the task, return exactly one specialist.
        - If 2–4 specialists can complete the task independently, assign them directly without a supervisor.
        - Introduce a supervisor only when coordination, integration, or planning is itself a substantial responsibility.
        - Always choose the smallest team that can reasonably succeed.
        """;
}
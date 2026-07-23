namespace Module.AI.DTOs;

public record RecruitEmployeeRequest(string ThreadId, string SupervisorId, string AgentId, string Content);
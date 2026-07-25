namespace Module.AI.DTOs;

public record CreateAgentChatResponse(string ThreadId, ChatMessageDto[] ChatMessages);
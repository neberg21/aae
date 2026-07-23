using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.AI;
using Xunit;

namespace Service.Integration;

public class AgentWorkflows : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AgentWorkflows(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateEmployeeAsync()
    {
        var agentService = _factory.Services.GetRequiredService<CoreAgentService>();
        var leo = await agentService.GetLeo();
        var leoPrompt = leo.SystemPrompt;
        const string url = "https://api.nano-gpt.com/api/v1";
        const string testApiKey = "sk-nano-1b0ff19f-026f-4775-a946-46254d6f8ebd";
        
    }
}
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.AI;
using Module.AI.DTOs;
using Xunit;

namespace Service.Unit.AI;

public class CreateAgentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IServiceProvider _serviceProvider;

    public CreateAgentTests(WebApplicationFactory<Program> factory)
    {
        _serviceProvider = factory.Services.CreateScope().ServiceProvider;
    }

    [Fact]
    public async Task PostAsync_CreateAgent_CreatesNewAgent()
    {
        var agentService = _serviceProvider.GetRequiredService<CreateAgentService>();
        var createAgentRequest = new CreateAgentRequest
        {
            ThreadId = "thread-1",
            AgentId = "specialist-test-engineer",
            JobTitle = "Software Engineer",
            JobDescription = "I am a software engineer",
            SystemPrompt = "You are a helpful assistant.",
            Department = "Development",
            SupervisorId = null,
            Guardrails = [],
            Tools = []
        };
        var agent = await agentService.CreateAgent(createAgentRequest);

        Assert.NotNull(agent);
        Assert.Equal(CreateAgentResponseStatus.Onboarding, agent.Status);
        Assert.Equal("specialist-test-engineer", agent.AgentId);
    }
}
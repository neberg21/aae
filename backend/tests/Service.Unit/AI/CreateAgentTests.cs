using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Module.AI.DTOs;
using Xunit;

namespace Service.Unit.AI;

public class CreateAgentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreateAgentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostAsync_CreateAgent_CreatesNewAgent()
    {
        var client = _factory.CreateClient();
        var createAgentRequest = new CreateAgentRequest
        {
            ThreadId = "thread-1",
            AgentId = "specialist-test-engineer",
            JobTitle = "Software Engineer",
            JobDescription = "I am a software engineer",
            SystemPrompt = "You are a helpful assistant.",
            Department = "Development",
            ManagerId = null,
            Guardrails = [],
            Tools = []
        };
        var response = await client.PostAsJsonAsync("/ai-api/agents", createAgentRequest);
        var agent = await response.Content.ReadFromJsonAsync<CreateAgentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(agent);
        Assert.NotEmpty(agent.Name);
        Assert.Equal("specialist-test-engineer", agent.AgentId);
    }
}
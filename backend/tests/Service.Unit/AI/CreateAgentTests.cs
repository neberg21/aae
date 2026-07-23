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

    [Fact]
    public async Task GetAsync_OpenApiDemoDocument_ReturnsOkWithDemoPath()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/ai.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/ai-api/agents", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAsync_Scalar_ReferencesOpenApiDemoDocument()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/scalar/ai");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Scalar strips the leading slash and resolves from the app root in JS.
        Assert.Contains("\"url\":\"openapi/ai.json\"", body, StringComparison.Ordinal);
    }
}
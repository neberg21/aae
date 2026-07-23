using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Module.AI.DTOs;
using Xunit;

namespace Service.Unit.AI;

public class SearchAgentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SearchAgentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAsync_SearchByAgentId_FindsCreatedSupervisor()
    {
        var client = _factory.CreateClient();
        var createRequest = new CreateAgentRequest
        {
            ThreadId = "thread-search-1",
            AgentId = "supervisor-qa",
            JobTitle = "Supervisor QA",
            JobDescription = "QA lead",
            SystemPrompt = "You supervise QA.",
            Department = "QA",
            SupervisorId = "leo",
            Guardrails = [],
            Tools = []
        };
        var createResponse = await client.PostAsJsonAsync("/ai-api/agents", createRequest);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var searchResponse = await client.GetAsync("/ai-api/agents/search?agentId=supervisor-qa");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var page = await searchResponse.Content.ReadFromJsonAsync<GetAgentsResponse>();
        Assert.NotNull(page);
        Assert.Contains(page.Items, i => i.AgentId == "supervisor-qa");
    }
}
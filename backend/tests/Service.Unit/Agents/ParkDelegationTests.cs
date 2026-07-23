using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Module.Agents.DTOs;
using Xunit;

namespace Service.Unit.Agents;

public class ParkDelegationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ParkDelegationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostAsync_ParkDelegation_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var request = new ParkDelegationRequest
        {
            ThreadId = "thread-park-1",
            SenderAgentId = "leo",
            TargetAgentId = "supervisor-finance",
            Content = "Own Finance module"
        };

        var response = await client.PostAsJsonAsync("/api/agents/park-delegation", request);
        var body = await response.Content.ReadFromJsonAsync<ParkDelegationResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Ok);
    }

    [Fact]
    public async Task PostAsync_CreateIdentity_ResumesParkedDelegation()
    {
        var client = _factory.CreateClient();
        var parkRequest = new ParkDelegationRequest
        {
            ThreadId = "thread-resume-1",
            SenderAgentId = "leo",
            TargetAgentId = "supervisor-finance",
            Content = "Own Finance module"
        };
        var parkResponse = await client.PostAsJsonAsync("/api/agents/park-delegation", parkRequest);
        Assert.Equal(HttpStatusCode.OK, parkResponse.StatusCode);

        var createRequest = new CreateAgentRequest
        {
            ThreadId = "thread-resume-1",
            AgentId = "supervisor-finance",
            JobTitle = "Supervisor Finance",
            JobDescription = "Hire finance supervisor",
            SystemPrompt = "You supervise Finance.",
            Department = "Operations",
            ManagerId = "leo",
            Guardrails = [],
            Tools = []
        };
        var createResponse = await client.PostAsJsonAsync("/api/agents/create-identity", createRequest);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var identity = await createResponse.Content.ReadFromJsonAsync<CreateAgentResponse>();
        Assert.NotNull(identity);
        Assert.Equal("supervisor-finance", identity.AgentId);

        var conflictResponse = await client.PostAsJsonAsync("/api/agents/create-identity", createRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
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
            ManagerId = "leo",
            Guardrails = [],
            Tools = []
        };
        var createResponse = await client.PostAsJsonAsync("/api/agents/create-identity", createRequest);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var searchResponse = await client.GetAsync("/api/agents/search?agentId=supervisor-qa");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var page = await searchResponse.Content.ReadFromJsonAsync<GetAgentsResponse>();
        Assert.NotNull(page);
        Assert.Contains(page.Items, i => i.AgentId == "supervisor-qa");
    }
}

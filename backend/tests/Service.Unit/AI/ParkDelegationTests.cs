using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Module.AI.DTOs;
using Xunit;

namespace Service.Unit.AI;

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

        var response = await client.PostAsJsonAsync("/ai-api/agents/actions/park-delegation", request);
        var body = await response.Content.ReadFromJsonAsync<ParkDelegationResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Ok);
    }

    [Fact]
    public async Task PostAsync_CreateAgent_ResumesParkedDelegation()
    {
        var client = _factory.CreateClient();
        var parkRequest = new ParkDelegationRequest
        {
            ThreadId = "thread-resume-1",
            SenderAgentId = "leo",
            TargetAgentId = "supervisor-finance",
            Content = "Own Finance module"
        };
        var parkResponse = await client.PostAsJsonAsync("/ai-api/agents/actions/park-delegation", parkRequest);
        Assert.Equal(HttpStatusCode.OK, parkResponse.StatusCode);

        var createRequest = new CreateAgentRequest
        {
            ThreadId = "thread-resume-1",
            AgentId = "supervisor-finance",
            JobTitle = "Supervisor Finance",
            JobDescription = "Hire finance supervisor",
            SystemPrompt = "You supervise Finance.",
            Department = "Operations",
            SupervisorId = "leo",
            Guardrails = [],
            Tools = []
        };
        var createResponse = await client.PostAsJsonAsync("/ai-api/agents", createRequest);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var identity = await createResponse.Content.ReadFromJsonAsync<CreateAgentResponse>();
        Assert.NotNull(identity);
        Assert.Equal("supervisor-finance", identity.AgentId);

        var conflictResponse = await client.PostAsJsonAsync("/ai-api/agents", createRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }
}
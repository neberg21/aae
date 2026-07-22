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
}

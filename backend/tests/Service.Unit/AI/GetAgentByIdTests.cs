using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Module.AI.DTOs;
using Xunit;

namespace Service.Unit.AI;

public class GetAgentByIdTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetAgentByIdTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    
    [Theory]
    [InlineData("leo")]
    [InlineData("helga")]
    public async Task GetCoreAgent(string agentId)
    {
        var httpClient = _factory.CreateClient();
        var response = await httpClient.GetAsync($"ai-api/agents/{agentId}");
        var agent = await response.Content.ReadFromJsonAsync<GetAgentByIdResponse>();

        Assert.NotNull(agent);
    }
}
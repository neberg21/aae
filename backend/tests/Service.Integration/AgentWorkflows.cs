using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Module.Agents.DTOs;
using Xunit;

namespace Service.Integration;

public class AgentWorkflows : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AgentWorkflows(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("leo")]
    [InlineData("helga")]
    public async Task GetCoreAgent(string agentId)
    {
        var httpClient = _factory.CreateClient();
        var response = await httpClient.GetAsync($"api/agents/{agentId}");
        var agent = await response.Content.ReadFromJsonAsync<GetAgentByIdResponse>();

        Assert.NotNull(agent);
    }
}
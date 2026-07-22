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

    public async Task CreateEmployeeAsync()
    {
        var agent = await GetLeo();
        var leoPrompt = agent.SystemPrompt;
        const string url = "https://api.nano-gpt.com/api/v1";
        const string testApiKey = "sk-nano-1b0ff19f-026f-4775-a946-46254d6f8ebd";

    }

    private async Task<GetAgentByIdResponse> GetLeo()
    {
        var httpClient = _factory.CreateClient();
        var response = await httpClient.GetAsync($"api/agents/leo");
        var agent = await response.Content.ReadFromJsonAsync<GetAgentByIdResponse>();

        Assert.NotNull(agent);
        return agent;
    }
}
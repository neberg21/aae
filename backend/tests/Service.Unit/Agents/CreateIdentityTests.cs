using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Service.Unit.Agents;

public class CreateIdentityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreateIdentityTests (WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostAsync_CreateIdentity_CreatesNewIdentity()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/agents/create-identity", null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", body);
    }

    [Fact]
    public async Task GetAsync_OpenApiDemoDocument_ReturnsOkWithDemoPath()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/agents.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/agents/create-identity", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAsync_ScalarDemo_ReferencesOpenApiDemoDocument()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/scalar/agents");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Scalar strips the leading slash and resolves from the app root in JS.
        Assert.Contains("\"url\":\"openapi/agents.json\"", body, StringComparison.Ordinal);
    }
}
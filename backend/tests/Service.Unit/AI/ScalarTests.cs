using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Service.Unit.AI;

public class ScalarTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ScalarTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/ai-api/agents")]
    [InlineData("/ai-api/agents/search")]
    [InlineData("/ai-api/agents/actions/await-request-approval")]
    [InlineData("/ai-api/agents/actions/resolve-request-approval")]
    [InlineData("/ai-api/threads")]
    public async Task GetAsync_OpenApiDemoDocument_ReturnsOkWithDemoPath(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/ai.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(path, body, StringComparison.OrdinalIgnoreCase);
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
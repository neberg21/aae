using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Service.Unit;

public sealed class DemoPingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DemoPingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAsync_ApiDemoPingPath_ReturnsPong()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/demo/ping");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", body);
    }

    [Fact]
    public async Task GetAsync_OpenApiDemoDocument_ReturnsOkWithDemoPath()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/demo.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/demo/ping", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAsync_ScalarDemo_ReferencesOpenApiDemoDocument()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/scalar/demo");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/openapi/demo.json", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/demo/openapi.json", body, StringComparison.OrdinalIgnoreCase);
    }
}

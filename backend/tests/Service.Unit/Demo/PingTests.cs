using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Service.Unit.Demo;

public sealed class PingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAsync_Ping_ReturnsPong()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/demo-api/ping");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", body);
    }
}
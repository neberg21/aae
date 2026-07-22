using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace Service.Unit.WebUi;

public sealed class WebUiHostTests : IClassFixture<WebUiWebApplicationFactory>
{
    private readonly WebUiWebApplicationFactory _factory;

    public WebUiHostTests(WebUiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAsync_Root_ReturnsIndexHtml()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AAE UI", body, StringComparison.Ordinal);
        AssertMediaTypeIsHtml(response.Content.Headers.ContentType);
    }

    [Fact]
    public async Task GetAsync_UnknownUiPath_ReturnsIndexHtml()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/team/agents");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AAE UI", body, StringComparison.Ordinal);
        AssertMediaTypeIsHtml(response.Content.Headers.ContentType);
    }

    [Fact]
    public async Task GetAsync_DemoPing_StillReturnsPongWhenWebRootPresent()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/demo/ping");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", body);
    }

    [Fact]
    public async Task GetAsync_UnknownApiPath_ReturnsNotFoundNotHtml()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/does-not-exist");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("AAE UI", body, StringComparison.Ordinal);
    }

    private static void AssertMediaTypeIsHtml(MediaTypeHeaderValue? contentType)
    {
        Assert.NotNull(contentType);
        Assert.Equal("text/html", contentType.MediaType, ignoreCase: true);
    }
}

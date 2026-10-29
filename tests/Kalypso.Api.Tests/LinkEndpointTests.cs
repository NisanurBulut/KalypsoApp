using System.Net;
using System.Net.Http.Json;
using Kalypso.Contracts;

namespace Kalypso.Api.Tests;

public sealed class LinkEndpointTests : IClassFixture<KalypsoFactory>
{
    private readonly HttpClient _client;

    public LinkEndpointTests(KalypsoFactory factory) =>
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });

    private Task<HttpResponseMessage> Create(string url, string? customCode = null) =>
        _client.PostAsJsonAsync("/api/links", new CreateLinkRequest(url, customCode));

    [Fact]
    public async Task Create_ReturnsGeneratedCodeAndShortUrl()
    {
        var response = await Create("https://example.com/some/very/long/path");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var link = await response.Content.ReadFromJsonAsync<LinkResponse>();
        Assert.NotNull(link);
        Assert.Equal(CodeGenerator.Length, link.Code.Length);
        Assert.Equal($"https://kly.test/{link.Code}", link.ShortUrl);
        Assert.Equal("https://example.com/some/very/long/path", link.OriginalUrl);
        Assert.Equal(0, link.Clicks);
    }

    [Fact]
    public async Task Create_UsesCustomCode()
    {
        var response = await Create("https://example.com", "my-sea_1");

        var link = await response.Content.ReadFromJsonAsync<LinkResponse>();
        Assert.Equal("my-sea_1", link!.Code);
        Assert.Equal("https://kly.test/my-sea_1", link.ShortUrl);
    }

    [Fact]
    public async Task Create_RejectsTakenCustomCode()
    {
        await Create("https://example.com", "taken-name");

        var second = await Create("https://example.org", "taken-name");

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/file")]
    [InlineData("javascript:alert(1)")]
    [InlineData("")]
    public async Task Create_RejectsBadUrls(string url)
    {
        var response = await Create(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("url", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("has space")]
    [InlineData("api")]
    [InlineData("Health")]
    public async Task Create_RejectsBadCustomCodes(string code)
    {
        var response = await Create("https://example.com", code);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("customCode", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ShortAddress_RedirectsAndCountsClicks()
    {
        var created = await (await Create("https://example.com/target", "go-there")).Content.ReadFromJsonAsync<LinkResponse>();

        var first = await _client.GetAsync($"/{created!.Code}");
        await _client.GetAsync($"/{created.Code}");

        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Equal("https://example.com/target", first.Headers.Location!.ToString());

        var stats = await _client.GetFromJsonAsync<LinkResponse>($"/api/links/{created.Code}");
        Assert.Equal(2, stats!.Clicks);
    }

    [Fact]
    public async Task UnknownCode_ReturnsNotFound()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/nothing-here")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/links/nothing-here")).StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_IsServed()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/links", await response.Content.ReadAsStringAsync());
    }
}

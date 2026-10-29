using System.Net;
using System.Net.Http.Json;
using Kalypso.Client.Services;
using Kalypso.Contracts;

namespace Kalypso.Client.Tests;

public sealed class JsonHistoryStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"kalypso-history-{Guid.NewGuid():N}");
    private string FilePath => Path.Combine(_folder, "history.json");

    private static LinkResponse Link(string code) =>
        new(code, $"https://kly.test/{code}", "https://example.com", DateTimeOffset.UnixEpoch, 1);

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task EmptyWhenThereIsNoFile()
    {
        Assert.Empty(await new JsonHistoryStore(FilePath).LoadAsync());
    }

    [Fact]
    public async Task AddedLinksSurviveANewStoreInstanceAndComeNewestFirst()
    {
        var store = new JsonHistoryStore(FilePath);
        await store.AddAsync(Link("aaaa"));
        await store.AddAsync(Link("bbbb"));
        await store.AddAsync(Link("aaaa")); // moves to the top instead of duplicating

        var loaded = await new JsonHistoryStore(FilePath).LoadAsync();

        Assert.Equal(["aaaa", "bbbb"], loaded.Select(l => l.Code));
    }

    [Fact]
    public async Task RemoveAndClearWork()
    {
        var store = new JsonHistoryStore(FilePath);
        await store.AddAsync(Link("aaaa"));
        await store.AddAsync(Link("bbbb"));

        await store.RemoveAsync("aaaa");
        Assert.Equal(["bbbb"], (await store.LoadAsync()).Select(l => l.Code));

        await store.ClearAsync();
        Assert.Empty(await store.LoadAsync());
    }

    [Fact]
    public async Task KeepsOnlyTheNewestItems()
    {
        var store = new JsonHistoryStore(FilePath);
        await store.ReplaceAllAsync(Enumerable.Range(0, JsonHistoryStore.MaxItems + 10).Select(i => Link($"code{i:D4}")));

        Assert.Equal(JsonHistoryStore.MaxItems, (await store.LoadAsync()).Count);
    }

    [Fact]
    public async Task ADamagedFileIsTreatedAsEmpty()
    {
        Directory.CreateDirectory(_folder);
        await File.WriteAllTextAsync(FilePath, "{ this is not json");

        Assert.Empty(await new JsonHistoryStore(FilePath).LoadAsync());
    }
}

public class KalypsoApiClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(respond(request));
        }
    }

    private sealed class ThrowingHandler(Exception error) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw error;
    }

    private static KalypsoApiClient Client(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new FakeSettings { ApiBaseUrl = "http://api.test:5299/" });

    [Fact]
    public async Task Create_PostsToTheConfiguredAddressAndReadsTheLink()
    {
        var link = new LinkResponse("Abc1234", "http://api.test:5299/Abc1234", "https://example.com", DateTimeOffset.UnixEpoch, 0);
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created) { Content = JsonContent.Create(link) });

        var result = await Client(handler).CreateAsync(new CreateLinkRequest("https://example.com"));

        Assert.Equal(link, result);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("http://api.test:5299/api/links", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task Create_ShowsTheFirstValidationMessage()
    {
        const string body = """{"title":"One or more validation errors occurred.","errors":{"customCode":["Use only letters."]}}""";
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) });

        var error = await Assert.ThrowsAsync<ApiException>(() => Client(handler).CreateAsync(new CreateLinkRequest("https://example.com", "x")));

        Assert.Equal("Use only letters.", error.Message);
    }

    [Fact]
    public async Task Create_ShowsTheTitleOfAProblemDocument()
    {
        const string body = """{"title":"That name is already taken."}""";
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict) { Content = new StringContent(body) });

        var error = await Assert.ThrowsAsync<ApiException>(() => Client(handler).CreateAsync(new CreateLinkRequest("https://example.com", "taken")));

        Assert.Equal("That name is already taken.", error.Message);
    }

    [Fact]
    public async Task Create_HasFriendlyMessagesForRateLimitAndOutages()
    {
        var limited = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var tooMany = await Assert.ThrowsAsync<ApiException>(() => Client(limited).CreateAsync(new CreateLinkRequest("https://example.com")));
        Assert.Contains("Too many requests", tooMany.Message);

        var down = await Assert.ThrowsAsync<ApiException>(
            () => Client(new ThrowingHandler(new HttpRequestException())).CreateAsync(new CreateLinkRequest("https://example.com")));
        Assert.Contains("Cannot reach", down.Message);
    }

    [Fact]
    public async Task Get_ReturnsNullForUnknownCodes()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        Assert.Null(await Client(handler).GetAsync("nope"));
    }

    [Fact]
    public async Task Ping_IsTrueOnlyWhenTheHealthCheckPasses()
    {
        Assert.True(await Client(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))).PingAsync());
        Assert.False(await Client(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))).PingAsync());
        Assert.False(await Client(new ThrowingHandler(new HttpRequestException())).PingAsync());
    }
}

public class QrCodeServiceTests
{
    [Fact]
    public void CreatesAPngImage()
    {
        var png = new QrCodeService().CreatePng("https://kly.test/Abc1234");

        Assert.True(png.Length > 100);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], png[..4]); // PNG signature
    }
}

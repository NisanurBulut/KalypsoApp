using Kalypso.Client.Services;
using Kalypso.Contracts;

namespace Kalypso.Client.Tests;

internal sealed class FakeApi : IKalypsoApi
{
    public List<CreateLinkRequest> Created { get; } = [];
    public Dictionary<string, LinkResponse> Known { get; } = [];
    public ApiException? Failure { get; set; }
    public bool Reachable { get; set; } = true;

    public Task<LinkResponse> CreateAsync(CreateLinkRequest request, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        Created.Add(request);
        var code = request.CustomCode ?? "Abc1234";
        return Task.FromResult(new LinkResponse(code, $"https://kly.test/{code}", request.Url, DateTimeOffset.UnixEpoch, 0));
    }

    public Task<LinkResponse?> GetAsync(string code, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(Known.GetValueOrDefault(code));
    }

    public Task<bool> PingAsync(CancellationToken cancellationToken = default) => Task.FromResult(Reachable);
}

internal sealed class FakeHistory : IHistoryStore
{
    public List<LinkResponse> Items { get; } = [];

    public Task<IReadOnlyList<LinkResponse>> LoadAsync() => Task.FromResult<IReadOnlyList<LinkResponse>>(Items.ToList());

    public Task AddAsync(LinkResponse link)
    {
        Items.RemoveAll(i => i.Code == link.Code);
        Items.Insert(0, link);
        return Task.CompletedTask;
    }

    public Task ReplaceAllAsync(IEnumerable<LinkResponse> links)
    {
        var copy = links.ToList();
        Items.Clear();
        Items.AddRange(copy);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string code)
    {
        Items.RemoveAll(i => i.Code == code);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Items.Clear();
        return Task.CompletedTask;
    }
}

internal sealed class FakePlatform : IPlatformActions
{
    public List<string> Copied { get; } = [];
    public List<string> Shared { get; } = [];
    public List<string> Opened { get; } = [];

    public Task CopyAsync(string text) { Copied.Add(text); return Task.CompletedTask; }
    public Task ShareAsync(string text) { Shared.Add(text); return Task.CompletedTask; }
    public Task OpenAsync(string url) { Opened.Add(url); return Task.CompletedTask; }
}

internal sealed class FakeQr : IQrCodeService
{
    public byte[] CreatePng(string text) => [1, 2, 3];
}

internal sealed class FakeSettings : ISettingsStore
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5299";
    public string DefaultApiBaseUrl => "http://localhost:5299";
}

using Kalypso.Client.Services;
using Kalypso.Client.ViewModels;
using Kalypso.Contracts;

namespace Kalypso.Client.Tests;

public class ShortenViewModelTests
{
    private readonly FakeApi _api = new();
    private readonly FakeHistory _history = new();
    private readonly FakePlatform _platform = new();

    private ShortenViewModel Create() => new(_api, _history, _platform, new FakeQr());

    [Fact]
    public async Task Shorten_AddsSchemeStoresHistoryAndShowsResult()
    {
        var vm = Create();
        vm.Url = "example.com/page";

        await vm.ShortenCommand.ExecuteAsync(null);

        Assert.Equal("https://example.com/page", _api.Created.Single().Url);
        Assert.True(vm.HasResult);
        Assert.False(vm.IsFormVisible);
        Assert.Equal([1, 2, 3], vm.QrPng);
        Assert.Single(_history.Items);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Shorten_SendsCustomCodeOnlyWhenSwitchedOn()
    {
        var vm = Create();
        vm.Url = "https://example.com";
        vm.CustomCode = "my-sea";

        await vm.ShortenCommand.ExecuteAsync(null);
        Assert.Null(_api.Created[0].CustomCode);

        vm.NewLinkCommand.Execute(null);
        vm.Url = "https://example.com";
        vm.UseCustomCode = true;
        vm.CustomCode = "my-sea";
        await vm.ShortenCommand.ExecuteAsync(null);
        Assert.Equal("my-sea", _api.Created[1].CustomCode);
    }

    [Theory]
    [InlineData("", "", "Please enter a link.")]
    [InlineData("https://example.com", "abc", "The name must have 4 to 20 characters.")]
    public async Task Shorten_ShowsValidationErrorsWithoutCallingTheApi(string url, string code, string expected)
    {
        var vm = Create();
        vm.Url = url;
        vm.UseCustomCode = true;
        vm.CustomCode = code;

        await vm.ShortenCommand.ExecuteAsync(null);

        Assert.Equal(expected, vm.ErrorMessage);
        Assert.True(vm.HasError);
        Assert.Empty(_api.Created);
    }

    [Fact]
    public async Task Shorten_ShowsApiErrors()
    {
        _api.Failure = new ApiException("That name is already taken.");
        var vm = Create();
        vm.Url = "https://example.com";

        await vm.ShortenCommand.ExecuteAsync(null);

        Assert.Equal("That name is already taken.", vm.ErrorMessage);
        Assert.False(vm.HasResult);
        Assert.False(vm.IsBusy);
        Assert.Empty(_history.Items);
    }

    [Fact]
    public async Task ResultActions_UseThePlatform()
    {
        var vm = Create();
        vm.Url = "https://example.com";
        await vm.ShortenCommand.ExecuteAsync(null);

        await vm.CopyCommand.ExecuteAsync(null);
        await vm.ShareCommand.ExecuteAsync(null);
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.Equal(["https://kly.test/Abc1234"], _platform.Copied);
        Assert.Equal(["https://kly.test/Abc1234"], _platform.Shared);
        Assert.Equal(["https://kly.test/Abc1234"], _platform.Opened);
        Assert.Equal("Copied to clipboard", vm.StatusMessage);
    }

    [Fact]
    public async Task NewLink_ResetsTheForm()
    {
        var vm = Create();
        vm.Url = "https://example.com";
        await vm.ShortenCommand.ExecuteAsync(null);

        vm.NewLinkCommand.Execute(null);

        Assert.True(vm.IsFormVisible);
        Assert.Equal("", vm.Url);
        Assert.Null(vm.QrPng);
    }

    [Theory]
    [InlineData("Look at this https://example.com/a?b=1 it is great", "https://example.com/a?b=1")]
    [InlineData("  example.org  ", "example.org")]
    public void ReceiveSharedText_FindsTheWebAddress(string shared, string expected)
    {
        var vm = Create();

        vm.ReceiveSharedText(shared);

        Assert.Equal(expected, vm.Url);
    }
}

public class HistoryViewModelTests
{
    private readonly FakeApi _api = new();
    private readonly FakeHistory _history = new();
    private readonly FakePlatform _platform = new();

    private static LinkResponse Link(string code, long clicks = 0) =>
        new(code, $"https://kly.test/{code}", "https://example.com", DateTimeOffset.UnixEpoch, clicks);

    private HistoryViewModel Create() => new(_api, _history, _platform);

    [Fact]
    public async Task Load_ShowsStoredLinksAndTotals()
    {
        _history.Items.AddRange([Link("aaaa", 2), Link("bbbb", 5)]);
        var vm = Create();
        Assert.True(vm.IsEmpty);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.False(vm.IsEmpty);
        Assert.Equal(2, vm.LinkCount);
        Assert.Equal(7, vm.TotalClicks);
        Assert.Equal("2 links", vm.LinkCountText);
        Assert.Equal("7 clicks", vm.TotalClicksText);
    }

    [Fact]
    public async Task Totals_UseTheSingularForOne()
    {
        _history.Items.Add(Link("aaaa", 1));
        var vm = Create();

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("1 link", vm.LinkCountText);
        Assert.Equal("1 click", vm.TotalClicksText);
    }

    [Fact]
    public async Task Refresh_UpdatesClickCountsFromTheApi()
    {
        _history.Items.AddRange([Link("aaaa"), Link("bbbb")]);
        _api.Known["aaaa"] = Link("aaaa", 9);
        var vm = Create();

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(9, vm.Items.Single(i => i.Code == "aaaa").Clicks);
        Assert.Equal(0, vm.Items.Single(i => i.Code == "bbbb").Clicks);
        Assert.Equal(9, _history.Items.Single(i => i.Code == "aaaa").Clicks);
        Assert.False(vm.IsRefreshing);
    }

    [Fact]
    public async Task Refresh_KeepsLinksAndShowsTheErrorWhenTheApiIsDown()
    {
        _history.Items.Add(Link("aaaa", 3));
        _api.Failure = new ApiException("Cannot reach the Kalypso API.");
        var vm = Create();

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Single(vm.Items);
        Assert.Equal("Cannot reach the Kalypso API.", vm.Message);
        Assert.True(vm.HasMessage);
    }

    [Fact]
    public async Task DeleteAndClear_RemoveFromStoreAndList()
    {
        _history.Items.AddRange([Link("aaaa"), Link("bbbb")]);
        var vm = Create();
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.DeleteCommand.ExecuteAsync(vm.Items[0]);
        Assert.Equal(["bbbb"], vm.Items.Select(i => i.Code));
        Assert.Equal(["bbbb"], _history.Items.Select(i => i.Code));

        await vm.ClearCommand.ExecuteAsync(null);
        Assert.True(vm.IsEmpty);
        Assert.Empty(_history.Items);
    }

    [Fact]
    public async Task Copy_UsesThePlatformAndSaysSo()
    {
        var vm = Create();

        await vm.CopyCommand.ExecuteAsync(Link("aaaa"));

        Assert.Equal(["https://kly.test/aaaa"], _platform.Copied);
        Assert.Equal("Copied to clipboard", vm.Message);
    }
}

public class SettingsViewModelTests
{
    private readonly FakeSettings _settings = new();
    private readonly FakeApi _api = new();

    [Fact]
    public void Save_StoresAValidAddressWithoutTrailingSlash()
    {
        var vm = new SettingsViewModel(_settings, _api) { ApiBaseUrl = " http://192.168.1.20:5299/ " };

        vm.SaveCommand.Execute(null);

        Assert.Equal("http://192.168.1.20:5299", _settings.ApiBaseUrl);
        Assert.True(vm.IsStatusOk);
    }

    [Fact]
    public void Save_RejectsAnInvalidAddress()
    {
        var vm = new SettingsViewModel(_settings, _api) { ApiBaseUrl = "not an address" };

        vm.SaveCommand.Execute(null);

        Assert.Equal("http://localhost:5299", _settings.ApiBaseUrl);
        Assert.True(vm.IsStatusError);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Test_ReportsWhetherTheApiAnswers(bool reachable, bool expectedOk)
    {
        _api.Reachable = reachable;
        var vm = new SettingsViewModel(_settings, _api);

        await vm.TestCommand.ExecuteAsync(null);

        Assert.Equal(expectedOk, vm.IsStatusOk);
        Assert.Equal(!expectedOk, vm.IsStatusError);
        Assert.False(vm.IsTesting);
    }

    [Fact]
    public void ResetToDefault_RestoresTheDefaultAddress()
    {
        _settings.ApiBaseUrl = "http://other:1";
        var vm = new SettingsViewModel(_settings, _api) { ApiBaseUrl = "http://other:1" };

        vm.ResetToDefaultCommand.Execute(null);

        Assert.Equal("http://localhost:5299", vm.ApiBaseUrl);
        Assert.Equal("http://localhost:5299", _settings.ApiBaseUrl);
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kalypso.Client.Services;
using Kalypso.Contracts;

namespace Kalypso.Client.ViewModels;

/// <summary>The list of links made on this device, with their click counts.</summary>
public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly IKalypsoApi _api;
    private readonly IHistoryStore _history;
    private readonly IPlatformActions _platform;

    public HistoryViewModel(IKalypsoApi api, IHistoryStore history, IPlatformActions platform)
    {
        _api = api;
        _history = history;
        _platform = platform;
        Items.CollectionChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    public ObservableCollection<LinkResponse> Items { get; } = [];

    public bool IsEmpty => Items.Count == 0;
    public int LinkCount => Items.Count;
    public long TotalClicks => Items.Sum(i => i.Clicks);

    public string LinkCountText => Items.Count == 1 ? "1 link" : $"{Items.Count} links";
    public string TotalClicksText => TotalClicks == 1 ? "1 click" : $"{TotalClicks} clicks";

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    /// <summary>A short note for the user: an error, or "Copied to clipboard".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    public partial string Message { get; set; } = "";

    public bool HasMessage => Message.Length > 0;

    [RelayCommand]
    private async Task LoadAsync() => SetItems(await _history.LoadAsync());

    /// <summary>Asks the API for the current click count of every link (pull to refresh).</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        Message = "";
        try
        {
            var links = (await _history.LoadAsync()).ToList();
            try
            {
                for (var i = 0; i < links.Count; i++)
                {
                    if (await _api.GetAsync(links[i].Code) is { } fresh)
                        links[i] = fresh;
                }
            }
            catch (ApiException ex)
            {
                Message = ex.Message; // keep what we have; the numbers may be a little old
            }

            await _history.ReplaceAllAsync(links);
            SetItems(links);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(LinkResponse link)
    {
        await _history.RemoveAsync(link.Code);
        Items.Remove(link);
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        await _history.ClearAsync();
        Items.Clear();
    }

    [RelayCommand]
    private async Task CopyAsync(LinkResponse link)
    {
        await _platform.CopyAsync(link.ShortUrl);
        Message = "Copied to clipboard";
    }

    [RelayCommand]
    private Task ShareAsync(LinkResponse link) => _platform.ShareAsync(link.ShortUrl);

    [RelayCommand]
    private Task OpenAsync(LinkResponse link) => _platform.OpenAsync(link.ShortUrl);

    private void SetItems(IEnumerable<LinkResponse> links)
    {
        var list = links.ToList();
        Items.Clear();
        foreach (var link in list)
            Items.Add(link);
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kalypso.Client.Services;

namespace Kalypso.Client.ViewModels;

/// <summary>Where is the Kalypso API? The address differs between an emulator, a real phone and a deployed server.</summary>
public sealed partial class SettingsViewModel(ISettingsStore settings, IKalypsoApi api) : ObservableObject
{
    [ObservableProperty]
    public partial string ApiBaseUrl { get; set; } = settings.ApiBaseUrl;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool IsStatusOk { get; set; }

    [ObservableProperty]
    public partial bool IsStatusError { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestCommand))]
    public partial bool IsTesting { get; set; }

    [RelayCommand]
    private void Save()
    {
        if (TrySave())
            SetStatus("Saved. New links will use this address.", ok: true);
    }

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task TestAsync()
    {
        if (!TrySave()) return;

        IsTesting = true;
        SetStatus("Calling the API...", ok: true);
        try
        {
            var reachable = await api.PingAsync();
            SetStatus(reachable ? "Connected! The Kalypso API is ready." : "Could not reach the Kalypso API at this address.", reachable);
        }
        finally
        {
            IsTesting = false;
        }
    }

    private bool CanTest() => !IsTesting;

    [RelayCommand]
    private void ResetToDefault()
    {
        ApiBaseUrl = settings.DefaultApiBaseUrl;
        Save();
    }

    private bool TrySave()
    {
        var text = ApiBaseUrl.Trim();
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            SetStatus("Enter a full address such as http://192.168.1.20:5299", ok: false);
            return false;
        }

        ApiBaseUrl = text.TrimEnd('/');
        settings.ApiBaseUrl = ApiBaseUrl;
        return true;
    }

    private void SetStatus(string message, bool ok)
    {
        StatusMessage = message;
        IsStatusOk = ok;
        IsStatusError = !ok;
    }
}

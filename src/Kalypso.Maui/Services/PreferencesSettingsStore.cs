using Kalypso.Client.Services;

namespace Kalypso.Maui.Services;

/// <summary>Keeps the API address in the app's preferences (SharedPreferences on Android, local settings on Windows).</summary>
public sealed class PreferencesSettingsStore : ISettingsStore
{
    private const string ApiBaseUrlKey = "api_base_url";

    public string ApiBaseUrl
    {
        get => Preferences.Default.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
        set => Preferences.Default.Set(ApiBaseUrlKey, value);
    }

    /// <summary>The Android emulator reaches the PC through 10.0.2.2; on Windows the API is simply on localhost.</summary>
    public string DefaultApiBaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5299" : "http://localhost:5299";
}

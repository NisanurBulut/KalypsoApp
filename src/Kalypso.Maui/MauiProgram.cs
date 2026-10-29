using Kalypso.Client.Services;
using Kalypso.Client.ViewModels;
using Kalypso.Maui.Pages;
using Kalypso.Maui.Services;

namespace Kalypso.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        var services = builder.Services;

        // Services: the portable ones come from Kalypso.Client, the ones that need the OS live in this project.
        services.AddSingleton<ISettingsStore, PreferencesSettingsStore>();
        services.AddSingleton<IPlatformActions, MauiPlatformActions>();
        services.AddSingleton<IQrCodeService, QrCodeService>();
        services.AddSingleton<IHistoryStore>(_ => new JsonHistoryStore(Path.Combine(FileSystem.AppDataDirectory, "history.json")));
        services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(15) });
        services.AddSingleton<IKalypsoApi, KalypsoApiClient>();

        // View models (shared logic) and pages (UI only).
        services.AddSingleton<ShortenViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddTransient<ShortenPage>();
        services.AddTransient<HistoryPage>();
        services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}

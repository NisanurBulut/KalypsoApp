using Kalypso.Client.Services;

namespace Kalypso.Maui.Services;

public sealed class MauiPlatformActions : IPlatformActions
{
    public Task CopyAsync(string text) => Clipboard.Default.SetTextAsync(text);

    public Task ShareAsync(string text) =>
        Share.Default.RequestAsync(new ShareTextRequest { Text = text, Title = "Share your Kalypso link" });

    public async Task OpenAsync(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            await Launcher.Default.OpenAsync(uri);
    }
}

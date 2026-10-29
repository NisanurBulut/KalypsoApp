using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Kalypso.Maui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
// Makes Kalypso appear in the "Share" menu of other apps, e.g. a browser sharing the current page.
[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "text/plain")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Publish(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Publish(intent);
    }

    private static void Publish(Intent? intent)
    {
        if (intent?.Action == Intent.ActionSend && intent.GetStringExtra(Intent.ExtraText) is { Length: > 0 } text)
            IncomingShare.Publish(text);
    }
}

namespace Kalypso.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // The colours are tuned for a light page; do not follow the phone's dark mode.
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState) => new(new AppShell());
}

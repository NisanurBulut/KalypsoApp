using Kalypso.Client.ViewModels;

namespace Kalypso.Maui.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        VersionLabel.Text = $"Kalypso {AppInfo.Current.VersionString}";
    }
}

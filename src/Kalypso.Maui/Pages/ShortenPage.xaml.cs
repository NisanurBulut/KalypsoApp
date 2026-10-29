using Kalypso.Client.ViewModels;

namespace Kalypso.Maui.Pages;

public partial class ShortenPage : ContentPage
{
    private readonly ShortenViewModel _viewModel;

    public ShortenPage(ShortenViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        IncomingShare.Received += OnSharedTextReceived;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TakeSharedText();
    }

    private void OnSharedTextReceived() => MainThread.BeginInvokeOnMainThread(async () =>
    {
        // The user may be on another tab when a link is shared with the app.
        if (Shell.Current is not null)
            await Shell.Current.GoToAsync("//shorten");
        TakeSharedText();
    });

    private void TakeSharedText()
    {
        if (IncomingShare.Take() is { } text)
            _viewModel.ReceiveSharedText(text);
    }
}

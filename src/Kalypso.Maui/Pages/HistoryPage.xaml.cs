using Kalypso.Client.ViewModels;

namespace Kalypso.Maui.Pages;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _viewModel;

    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnClearClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsEmpty)
            return;

        if (await DisplayAlertAsync("Clear history?", "The links stay alive on the server; only this list is emptied.", "Clear", "Keep"))
            await _viewModel.ClearCommand.ExecuteAsync(null);
    }
}

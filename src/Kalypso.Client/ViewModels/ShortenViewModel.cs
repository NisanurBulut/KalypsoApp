using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kalypso.Client.Services;
using Kalypso.Contracts;

namespace Kalypso.Client.ViewModels;

/// <summary>The home screen: paste a link, get a short one, copy / share / open it or scan its QR code.</summary>
public sealed partial class ShortenViewModel(IKalypsoApi api, IHistoryStore history, IPlatformActions platform, IQrCodeService qr) : ObservableObject
{
    [ObservableProperty]
    public partial string Url { get; set; } = "";

    [ObservableProperty]
    public partial string CustomCode { get; set; } = "";

    [ObservableProperty]
    public partial bool UseCustomCode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShortenCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult), nameof(IsFormVisible))]
    public partial LinkResponse? Result { get; set; }

    [ObservableProperty]
    public partial byte[]? QrPng { get; set; }

    /// <summary>A short confirmation such as "Copied to clipboard".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    public partial string StatusMessage { get; set; } = "";

    public bool HasStatus => StatusMessage.Length > 0;
    public bool HasError => ErrorMessage.Length > 0;
    public bool HasResult => Result is not null;
    public bool IsFormVisible => Result is null;

    [RelayCommand(CanExecute = nameof(CanShorten))]
    private async Task ShortenAsync()
    {
        ErrorMessage = "";
        StatusMessage = "";

        var url = NormalizeUrl(Url);
        var code = UseCustomCode ? CustomCode.Trim() : null;
        var problem = LinkRules.ValidateUrl(url) ?? LinkRules.ValidateCode(code);
        if (problem is not null)
        {
            ErrorMessage = problem;
            return;
        }

        IsBusy = true;
        try
        {
            var link = await api.CreateAsync(new CreateLinkRequest(url, code));
            await history.AddAsync(link);
            QrPng = qr.CreatePng(link.ShortUrl);
            Result = link;
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanShorten() => !IsBusy;

    [RelayCommand]
    private async Task CopyAsync()
    {
        if (Result is null) return;
        await platform.CopyAsync(Result.ShortUrl);
        StatusMessage = "Copied to clipboard";
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (Result is not null) await platform.ShareAsync(Result.ShortUrl);
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (Result is not null) await platform.OpenAsync(Result.ShortUrl);
    }

    [RelayCommand]
    private void NewLink()
    {
        Result = null;
        QrPng = null;
        Url = "";
        CustomCode = "";
        UseCustomCode = false;
        ErrorMessage = "";
        StatusMessage = "";
    }

    /// <summary>Called when another app shares text with Kalypso: takes the first web address out of it.</summary>
    public void ReceiveSharedText(string text)
    {
        NewLink();
        Url = ExtractUrl(text);
    }

    public static string ExtractUrl(string text)
    {
        var match = Regex.Match(text, @"https?://\S+", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : text.Trim();
    }

    /// <summary>"example.com/page" becomes "https://example.com/page" so people do not have to type the scheme.</summary>
    public static string NormalizeUrl(string input)
    {
        var url = input.Trim();
        return url.Length > 0 && !url.Contains("://", StringComparison.Ordinal) ? "https://" + url : url;
    }
}

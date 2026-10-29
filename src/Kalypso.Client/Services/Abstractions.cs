using Kalypso.Contracts;

namespace Kalypso.Client.Services;

/// <summary>Talks to the Kalypso API.</summary>
public interface IKalypsoApi
{
    /// <exception cref="ApiException">The API could not be reached or refused the request.</exception>
    Task<LinkResponse> CreateAsync(CreateLinkRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns <c>null</c> when the API does not know the code.</summary>
    /// <exception cref="ApiException">The API could not be reached.</exception>
    Task<LinkResponse?> GetAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>True when the API at the configured address answers its health check.</summary>
    Task<bool> PingAsync(CancellationToken cancellationToken = default);
}

/// <summary>Remembers the short links created on this device.</summary>
public interface IHistoryStore
{
    Task<IReadOnlyList<LinkResponse>> LoadAsync();

    /// <summary>Puts the link at the top; a link with the same code is replaced.</summary>
    Task AddAsync(LinkResponse link);

    Task ReplaceAllAsync(IEnumerable<LinkResponse> links);
    Task RemoveAsync(string code);
    Task ClearAsync();
}

/// <summary>Where the app keeps the API address.</summary>
public interface ISettingsStore
{
    string ApiBaseUrl { get; set; }

    /// <summary>The address that works on a fresh install (it differs per platform).</summary>
    string DefaultApiBaseUrl { get; }
}

/// <summary>Things only the operating system can do. Implemented by the MAUI app.</summary>
public interface IPlatformActions
{
    Task CopyAsync(string text);
    Task ShareAsync(string text);
    Task OpenAsync(string url);
}

public interface IQrCodeService
{
    /// <summary>A PNG image of the QR code for <paramref name="text"/>.</summary>
    byte[] CreatePng(string text);
}

/// <summary>An error that can be shown to the user as it is.</summary>
public sealed class ApiException(string message) : Exception(message);

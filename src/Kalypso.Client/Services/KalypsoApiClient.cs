using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kalypso.Contracts;

namespace Kalypso.Client.Services;

public sealed class KalypsoApiClient(HttpClient http, ISettingsStore settings) : IKalypsoApi
{
    public async Task<LinkResponse> CreateAsync(CreateLinkRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, BuildUri("api/links")) { Content = JsonContent.Create(request) };
        using var response = await SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ApiException(await ReadErrorAsync(response, cancellationToken));

        return await response.Content.ReadFromJsonAsync<LinkResponse>(cancellationToken)
            ?? throw new ApiException("The API sent an answer the app does not understand.");
    }

    public async Task<LinkResponse?> GetAsync(string code, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, BuildUri($"api/links/{Uri.EscapeDataString(code)}"));
        using var response = await SendAsync(message, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
            throw new ApiException(await ReadErrorAsync(response, cancellationToken));

        return await response.Content.ReadFromJsonAsync<LinkResponse>(cancellationToken);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, BuildUri("health"));
            using var response = await SendAsync(message, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (ApiException)
        {
            return false;
        }
    }

    private Uri BuildUri(string path)
    {
        var baseUrl = settings.ApiBaseUrl.Trim().TrimEnd('/') + "/";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            throw new ApiException("The API address in Settings is not valid.");
        return new Uri(baseUri, path);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        try
        {
            return await http.SendAsync(message, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("Cannot reach the Kalypso API. Check your connection and the address in Settings.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiException("The Kalypso API took too long to answer. Please try again.");
        }
    }

    /// <summary>Reads the message of a ProblemDetails / ValidationProblemDetails body, with a fallback per status code.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            return "Too many requests. Please wait a minute and try again.";

        try
        {
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var field in errors.EnumerateObject())
                {
                    if (field.Value.ValueKind == JsonValueKind.Array && field.Value.GetArrayLength() > 0)
                        return field.Value[0].GetString() ?? "The API rejected the request.";
                }
            }

            if (root.TryGetProperty("title", out var title) && title.GetString() is { Length: > 0 } text)
                return text;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // Not a JSON problem document: fall through to the generic message.
        }

        return $"The API answered with an error ({(int)response.StatusCode}).";
    }
}

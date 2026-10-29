namespace Kalypso.Contracts;

/// <summary>Body of <c>POST /api/links</c>.</summary>
/// <param name="Url">The long address to shorten (http or https).</param>
/// <param name="CustomCode">Optional name for the short link. A random code is used when it is empty.</param>
public sealed record CreateLinkRequest(string Url, string? CustomCode = null);

/// <summary>A short link as the API returns it.</summary>
public sealed record LinkResponse(string Code, string ShortUrl, string OriginalUrl, DateTimeOffset CreatedAt, long Clicks);

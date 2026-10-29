namespace Kalypso.Contracts;

/// <summary>
/// Validation rules shared by the API (which enforces them) and the app (which uses them to give feedback
/// before a request is sent). Every method returns an error message, or <c>null</c> when the value is fine.
/// </summary>
public static class LinkRules
{
    public const int MaxUrlLength = 2048;
    public const int MinCodeLength = 4;
    public const int MaxCodeLength = 20;

    /// <summary>Names that would clash with the API's own routes.</summary>
    private static readonly string[] ReservedCodes = ["api", "health", "openapi", "swagger", "favicon.ico"];

    public static string? ValidateUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "Please enter a link.";
        if (url.Length > MaxUrlLength)
            return $"The link is too long (at most {MaxUrlLength} characters).";
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return "Enter a full web address that starts with http:// or https://.";
        return null;
    }

    /// <summary>An empty code is valid: the API then makes one up.</summary>
    public static string? ValidateCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        code = code.Trim();
        if (code.Length < MinCodeLength || code.Length > MaxCodeLength)
            return $"The name must have {MinCodeLength} to {MaxCodeLength} characters.";
        if (!code.All(IsCodeCharacter))
            return "Use only letters, numbers, - and _ in the name.";
        if (ReservedCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
            return "That name is reserved. Please choose another one.";
        return null;
    }

    private static bool IsCodeCharacter(char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '_';
}

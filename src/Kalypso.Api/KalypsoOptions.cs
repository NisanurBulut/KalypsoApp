namespace Kalypso.Api;

/// <summary>Settings from the "Kalypso" section of appsettings.json or environment variables (<c>Kalypso__PublicBaseUrl</c>).</summary>
public sealed class KalypsoOptions
{
    public const string SectionName = "Kalypso";

    /// <summary>
    /// Address used when building short links, e.g. <c>https://kalypso.example.com</c>. Set it when the API runs
    /// behind a proxy; when empty, the address of the incoming request is used.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>How many links one IP address may create per minute.</summary>
    public int CreateLimitPerMinute { get; set; } = 30;
}

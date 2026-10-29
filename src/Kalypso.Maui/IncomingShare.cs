namespace Kalypso.Maui;

/// <summary>
/// Text that another app shared with Kalypso (Android "Share" menu). The platform code publishes it; the
/// Shorten page picks it up, either when it appears (cold start) or right away through <see cref="Received"/>.
/// </summary>
public static class IncomingShare
{
    private static string? _pending;

    public static event Action? Received;

    public static void Publish(string text)
    {
        _pending = text;
        Received?.Invoke();
    }

    /// <summary>Returns the shared text once, then forgets it.</summary>
    public static string? Take()
    {
        var text = _pending;
        _pending = null;
        return text;
    }
}

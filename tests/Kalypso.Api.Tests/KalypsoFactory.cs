using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kalypso.Api.Tests;

/// <summary>Starts the real API with its own throw-away SQLite file and a fixed public address.</summary>
public sealed class KalypsoFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"kalypso-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath};Pooling=False");
        builder.UseSetting("Kalypso:PublicBaseUrl", "https://kly.test");
        builder.UseSetting("Kalypso:CreateLimitPerMinute", "1000");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_databasePath); } catch (IOException) { /* a temp file; the OS cleans it up later */ }
    }
}

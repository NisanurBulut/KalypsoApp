using System.Threading.RateLimiting;
using Kalypso.Api;
using Kalypso.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KalypsoOptions>(builder.Configuration.GetSection(KalypsoOptions.SectionName));

// The connection string is read lazily so tests can replace it through configuration.
builder.Services.AddDbContext<KalypsoDbContext>((services, options) =>
    options.UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString("Default") ?? "Data Source=kalypso.db"));

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(limiter =>
{
    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiter.AddPolicy(Program.CreatePolicy, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = context.RequestServices.GetRequiredService<IOptions<KalypsoOptions>>().Value.CreateLimitPerMinute,
            Window = TimeSpan.FromMinutes(1),
        }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<KalypsoDbContext>().Database.EnsureCreated();
}

app.UseCors();
app.UseRateLimiter();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Json(new { name = "Kalypso API", status = "ok", openapi = "/openapi/v1.json" }))
    .ExcludeFromDescription();
app.MapLinkEndpoints();

app.Run();

/// <summary>Public so the integration tests can start the app with <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program
{
    public const string CreatePolicy = "create-link";
}

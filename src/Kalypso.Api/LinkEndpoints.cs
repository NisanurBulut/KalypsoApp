using Kalypso.Api.Data;
using Kalypso.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kalypso.Api;

public static class LinkEndpoints
{
    // Braces inside a route constraint are written twice: {{4,20}} means the regex quantifier {4,20}.
    private const string CodeRoute = "{code:regex(^[A-Za-z0-9_-]{{4,20}}$)}";
    private const int MaxCodeAttempts = 5;

    public static void MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var links = app.MapGroup("/api/links").WithTags("Links");

        links.MapPost("/", CreateAsync)
            .RequireRateLimiting(Program.CreatePolicy)
            .WithSummary("Shorten a link, optionally with a name of your own.");

        links.MapGet("/" + CodeRoute, GetAsync)
            .WithSummary("Look up a short link and its click count.");

        // The public short address: https://host/AbC123 sends the visitor to the original link.
        app.MapGet("/" + CodeRoute, RedirectAsync)
            .ExcludeFromDescription();
    }

    private static async Task<IResult> CreateAsync(
        CreateLinkRequest request, KalypsoDbContext db, IOptions<KalypsoOptions> options, HttpRequest http, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (LinkRules.ValidateUrl(request.Url) is { } urlError)
            errors["url"] = [urlError];
        if (LinkRules.ValidateCode(request.CustomCode) is { } codeError)
            errors["customCode"] = [codeError];
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var customCode = string.IsNullOrWhiteSpace(request.CustomCode) ? null : request.CustomCode.Trim();
        var taken = Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "That name is already taken. Please choose another one.");

        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var code = customCode ?? CodeGenerator.NewCode();
            if (await db.Links.AnyAsync(l => l.Code == code, ct))
            {
                if (customCode is not null) return taken;
                continue;
            }

            var link = new ShortLink { Code = code, OriginalUrl = request.Url.Trim(), CreatedAt = DateTimeOffset.UtcNow };
            db.Links.Add(link);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Somebody else took the code between the check and the insert.
                db.Entry(link).State = EntityState.Detached;
                if (customCode is not null) return taken;
                continue;
            }

            return Results.Created($"/api/links/{code}", ToResponse(link, options.Value, http));
        }

        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Could not find a free code. Please try again.");
    }

    private static async Task<Results<Ok<LinkResponse>, NotFound>> GetAsync(
        string code, KalypsoDbContext db, IOptions<KalypsoOptions> options, HttpRequest http, CancellationToken ct)
    {
        var link = await db.Links.AsNoTracking().FirstOrDefaultAsync(l => l.Code == code, ct);
        return link is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(link, options.Value, http));
    }

    private static async Task<IResult> RedirectAsync(string code, KalypsoDbContext db, CancellationToken ct)
    {
        var link = await db.Links.AsNoTracking().FirstOrDefaultAsync(l => l.Code == code, ct);
        if (link is null)
            return Results.NotFound();

        await db.Links.Where(l => l.Id == link.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.Clicks, l => l.Clicks + 1), ct);
        return Results.Redirect(link.OriginalUrl); // 302, so every visit reaches the API and is counted
    }

    private static LinkResponse ToResponse(ShortLink link, KalypsoOptions options, HttpRequest http)
    {
        var baseUrl = string.IsNullOrWhiteSpace(options.PublicBaseUrl)
            ? $"{http.Scheme}://{http.Host}"
            : options.PublicBaseUrl.TrimEnd('/');
        return new LinkResponse(link.Code, $"{baseUrl}/{link.Code}", link.OriginalUrl, link.CreatedAt, link.Clicks);
    }
}

using KalypsoApp.Data;
using KalypsoApp.Models;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionStringItem = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<KalypsoDbContext>(o => o.UseSqlite(connectionStringItem));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapPost("/shorturl", handler: async (UrlDto url, KalypsoDbContext db, HttpContext ctx) =>
{
    if (!Uri.TryCreate(url.Url, UriKind.Absolute, out var inputUrl))
        return Results.BadRequest("Invalid Url has been provided");

    var random = new Random();
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789@az";
    var randomStrItem = new string(Enumerable.Repeat(chars, 6)
        .Select(x => x[random.Next(x.Length)]).ToArray());

    var urlModel = new UrlManagement
    {
        Url = url.Url,
        ShortUrl = randomStrItem
    };

    db.Urls.Add(urlModel);
    await db.SaveChangesAsync();

    var resultItem = $"{ctx.Request.Scheme}://{ctx.Request.Host}/{urlModel.ShortUrl}";

    var urlShortItem = new UrlShortResponseDto()
    {
        Url = resultItem
    };
    return Results.Ok(urlShortItem);
});

app.MapFallback(handler: async (KalypsoDbContext db, HttpContext ctx) =>
{
    var pathItem = ctx.Request.Path.ToUriComponent().Trim('/');
    var urlMatchItem = await db.Urls.FirstOrDefaultAsync(x =>
   x.ShortUrl.Trim() == pathItem.Trim());

    if (urlMatchItem == null)
        return Results.BadRequest("Invalid request");

    return Results.Redirect(urlMatchItem.Url);
});

app.Run();


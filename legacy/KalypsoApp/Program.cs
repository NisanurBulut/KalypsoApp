using KalypsoApp;
using KalypsoApp.Data;
using KalypsoApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
var connectionStringItem = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<KalypsoDbContext>(o => o.UseSqlite(connectionStringItem));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors("AnyOrigin");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapPost("/shortenurl", handler: async (UrlCustomDto url, KalypsoDbContext db, HttpContext ctx) =>
{
    if (!Uri.TryCreate(url.Url, UriKind.Absolute, out var inputUrl))
        return Results.BadRequest("Invalid Url has been provided");

    if (string.IsNullOrEmpty(url.UrlCustomPart) || url.UrlCustomPart.Length<7)
        return Results.BadRequest("Valid UrlCustomPart has not been provided");

    var randomStrItem = KalypsoHelper.GetRandomString();

    var resultItem = KalypsoHelper.GetRequestUrl(ctx, randomStrItem);
    var urlModel = new UrlManagement(url.Url, resultItem, randomStrItem);

    db.Urls.Add(urlModel);
    await db.SaveChangesAsync();


    var urlShortItem = new UrlShortResponseDto(resultItem);
    return Results.Ok(urlShortItem);
});

app.MapGet("/getlongurl", handler: async (string urlParam, KalypsoDbContext db, HttpContext ctx) =>
{
   
    if(String.IsNullOrEmpty(urlParam) || !Uri.IsWellFormedUriString(urlParam, UriKind.Absolute))
    {
        return Results.BadRequest("Invalid urlParam");
    }

    Uri baseUri = new Uri(urlParam); 
    if (!Uri.TryCreate(baseUri.AbsoluteUri, UriKind.Absolute, out var inputUrl))
        return Results.BadRequest("Invalid Url has been provided");


    var urlMatchItem = await db.Urls.FirstOrDefaultAsync(x => x.ShortUrl.Trim() == urlParam.Trim());

    if (urlMatchItem == null)
        return Results.BadRequest("Not Found Short URL");

    return Results.Redirect(urlMatchItem.Url);
});

app.MapPost("/shorturl", handler: async (UrlDto url, KalypsoDbContext db, HttpContext ctx) =>
{
    if (!Uri.TryCreate(url.Url, UriKind.Absolute, out var inputUrl))
        return Results.BadRequest("Invalid Url has been provided");

    var randomStrItem = KalypsoHelper.GetRandomString();
    var resultItem = KalypsoHelper.GetRequestUrl(ctx,randomStrItem);
    var urlModel = new UrlManagement(url.Url, resultItem, randomStrItem);

    db.Urls.Add(urlModel);
    await db.SaveChangesAsync();

    var urlShortItem = new UrlShortResponseDto(resultItem);
    return Results.Ok(urlShortItem);
});

app.MapFallback(handler: async (KalypsoDbContext db, HttpContext ctx) =>
{
    var pathItem = ctx.Request.Path.ToUriComponent().Trim('/');
    var urlMatchItem = await db.Urls.FirstOrDefaultAsync(x =>
   x.ShortUrlPart.Trim() == pathItem.Trim());

    if (urlMatchItem == null)
        return Results.BadRequest("Invalid request");

    return Results.Redirect(urlMatchItem.Url);
});

app.Run();


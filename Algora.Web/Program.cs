using Algora.Application.Interfaces;
using Algora.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShopifySharp;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

builder.Services.Configure<ShopifyOptions>(config.GetSection("Shopify"));

// Optional: make the bound options instance directly available if you need the POCO
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ShopifyOptions>>().Value);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(config.GetConnectionString("Default") ?? "Data Source=algora.db"));

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// OAuth install
app.MapGet("/auth/install", ([FromQuery] string shop, [FromServices] IOptions<ShopifyOptions> opt, HttpResponse res) =>
{
    var state = Guid.NewGuid().ToString("N");
    res.Cookies.Append("state", state, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });
    var url = "https://" + shop + "/admin/oauth/authorize?client_id=" + opt.Value.ApiKey + "&scope=" + Uri.EscapeDataString(opt.Value.Scopes) + "&redirect_uri=" + Uri.EscapeDataString(opt.Value.AppUrl + "/auth/callback") + "&state=" + state;
    return Results.Redirect(url);
});

// OAuth callback
app.MapGet("/auth/callback", async (HttpContext http, [FromServices] IOptions<ShopifyOptions> opt, [FromServices] IShopifyOAuthService oauth) =>
{
    var q = http.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
    if (!q.TryGetValue("shop", out var shop) || !q.TryGetValue("code", out var code)) return Results.BadRequest("Missing shop/code");
    if (!ShopifyHmac.IsAuthenticQuery(q, opt.Value.ApiSecret)) return Results.Unauthorized();
    var stateCookie = http.Request.Cookies["state"];
    if (q.TryGetValue("state", out var state) && stateCookie != state) return Results.Unauthorized();

    var token = await oauth.ExchangeCodeForTokenAsync(shop, code);
    return Results.Redirect("/app?shop=" + shop);
});

// Webhooks
app.MapPost("/webhooks/{topic}", async (HttpRequest req, [FromRoute] string topic, [FromServices] IOptions<ShopifyOptions> opt, [FromServices] AppDbContext db) =>
{
    var shop = req.Headers["X-Shopify-Shop-Domain"].ToString();
    var hmacHeader = req.Headers["X-Shopify-Hmac-Sha256"].ToString();
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    if (!ShopifyHmac.IsAuthenticWebhook(hmacHeader, body, opt.Value.ApiSecret)) return Results.Unauthorized();

    db.WebhookLogs.Add(new Algora.Domain.Entities.WebhookLog { Shop = shop, Topic = topic, Payload = body });
    await db.SaveChangesAsync();
    return Results.Ok();
});

// Example GraphQL API
app.MapGet("/api/products", async ([FromQuery] string shop, [FromServices] IShopifyOAuthService oauth, [FromServices] IShopifyGraphService graph) =>
{
    var token = await oauth.GetAccessTokenAsync(shop);
    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

    var query = "{\n  products(first: 20) {\n    edges {\n      node {\n        id\n        title\n        status\n      }\n    }\n  }\n}";
    var json = await graph.PostAsync(shop, token, query);
    return Results.Content(json, "application/json");
});

app.MapGet("/", context =>
{
    context.Response.Redirect("/dashboard");
    return Task.CompletedTask;
});

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

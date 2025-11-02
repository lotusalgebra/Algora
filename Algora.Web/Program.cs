using Algora.Application.Interfaces;
using Algora.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShopifySharp;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

// Bind Shopify-related configuration from appsettings.json -> Shopify section
builder.Services.Configure<ShopifyOptions>(config.GetSection("Shopify"));

// Optional: expose the bound ShopifyOptions POCO directly for code that prefers the concrete type
// Prefer injecting IOptions<ShopifyOptions> into services where possible.
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ShopifyOptions>>().Value);

// Configure EF Core with SQLite using the "Default" connection string (fallback to local file)
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(config.GetConnectionString("Default") ?? "Data Source=algora.db"));

// Register infrastructure services (repository, shop context, Shopify clients, app services).
// The extension method AddInfrastructureServices centralizes registrations to keep Program.cs tidy.
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Razor Pages support for the UI (this is a Razor Pages project)
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Enable swagger in development to inspect APIs quickly
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// OAuth install endpoint
// - Accepts `shop` as query string and builds the OAuth install URL for the merchant.
// - Saves a short-lived `state` cookie to validate the callback.
app.MapGet("/auth/install", ([FromQuery] string shop, [FromServices] IOptions<ShopifyOptions> opt, HttpResponse res) =>
{
    var state = Guid.NewGuid().ToString("N");
    res.Cookies.Append("state", state, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });
    var url = "https://" + shop + "/admin/oauth/authorize?client_id=" + opt.Value.ApiKey + "&scope=" + Uri.EscapeDataString(opt.Value.Scopes) + "&redirect_uri=" + Uri.EscapeDataString(opt.Value.AppUrl + "/auth/callback") + "&state=" + state;
    return Results.Redirect(url);
});

// OAuth callback endpoint
// - Validates HMAC and state cookie then exchanges the temporary code for a permanent access token.
// - On success redirects to the application UI.
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

// Webhooks receiver
// - Validates the webhook HMAC and persists the raw payload to the database for auditing/troubleshooting.
// - Keeps the handler minimal and fast; heavy work should be queued to background processing.
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

// Example GraphQL proxy endpoint
// - Demonstrates how to call the Shopify GraphQL wrapper service.
// - Expects `shop` query parameter and uses IShopifyOAuthService to obtain the stored access token.
app.MapGet("/api/products", async ([FromQuery] string shop, [FromServices] IShopifyOAuthService oauth, [FromServices] IShopifyGraphService graph) =>
{
    var token = await oauth.GetAccessTokenAsync(shop);
    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

    var query = "{\n  products(first: 20) {\n    edges {\n      node {\n        id\n        title\n        status\n      }\n    }\n  }\n}";
    var json = await graph.PostAsync(shop, token, query);
    return Results.Content(json, "application/json");
});

// Root redirect to application's dashboard
app.MapGet("/", context =>
{
    context.Response.Redirect("/dashboard");
    return Task.CompletedTask;
});

// Routing & authorization middleware for Razor Pages
app.UseRouting();
app.UseAuthorization();

// Static assets mapping and Razor Pages endpoints
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

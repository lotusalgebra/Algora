using Algora.Application.Interfaces;
using Algora.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

// Preload libwkhtmltox native library from known paths (must run before host is built)
var baseDir = AppContext.BaseDirectory;
string[] nativeCandidates =
{
    Path.Combine(baseDir, "runtimes", "win-x64", "native"),
    Path.Combine(baseDir, "native"),
    baseDir
};

bool loaded = false;
foreach (var nativeDir in nativeCandidates)
{
    if (!Directory.Exists(nativeDir)) continue;

    var libPath1 = Path.Combine(nativeDir, "libwkhtmltox.dll");
    var libPath2 = Path.Combine(nativeDir, "wkhtmltox.dll");

    var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
    if (!currentPath.Contains(nativeDir, StringComparison.OrdinalIgnoreCase))
    {
        Environment.SetEnvironmentVariable("PATH", nativeDir + Path.PathSeparator + currentPath);
    }

    if (File.Exists(libPath1) && NativeLibrary.TryLoad(libPath1, out _)) { loaded = true; break; }
    if (File.Exists(libPath2) && NativeLibrary.TryLoad(libPath2, out _)) { loaded = true; break; }
}

if (!loaded)
{
    Console.WriteLine($"WARNING: wkhtmltopdf native library not found. PDF generation will fail.");
    Console.WriteLine($"Searched: {string.Join(", ", nativeCandidates)}");
}

var builder = WebApplication.CreateBuilder(args);

// Register all infrastructure services (DB, Shopify, PDF, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();

// ----- OAuth endpoints (minimal APIs) -----

// Helper: validate Shopify HMAC
static bool IsValidShopifyHmac(string secret, IQueryCollection query)
{
    if (!query.TryGetValue("hmac", out var hmacValues)) return false;
    var hmac = hmacValues.ToString();

    var items = query
        .Where(kv => kv.Key != "hmac" && kv.Key != "signature")
        .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString()))
        .OrderBy(kv => kv.Key, StringComparer.Ordinal)
        .Select(kv => $"{kv.Key}={kv.Value}");

    var message = string.Join("&", items);
    using var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(message));
    var calc = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    return calc == hmac;
}

// OAuth install endpoint
app.MapGet("/auth/install", ([FromQuery] string shop, [FromServices] ShopifyOptions opt, HttpResponse res) =>
{
    if (string.IsNullOrWhiteSpace(shop)) return Results.BadRequest("shop query is required");
    var state = Guid.NewGuid().ToString("N");
    res.Cookies.Append("shopify_state", state, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });

    var redirectUri = opt.AppUrl.TrimEnd('/') + "/auth/callback";
    var url = $"https://{shop}/admin/oauth/authorize?client_id={opt.ApiKey}&scope={Uri.EscapeDataString(opt.Scopes)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}";
    return Results.Redirect(url);
});

// OAuth callback endpoint
app.MapGet("/auth/callback", async (HttpContext http, [FromServices] ShopifyOptions opt, [FromServices] IShopifyOAuthService oauth) =>
{
    var q = http.Request.Query;
    var shop = q["shop"].ToString();
    var code = q["code"].ToString();
    var state = q["state"].ToString();

    if (!http.Request.Cookies.TryGetValue("shopify_state", out var savedState) || savedState != state)
        return Results.BadRequest("Invalid state");

    if (!IsValidShopifyHmac(opt.ApiSecret, http.Request.Query))
        return Results.BadRequest("Invalid HMAC");

    try
    {
        var token = await oauth.ExchangeCodeForTokenAsync(shop, code);
        return Results.Redirect("/dashboard");
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message);
    }
});

app.MapControllers();
app.MapRazorPages();
app.Run();

using Algora.Application.Interfaces;
using Algora.Infrastructure;
using Algora.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
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

// ----- Auth Microservice Client -----
builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>();

// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// ----- OAuth endpoints (minimal APIs) -----

// Update the OAuth endpoints to use the service methods

// OAuth install endpoint
app.MapGet("/auth/install", async ([FromQuery] string shop, [FromServices] IShopifyOAuthService oauth, HttpResponse res) =>
{
    if (string.IsNullOrWhiteSpace(shop)) return Results.BadRequest("shop query is required");
    
    var state = Guid.NewGuid().ToString("N");
    res.Cookies.Append("shopify_state", state, new CookieOptions 
    { 
        HttpOnly = true, 
        Secure = true, 
        SameSite = SameSiteMode.None 
    });

    var url = await oauth.GetAuthorizationUrlAsync(shop, state);
    return Results.Redirect(url);
});

// OAuth callback endpoint  
app.MapGet("/auth/callback", async (HttpContext http, [FromServices] IShopifyOAuthService oauth) =>
{
    var q = http.Request.Query;
    var shop = q["shop"].ToString();
    var code = q["code"].ToString();
    var state = q["state"].ToString();
    var hmac = q["hmac"].ToString();

    if (!http.Request.Cookies.TryGetValue("shopify_state", out var savedState) || savedState != state)
        return Results.BadRequest("Invalid state");

    // Build message for HMAC validation
    var items = q
        .Where(kv => kv.Key != "hmac" && kv.Key != "signature")
        .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString()))
        .OrderBy(kv => kv.Key, StringComparer.Ordinal)
        .Select(kv => $"{kv.Key}={kv.Value}");
    var message = string.Join("&", items);

    if (!await oauth.ValidateHmacAsync(shop, message, hmac))
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



using Algora.Application.Interfaces;
using Algora.Infrastructure;
using Algora.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;

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

// Seed default plans on startup
using (var scope = app.Services.CreateScope())
{
    var planService = scope.ServiceProvider.GetRequiredService<IPlanService>();
    await planService.SeedDefaultPlansAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

// Handle Shopify embedded app entry point (root URL with shop parameter)
app.MapGet("/", async (HttpContext ctx, Algora.Infrastructure.Data.AppDbContext db, IOptions<ShopifyOptions> opt) =>
{
    var shop = ctx.Request.Query["shop"].ToString();

    // If no shop parameter, redirect to login
    if (string.IsNullOrEmpty(shop))
    {
        return Results.Redirect("/auth/login");
    }

    // Validate HMAC if present
    var hmac = ctx.Request.Query["hmac"].ToString();
    if (!string.IsNullOrEmpty(hmac))
    {
        var query = ctx.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        if (!ShopifyHmac.IsAuthenticQuery(query, opt.Value.ApiSecret))
        {
            return Results.Unauthorized();
        }
    }

    // Check if shop is installed
    var dbShop = await db.Shops.FirstOrDefaultAsync(s => s.Domain == shop);
    if (dbShop == null || string.IsNullOrEmpty(dbShop.OfflineAccessToken))
    {
        return Results.Redirect($"/auth/install?shop={Uri.EscapeDataString(shop)}");
    }

    // Redirect to dashboard
    var host = ctx.Request.Query["host"].ToString();
    if (!string.IsNullOrEmpty(host))
    {
        return Results.Redirect($"/Dashboard?shop={Uri.EscapeDataString(shop)}&host={Uri.EscapeDataString(host)}");
    }
    return Results.Redirect($"/Dashboard?shop={Uri.EscapeDataString(shop)}");
});

app.Run();



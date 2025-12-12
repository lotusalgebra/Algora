using Algora.Application.Interfaces;
using Algora.Infrastructure;
using Algora.Infrastructure.Data;
using Algora.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using DinkToPdf;
using DinkToPdf.Contracts;
using System;
using System.IO;
using System.Runtime.InteropServices;

// Preload libwkhtmltox native library from a known path
var baseDir = AppContext.BaseDirectory;
var nativeDir = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native");
var libPath1 = Path.Combine(nativeDir, "libwkhtmltox.dll");
var libPath2 = Path.Combine(nativeDir, "wkhtmltox.dll");

// Expand PATH to include the native directory
var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
if (!currentPath.Contains(nativeDir, StringComparison.OrdinalIgnoreCase))
{
    Environment.SetEnvironmentVariable("PATH", nativeDir + Path.PathSeparator + currentPath);
}

// Try to load either lib name
bool loaded = false;
if (File.Exists(libPath1) && NativeLibrary.TryLoad(libPath1, out _)) loaded = true;
else if (File.Exists(libPath2) && NativeLibrary.TryLoad(libPath2, out _)) loaded = true;

if (!loaded)
{
    // Fallback: rely on PATH (may still succeed if files are elsewhere)
    // If it still fails at runtime, ensure the DLLs exist at nativeDir or adjust this path.
    Console.WriteLine($"wkhtmltopdf native library not found at: {nativeDir}");
}

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// DbContext (example; keep your existing registration if different)
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(config.GetConnectionString("Default") ?? "Data Source=algora.db"));

// allow controllers to return views and still use Razor Pages files
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// make MVC view engine also search the Pages folder
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    // Search Pages per-controller: /Pages/{Controller}/{View}.cshtml
    options.ViewLocationFormats.Insert(0, "/Pages/{1}/{0}.cshtml");
    // Shared pages folder
    options.ViewLocationFormats.Insert(1, "/Pages/Shared/{0}.cshtml");
});

// Register application services used by Pages\InvoiceDownload.cshtml.cs
builder.Services.AddSingleton<IInvoiceTemplateService, InvoiceTemplateService>();

// Register wkhtmltopdf converter and PDF service
var pdfConverter = new SynchronizedConverter(new PdfTools());
builder.Services.AddSingleton<IConverter>(pdfConverter);
builder.Services.AddSingleton<IPdfGeneratorService, WkHtmlToPdfGeneratorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Serve static files from wwwroot
app.UseStaticFiles();

app.UseAuthorization();

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

using Algora.Application.Interfaces;
using Algora.Infrastructure;
using Algora.WhatsApp;
using Algora.Web.Services;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Register all infrastructure services (DB, Shopify, PDF, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register WhatsApp module (Facebook WhatsApp Business API)
builder.Services.AddWhatsAppModule(builder.Configuration);

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

// Add authorization with dynamic feature policy provider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, FeaturePolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, FeatureAuthorizationHandler>();
builder.Services.AddAuthorization();

var app = builder.Build();

// Seed default plans and features on startup
using (var scope = app.Services.CreateScope())
{
    var planService = scope.ServiceProvider.GetRequiredService<IPlanService>();
    await planService.SeedDefaultPlansAsync();

    var featureService = scope.ServiceProvider.GetRequiredService<IPlanFeatureService>();
    await featureService.SeedDefaultFeaturesAsync();
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



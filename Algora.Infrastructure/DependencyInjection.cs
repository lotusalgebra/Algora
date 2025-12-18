using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Algora.Infrastructure.Licensing;
using Algora.Infrastructure.Persistence;
using Algora.Infrastructure.Services;
using Algora.Infrastructure.Services.Communication;
using Algora.Infrastructure.Shopify;
using Algora.Infrastructure.Shopify.Billing;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // ----- Database -----
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));

        // ----- Shopify config (app-level defaults, can be overridden per shop) -----
        services.Configure<ShopifyOptions>(configuration.GetSection("Shopify"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ShopifyOptions>>().Value);

        // ----- MVC / Razor Pages -----
        services.AddControllersWithViews();
        services.AddRazorPages();
        services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationFormats.Insert(0, "/Pages/{1}/{0}.cshtml");
            options.ViewLocationFormats.Insert(1, "/Pages/Shared/{0}.cshtml");
        });

        // ----- HttpContext accessor -----
        services.AddHttpContextAccessor();

        // ----- Shop management -----
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IShopContext, HttpShopContext>();
        services.AddScoped<IShopifyOAuthService, ShopifyOAuthService>();
        services.AddScoped<IShopifyGraphClient, ShopifyGraphClient>();

        // ----- Shopify functional services -----
        services.AddScoped<IShopifyOrderService, ShopifyOrderService>();
        services.AddScoped<IShopifyCustomerService, ShopifyCustomerService>();
        services.AddScoped<IShopifyInvoiceService, ShopifyInvoiceService>();
        services.AddScoped<IShopifyProductService, ShopifyProductGraphService>();
        services.AddScoped<ShopifyProductGraphService>(); // Also register concrete type for direct use
        services.AddScoped<IAbandonedCartService, AbandonedCartService>();

        // ----- Communication services (per-shop settings from database) -----
        services.AddScoped<ICommunicationSettingsService, CommunicationSettingsService>();
        services.AddScoped<IEmailMarketingService, EmailMarketingService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IWhatsAppService, WhatsAppService>();
        services.AddScoped<INotificationService, NotificationService>();

        // ----- Template and PDF generation -----
        services.AddScoped<IInvoiceTemplateService, InvoiceTemplateService>();
        services.AddSingleton<IPdfGeneratorService, QuestPdfInvoiceGeneratorService>();
        services.AddSingleton<QuestPdfInvoiceGeneratorService>(); // Also register concrete type for direct use

        // ----- Billing & Licensing -----
        services.AddScoped<IShopifyBillingService, ShopifyBillingService>();
        services.AddScoped<ILicenseService, LicenseService>();

        // ----- Repository layer -----
        services.AddRepositoryLayer();

        // ----- HttpClient -----
        services.AddHttpClient();
        services.AddMemoryCache();
        services.AddScoped<IAppConfigurationService, AppConfigurationService>();

        return services;
    }
}

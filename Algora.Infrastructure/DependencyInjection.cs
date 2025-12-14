using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Algora.Infrastructure.Licensing;
using Algora.Infrastructure.Persistence;
using Algora.Infrastructure.Services;
using Algora.Infrastructure.Shopify;
using Algora.Infrastructure.Shopify.Billing;
using DinkToPdf;
using DinkToPdf.Contracts;
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
        // ----- Database (SQL Server LocalDB) -----
        string? connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found in configuration.");
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));

        // ----- Shopify config -----
        services.Configure<ShopifyOptions>(configuration.GetSection("Shopify"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ShopifyOptions>>().Value);

        // ----- MVC / Razor Pages -----
        services.AddControllersWithViews();
        services.AddRazorPages();

        // Make MVC view engine also search the Pages folder
        services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationFormats.Insert(0, "/Pages/{1}/{0}.cshtml");
            options.ViewLocationFormats.Insert(1, "/Pages/Shared/{0}.cshtml");
        });

        // ----- Shopify context & GraphQL -----
        services.AddScoped<IShopContext, ShopContext>();
        services.AddScoped<IShopifyGraphClient, ShopifyGraphClient>();

        // ----- Shopify functional services -----
        services.AddScoped<IShopifyOrderService, ShopifyOrderService>();
        services.AddScoped<IShopifyCustomerService, ShopifyCustomerService>();
        services.AddScoped<IShopifyInvoiceService, ShopifyInvoiceService>();
        services.AddScoped<IShopifyProductService, ShopifyProductGraphService>();

        // ----- Template and PDF generation -----
        services.AddScoped<IInvoiceTemplateService, InvoiceTemplateService>();

        // wkhtmltopdf converter (singleton, thread-safe via SynchronizedConverter)
        var pdfConverter = new SynchronizedConverter(new PdfTools());
        services.AddSingleton<IConverter>(pdfConverter);
        services.AddSingleton<IPdfGeneratorService, WkHtmlToPdfGeneratorService>();

        // ----- Billing & Licensing -----
        services.AddScoped<IShopifyBillingService, ShopifyBillingService>();
        services.AddScoped<ILicenseService, LicenseService>();

        // ----- HttpContext & Shop context -----
        services.AddHttpContextAccessor();
        services.AddScoped<IShopContext, HttpShopContext>();
        services.AddScoped<IShopifyOAuthService, ShopifyOAuthService>();

        // ----- Repository layer -----
        services.AddRepositoryLayer();

        // ----- HttpClient (for Shopify REST + GraphQL) -----
        services.AddHttpClient();

        return services;
    }
}

using Algora.Application.Interfaces;
using Algora.Infrastructure.Licensing;
using Algora.Infrastructure.Persistence;
using Algora.Infrastructure.Services;
using Algora.Infrastructure.Shopify;
using Algora.Infrastructure.Shopify.Billing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Shopify config
        services.Configure<ShopifyOptions>(configuration.GetSection("Shopify"));
        // Bind Shopify-related configuration from appsettings.json -> Shopify section
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ShopifyOptions>>().Value);


        // Shopify context & GraphQL
        services.AddScoped<IShopContext, ShopContext>();
        services.AddScoped<IShopifyGraphClient, ShopifyGraphClient>();

        // Shopify functional services
        services.AddScoped<IShopifyOrderService, ShopifyOrderService>();
        services.AddScoped<IShopifyCustomerService, ShopifyCustomerService>();
        services.AddScoped<IShopifyInvoiceService, ShopifyInvoiceService>();
        services.AddScoped<IShopifyProductService, ShopifyProductGraphService>();

        // Template and PDF generation
        services.AddScoped<IInvoiceTemplateService, InvoiceTemplateService>();
        services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();

        services.AddScoped<IShopifyBillingService, ShopifyBillingService>();
        services.AddScoped<ILicenseService, LicenseService>();


        // Register IHttpContextAccessor and shop-related services
        services.AddHttpContextAccessor();
        services.AddScoped<IShopContext,HttpShopContext>();
        services.AddScoped<IShopifyOAuthService, ShopifyOAuthService>(); // implementation exists in infrastructure
                                                                         // Register your existing Shopify services (customers/orders) if not already registered
        services.AddScoped<IShopifyCustomerService,ShopifyCustomerService>();
        services.AddScoped<IShopifyOrderService, ShopifyOrderService>();

        services.AddRepositoryLayer();

        // Register HttpClient (for Shopify REST + GraphQL)
        services.AddHttpClient();

        return services;
    }
}

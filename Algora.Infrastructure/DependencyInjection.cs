using Algora.Application.Interfaces;
using Algora.Infrastructure.Persistence;
using Algora.Infrastructure.Services;
using Algora.Infrastructure.Shopify;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Algora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Shopify config
        services.Configure<ShopifyOptions>(configuration.GetSection("Shopify"));

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

        services.AddRepositoryLayer();

        // Register HttpClient (for Shopify REST + GraphQL)
        services.AddHttpClient();

        return services;
    }
}

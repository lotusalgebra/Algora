using Algora.Application.Interfaces;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.AI.Configuration;
using Algora.Infrastructure.AI.Providers.Image;
using Algora.Infrastructure.AI.Providers.Text;
using Algora.Infrastructure.AI.Services;
using Algora.Infrastructure.Data;
using Algora.Infrastructure.Licensing;
using Algora.Infrastructure.Persistence;
using Algora.Infrastructure.Services;
using Algora.Infrastructure.Services.Communication;
using Algora.Infrastructure.Services.Operations;
using Algora.Infrastructure.Services.CustomerHub;
using Algora.Infrastructure.Services.Scrapers;
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
        // ----- QuestPDF License -----
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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
        services.AddScoped<IWebhookSyncService, WebhookSyncService>();
        services.AddScoped<IWebhookRegistrationService, WebhookRegistrationService>();

        // ----- Communication services (per-shop settings from database) -----
        services.AddScoped<ICommunicationSettingsService, CommunicationSettingsService>();
        services.AddScoped<IEmailMarketingService, EmailMarketingService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICommunicationHistoryService, CommunicationHistoryService>();

        // ----- Template and PDF generation -----
        services.AddScoped<IInvoiceTemplateService, InvoiceTemplateService>();
        services.AddSingleton<IPdfGeneratorService, QuestPdfInvoiceGeneratorService>();
        services.AddSingleton<QuestPdfInvoiceGeneratorService>(); // Also register concrete type for direct use

        // ----- Billing & Licensing -----
        services.AddScoped<IShopifyBillingService, ShopifyBillingService>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IPlanFeatureService, PlanFeatureService>();
        services.AddScoped<IClientService, ClientService>();

        // ----- Repository layer -----
        services.AddRepositoryLayer();

        // ----- HttpClient -----
        services.AddHttpClient();
        services.AddMemoryCache();
        services.AddScoped<IAppConfigurationService, AppConfigurationService>();

        // ----- Global Settings & Encryption -----
        services.AddDataProtection();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddScoped<IGlobalSettingsService, GlobalSettingsService>();

        // ----- Inventory Prediction -----
        services.AddScoped<IInventoryPredictionService, InventoryPredictionService>();
        services.AddScoped<IInventoryAlertService, InventoryAlertService>();
        services.AddHostedService<InventoryPredictionBackgroundService>();

        // ----- Upsell & A/B Testing -----
        services.AddScoped<IProductAffinityService, ProductAffinityService>();
        services.AddScoped<IUpsellRecommendationService, UpsellRecommendationService>();
        services.AddScoped<IUpsellExperimentService, UpsellExperimentService>();
        services.AddHostedService<ProductAffinityBackgroundService>();

        // ----- Returns & Shippo -----
        services.Configure<ShippoOptions>(configuration.GetSection(ShippoOptions.SectionName));
        services.AddScoped<IReturnService, ReturnService>();
        services.AddScoped<IShippoService, ShippoService>();
        services.AddHttpClient("Shippo", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // ----- Bundle Builder -----
        services.AddScoped<IBundleService, BundleService>();
        services.AddScoped<IBundleShopifyService, BundleShopifyService>();

        // ----- Review Importer -----
        services.Configure<ScraperApiOptions>(configuration.GetSection(ScraperApiOptions.SectionName));
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IReviewImportService, ReviewImportService>();
        services.AddScoped<IReviewEmailService, ReviewEmailService>();
        services.AddScoped<IReviewScraper, AmazonReviewScraper>();
        services.AddScoped<IReviewScraper, AliExpressReviewScraper>();
        services.AddHostedService<ReviewImportBackgroundService>();
        services.AddHostedService<ReviewEmailBackgroundService>();
        services.AddHttpClient("ReviewScraper", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        });

        // ----- AI Content Generation -----
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        // Text generation providers
        services.AddScoped<ITextGenerationProvider, OpenAiTextProvider>();
        services.AddScoped<ITextGenerationProvider, AnthropicTextProvider>();
        services.AddScoped<ITextGenerationProvider, GeminiTextProvider>();

        // Image generation providers
        services.AddScoped<IImageGenerationProvider, DallEImageProvider>();
        services.AddScoped<IImageGenerationProvider, StabilityAiImageProvider>();

        // AI orchestrator service
        services.AddScoped<IAiContentService, AiContentService>();

        // Named HttpClients for AI providers
        services.AddHttpClient("OpenAI", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddHttpClient("Anthropic", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddHttpClient("Gemini");
        services.AddHttpClient("StabilityAI", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        
        // ----- Analytics Dashboard -----
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddHostedService<AnalyticsBackgroundService>();

        // ----- Meta Ads Integration -----
        services.AddHttpClient("MetaAds", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddScoped<IMetaAdsService, Services.Advertising.MetaAdsService>();
        services.AddHostedService<Services.Advertising.MetaAdsSyncBackgroundService>();

        // ----- Google Ads Integration -----
        services.AddHttpClient("GoogleAds", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddScoped<IGoogleAdsService, Services.Advertising.GoogleAdsService>();
        services.AddHostedService<Services.Advertising.GoogleAdsSyncBackgroundService>();

        // ----- TikTok Ads Integration -----
        services.AddHttpClient("TikTokAds", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddScoped<ITikTokAdsService, Services.Advertising.TikTokAdsService>();
        services.AddHostedService<Services.Advertising.TikTokAdsSyncBackgroundService>();

        // ----- Pinterest Ads Integration -----
        services.AddHttpClient("PinterestAds", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddScoped<IPinterestAdsService, Services.Advertising.PinterestAdsService>();
        services.AddHostedService<Services.Advertising.PinterestAdsSyncBackgroundService>();

        // ----- Reporting -----
        services.AddScoped<IReportingService, Services.Reporting.ReportingService>();

        // ----- Marketing Automation -----
        services.AddScoped<IMarketingAutomationService, MarketingAutomationService>();
        services.AddScoped<IPersonalizationService, PersonalizationService>();
        services.AddScoped<IABTestService, ABTestService>();
        services.AddHostedService<MarketingAutomationBackgroundService>();

        // ----- Operations Manager -----
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IBarcodeService, BarcodeService>();
        services.AddScoped<IPackingSlipService, PackingSlipService>();
        services.AddScoped<ILabelDesignerService, LabelDesignerService>();
        services.AddHostedService<PurchaseOrderBackgroundService>();

        // ----- Customer Experience Hub -----
        services.AddScoped<IUnifiedInboxService, UnifiedInboxService>();
        services.AddScoped<IAiResponseService, AiResponseService>();
        services.AddScoped<ISocialMediaService, SocialMediaService>();
        services.AddScoped<IExchangeService, ExchangeService>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<IChatbotBridgeService, ChatbotBridgeService>();
        services.AddHostedService<LoyaltyBackgroundService>();

        // HttpClient for Chatbot API Bridge
        services.AddHttpClient<IChatbotBridgeService, ChatbotBridgeService>(client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // AI text providers with fallback support (OpenAI -> Anthropic)
        services.AddScoped<OpenAiTextSimpleProvider>();
        services.AddScoped<AnthropicTextSimpleProvider>();
        services.AddScoped<IAiTextProvider, FallbackAiTextProvider>();

        // ----- AI Assistant Features -----
        services.AddScoped<ISeoOptimizerService, SeoOptimizerService>();
        services.AddScoped<IChatbotService, ChatbotService>();
        services.AddScoped<IPricingOptimizerService, PricingOptimizerService>();

        // HttpClient for Meta Graph API (Facebook/Instagram)
        services.AddHttpClient("MetaGraphAPI", client =>
        {
            client.BaseAddress = new Uri("https://graph.facebook.com/v18.0/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }
}

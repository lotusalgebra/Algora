using Algora.Chatbot.Application.Interfaces;
using Algora.Chatbot.Application.Interfaces.AI;
using Algora.Chatbot.Application.Interfaces.Services;
using Algora.Chatbot.Application.Interfaces.Shopify;
using Algora.Chatbot.Infrastructure.AI.Configuration;
using Algora.Chatbot.Infrastructure.AI.Providers;
using Algora.Chatbot.Infrastructure.AI.Services;
using Algora.Chatbot.Infrastructure.Data;
using Algora.Chatbot.Infrastructure.Services;
using Algora.Chatbot.Infrastructure.Shopify;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Algora.Chatbot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("Default");
        services.AddDbContext<ChatbotDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Configuration
        services.Configure<AiOptions>(configuration.GetSection("AI"));
        services.Configure<ShopifyOptions>(configuration.GetSection("Shopify"));

        // HttpClients
        services.AddHttpClient("OpenAI", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient("Anthropic", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient("Gemini");

        services.AddHttpClient("Shopify", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // AI Providers
        services.AddScoped<IChatbotAiProvider, OpenAiChatProvider>();
        services.AddScoped<IChatbotAiProvider, AnthropicChatProvider>();
        services.AddScoped<IChatbotAiProvider, GeminiChatProvider>();

        // AI Orchestrator
        services.AddScoped<IChatbotOrchestrator, ChatbotOrchestrator>();

        // Services
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IShopContext, HttpShopContext>();

        // Shopify Services
        services.AddScoped<IShopifyOAuthService, ShopifyOAuthService>();

        return services;
    }
}

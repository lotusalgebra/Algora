using Algora.WhatsApp.Configuration;
using Algora.WhatsApp.Data;
using Algora.WhatsApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Algora.WhatsApp;

/// <summary>
/// Extension methods for registering WhatsApp module services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds WhatsApp module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWhatsAppModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));

        // Database context
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");
        services.AddDbContext<WhatsAppDbContext>(options => options.UseSqlServer(connectionString));

        // Services
        services.AddScoped<IWhatsAppService, WhatsAppService>();

        // HttpClient for WhatsApp API
        services.AddHttpClient("WhatsApp", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }

    /// <summary>
    /// Adds WhatsApp module services with a custom DbContext configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="dbContextOptions">Custom DbContext options action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWhatsAppModule(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> dbContextOptions)
    {
        // Configuration
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));

        // Database context with custom options
        services.AddDbContext<WhatsAppDbContext>(dbContextOptions);

        // Services
        services.AddScoped<IWhatsAppService, WhatsAppService>();

        // HttpClient for WhatsApp API
        services.AddHttpClient("WhatsApp", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }
}

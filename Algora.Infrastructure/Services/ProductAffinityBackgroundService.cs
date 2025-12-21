using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Background service that periodically recalculates product affinities
/// and checks for A/B test auto-winner selection.
/// </summary>
public class ProductAffinityBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProductAffinityBackgroundService> _logger;
    private readonly TimeSpan _affinityInterval = TimeSpan.FromHours(24); // Daily affinity calculation
    private readonly TimeSpan _experimentCheckInterval = TimeSpan.FromHours(1); // Hourly experiment check

    public ProductAffinityBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ProductAffinityBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Product Affinity Background Service starting");

        // Initial delay to allow application to fully start
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        var lastAffinityRun = DateTime.MinValue;
        var lastExperimentCheck = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Affinity recalculation (daily)
                if (now - lastAffinityRun > _affinityInterval)
                {
                    _logger.LogInformation("Starting product affinity calculation cycle");
                    await ProcessAllShopsAffinitiesAsync(stoppingToken);
                    lastAffinityRun = now;
                    _logger.LogInformation("Completed product affinity calculation cycle");
                }

                // Experiment auto-winner check (hourly)
                if (now - lastExperimentCheck > _experimentCheckInterval)
                {
                    _logger.LogInformation("Starting experiment auto-winner check");
                    await CheckExperimentsAsync(stoppingToken);
                    lastExperimentCheck = now;
                    _logger.LogInformation("Completed experiment auto-winner check");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Product Affinity Background Service");
            }

            try
            {
                // Check every 10 minutes
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Product Affinity Background Service stopping");
    }

    private async Task ProcessAllShopsAffinitiesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get all active shops
        var activeShops = await db.Shops
            .Where(s => s.IsActive)
            .Select(s => s.Domain)
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Processing product affinities for {Count} shops", activeShops.Count);

        foreach (var shopDomain in activeShops)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await ProcessShopAffinitiesAsync(shopDomain, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing affinities for shop {Shop}", shopDomain);
            }
        }
    }

    private async Task ProcessShopAffinitiesAsync(string shopDomain, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested) return;

        using var scope = _serviceProvider.CreateScope();

        try
        {
            var affinityService = scope.ServiceProvider.GetRequiredService<IProductAffinityService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Get settings for lookback days
            var settings = await db.UpsellSettings
                .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain, stoppingToken);

            var lookbackDays = settings?.AffinityLookbackDays ?? 90;

            // Calculate affinities
            var affinitiesCalculated = await affinityService.CalculateAffinitiesAsync(shopDomain, lookbackDays);
            _logger.LogInformation("Shop {Shop}: Calculated {Count} product affinities", shopDomain, affinitiesCalculated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating affinities for shop {Shop}", shopDomain);
        }
    }

    private async Task CheckExperimentsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get all active shops with running experiments
        var shopsWithExperiments = await db.UpsellExperiments
            .Where(e => e.Status == "running")
            .Select(e => e.ShopDomain)
            .Distinct()
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Checking experiments for {Count} shops", shopsWithExperiments.Count);

        foreach (var shopDomain in shopsWithExperiments)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await ProcessShopExperimentsAsync(shopDomain, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking experiments for shop {Shop}", shopDomain);
            }
        }
    }

    private async Task ProcessShopExperimentsAsync(string shopDomain, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested) return;

        using var scope = _serviceProvider.CreateScope();

        try
        {
            var experimentService = scope.ServiceProvider.GetRequiredService<IUpsellExperimentService>();

            // Check for auto-winner selection
            var winnersSelected = await experimentService.ProcessAutoWinnerSelectionAsync(shopDomain);
            if (winnersSelected > 0)
            {
                _logger.LogInformation("Shop {Shop}: Auto-selected winners for {Count} experiments", shopDomain, winnersSelected);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing experiments for shop {Shop}", shopDomain);
        }
    }
}

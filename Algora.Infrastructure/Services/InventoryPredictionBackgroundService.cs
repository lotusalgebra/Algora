using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Background service that periodically updates inventory predictions
/// and generates alerts for all active shops.
/// </summary>
public class InventoryPredictionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventoryPredictionBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Run every 6 hours

    public InventoryPredictionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<InventoryPredictionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inventory Prediction Background Service starting");

        // Initial delay to allow application to fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting inventory prediction cycle");
                await ProcessAllShopsAsync(stoppingToken);
                _logger.LogInformation("Completed inventory prediction cycle");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Inventory Prediction Background Service");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when stopping
                break;
            }
        }

        _logger.LogInformation("Inventory Prediction Background Service stopping");
    }

    private async Task ProcessAllShopsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get all active shops
        var activeShops = await db.Shops
            .Where(s => s.IsActive)
            .Select(s => s.Domain)
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Processing inventory predictions for {Count} shops", activeShops.Count);

        foreach (var shopDomain in activeShops)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await ProcessShopAsync(shopDomain, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing shop {Shop}", shopDomain);
            }
        }
    }

    private async Task ProcessShopAsync(string shopDomain, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested) return;

        using var scope = _serviceProvider.CreateScope();

        try
        {
            var predictionService = scope.ServiceProvider.GetRequiredService<IInventoryPredictionService>();
            var alertService = scope.ServiceProvider.GetRequiredService<IInventoryAlertService>();

            // Calculate predictions (90 day lookback by default)
            var predictionsUpdated = await predictionService.CalculatePredictionsAsync(shopDomain);
            _logger.LogInformation("Shop {Shop}: Updated {Count} predictions", shopDomain, predictionsUpdated);

            if (stoppingToken.IsCancellationRequested) return;

            // Generate alerts based on predictions
            var alertsGenerated = await alertService.GenerateAlertsAsync(shopDomain);
            _logger.LogInformation("Shop {Shop}: Generated {Count} alerts", shopDomain, alertsGenerated);

            if (stoppingToken.IsCancellationRequested) return;

            // Send pending notifications
            var notificationsSent = await alertService.SendPendingNotificationsAsync(shopDomain);
            _logger.LogInformation("Shop {Shop}: Sent {Count} notifications", shopDomain, notificationsSent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inventory for shop {Shop}", shopDomain);
        }
    }
}

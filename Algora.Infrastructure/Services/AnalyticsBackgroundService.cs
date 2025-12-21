using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

public class AnalyticsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnalyticsBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);

    public AnalyticsBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AnalyticsBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analytics Background Service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAnalyticsUpdatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing analytics updates");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessAnalyticsUpdatesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

        var shops = await db.Shops
            .Where(s => !string.IsNullOrEmpty(s.OfflineAccessToken))
            .Select(s => s.Domain)
            .ToListAsync(stoppingToken);

        foreach (var shopDomain in shops)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                // Generate yesterday's snapshot if not exists
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                var snapshotExists = await db.AnalyticsSnapshots
                    .AnyAsync(s => s.ShopDomain == shopDomain && 
                                   s.SnapshotDate.Date == yesterday && 
                                   s.PeriodType == "daily", stoppingToken);

                if (!snapshotExists)
                {
                    _logger.LogInformation("Generating snapshot for {Shop} on {Date}", shopDomain, yesterday);
                    await analyticsService.GenerateSnapshotAsync(shopDomain, yesterday);
                }

                // Recalculate CLV periodically (once per day check)
                var lastClvUpdate = await db.CustomerLifetimeValues
                    .Where(c => c.ShopDomain == shopDomain)
                    .OrderByDescending(c => c.CalculatedAt)
                    .Select(c => c.CalculatedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                if (lastClvUpdate == default || (DateTime.UtcNow - lastClvUpdate).TotalDays >= 1)
                {
                    _logger.LogInformation("Recalculating CLV for {Shop}", shopDomain);
                    var count = await analyticsService.RecalculateClvAsync(shopDomain);
                    _logger.LogInformation("Updated CLV for {Count} customers in {Shop}", count, shopDomain);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing analytics for shop {Shop}", shopDomain);
            }
        }
    }
}

using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Operations;

/// <summary>
/// Background service for automated purchase order generation based on inventory predictions.
/// </summary>
public class PurchaseOrderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PurchaseOrderBackgroundService> _logger;

    // Run every 6 hours
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    public PurchaseOrderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PurchaseOrderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PurchaseOrderBackgroundService starting");

        // Wait a bit before first run to let the application start
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllShopsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PurchaseOrderBackgroundService");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        _logger.LogInformation("PurchaseOrderBackgroundService stopping");
    }

    private async Task ProcessAllShopsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get all active shops
        var shops = await db.Shops
            .Where(s => s.IsActive)
            .Select(s => s.Domain)
            .ToListAsync(ct);

        _logger.LogInformation("Processing auto purchase orders for {Count} shops", shops.Count);

        var totalCreated = 0;

        foreach (var shopDomain in shops)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var count = await ProcessShopAsync(scope.ServiceProvider, shopDomain, ct);
                totalCreated += count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process auto purchase orders for shop {ShopDomain}", shopDomain);
            }
        }

        if (totalCreated > 0)
        {
            _logger.LogInformation("Created {Count} auto purchase orders", totalCreated);
        }
    }

    private async Task<int> ProcessShopAsync(IServiceProvider sp, string shopDomain, CancellationToken ct)
    {
        var purchaseOrderService = sp.GetRequiredService<IPurchaseOrderService>();

        // Check if shop has auto-reorder enabled for any products
        var db = sp.GetRequiredService<AppDbContext>();
        var hasAutoReorder = await db.ProductInventoryThresholds
            .AnyAsync(t => t.ShopDomain == shopDomain && t.AutoReorderEnabled, ct);

        if (!hasAutoReorder)
            return 0;

        return await purchaseOrderService.ProcessAutoPurchaseOrdersAsync(shopDomain, ct);
    }
}

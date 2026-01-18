using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Advertising;

/// <summary>
/// Background service that periodically syncs Microsoft Advertising (Bing Ads) campaign data
/// for all connected shops.
/// </summary>
public class BingAdsSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BingAdsSyncBackgroundService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromHours(6);

    public BingAdsSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BingAdsSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bing Ads sync background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllConnectionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Bing Ads sync background service");
            }

            await Task.Delay(_syncInterval, stoppingToken);
        }

        _logger.LogInformation("Bing Ads sync background service stopped");
    }

    private async Task SyncAllConnectionsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bingAdsService = scope.ServiceProvider.GetRequiredService<IBingAdsService>();

        var connections = await db.BingAdsConnections
            .Where(c => c.IsConnected && c.AutoSyncEnabled)
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Syncing {Count} Bing Ads connections", connections.Count);

        foreach (var connection in connections)
        {
            if (stoppingToken.IsCancellationRequested) break;

            // Check if sync is due based on frequency setting
            if (connection.LastSyncedAt.HasValue)
            {
                var nextSyncDue = connection.LastSyncedAt.Value.AddHours(connection.SyncFrequencyHours);
                if (DateTime.UtcNow < nextSyncDue)
                {
                    _logger.LogDebug("Skipping Bing Ads sync for {ShopDomain}, next sync due at {NextSync}",
                        connection.ShopDomain, nextSyncDue);
                    continue;
                }
            }

            try
            {
                _logger.LogInformation("Syncing Bing Ads for {ShopDomain}", connection.ShopDomain);

                // Sync last 7 days of data
                var result = await bingAdsService.SyncCampaignsAsync(
                    connection.ShopDomain,
                    DateTime.UtcNow.AddDays(-7),
                    DateTime.UtcNow);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Bing Ads sync completed for {ShopDomain}: {Processed} campaigns, {Created} created, {Updated} updated",
                        connection.ShopDomain, result.CampaignsProcessed, result.RecordsCreated, result.RecordsUpdated);
                }
                else
                {
                    _logger.LogWarning("Bing Ads sync failed for {ShopDomain}: {Error}",
                        connection.ShopDomain, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync Bing Ads for {ShopDomain}", connection.ShopDomain);
            }

            // Small delay between shops to avoid rate limiting
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Advertising;

/// <summary>
/// Background service that periodically syncs Meta Ads data for all connected shops.
/// </summary>
public class MetaAdsSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetaAdsSyncBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    public MetaAdsSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MetaAdsSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Meta Ads Sync Background Service started");

        // Wait a bit before first run to let the app fully start
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllConnectionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Meta Ads sync background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Meta Ads Sync Background Service stopped");
    }

    private async Task SyncAllConnectionsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var metaAdsService = scope.ServiceProvider.GetRequiredService<IMetaAdsService>();

        // Find connections that need syncing
        var now = DateTime.UtcNow;
        var connections = await db.Set<MetaAdsConnection>()
            .Where(c => c.IsConnected && c.AutoSyncEnabled)
            .ToListAsync(stoppingToken);

        foreach (var connection in connections)
        {
            if (stoppingToken.IsCancellationRequested) break;

            // Check if it's time to sync based on frequency
            var nextSyncTime = (connection.LastSyncedAt ?? DateTime.MinValue)
                .AddHours(connection.SyncFrequencyHours);

            if (now < nextSyncTime)
            {
                _logger.LogDebug("Skipping sync for {ShopDomain}, next sync at {NextSync}",
                    connection.ShopDomain, nextSyncTime);
                continue;
            }

            // Check if token is expired
            if (connection.TokenExpiresAt.HasValue && connection.TokenExpiresAt < now)
            {
                _logger.LogWarning("Meta Ads token expired for {ShopDomain}", connection.ShopDomain);
                connection.LastSyncError = "Access token expired. Please reconnect.";
                connection.IsConnected = false;
                await db.SaveChangesAsync(stoppingToken);
                continue;
            }

            try
            {
                _logger.LogInformation("Starting Meta Ads sync for {ShopDomain}", connection.ShopDomain);

                // Sync last 7 days of data
                var result = await metaAdsService.SyncCampaignsAsync(
                    connection.ShopDomain,
                    now.AddDays(-7),
                    now);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Meta Ads sync completed for {ShopDomain}: {Campaigns} campaigns, {Created} created, {Updated} updated",
                        connection.ShopDomain, result.CampaignsProcessed, result.RecordsCreated, result.RecordsUpdated);
                }
                else
                {
                    _logger.LogWarning(
                        "Meta Ads sync failed for {ShopDomain}: {Error}",
                        connection.ShopDomain, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing Meta Ads for {ShopDomain}", connection.ShopDomain);
            }

            // Small delay between shops to avoid rate limiting
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

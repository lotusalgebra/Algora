using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Advertising;

/// <summary>
/// Background service that periodically syncs Snapchat Ads data for connected accounts.
/// </summary>
public class SnapchatAdsSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SnapchatAdsSyncBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    public SnapchatAdsSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SnapchatAdsSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Snapchat Ads Sync Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSyncJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Snapchat Ads sync background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Snapchat Ads Sync Background Service stopped");
    }

    private async Task ProcessSyncJobsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var snapchatAdsService = scope.ServiceProvider.GetRequiredService<ISnapchatAdsService>();

        // Get all active Snapchat Ads connections that need syncing
        var connections = await db.Set<SnapchatAdsConnection>()
            .Where(c => c.IsConnected && c.AutoSyncEnabled)
            .ToListAsync(stoppingToken);

        _logger.LogDebug("Found {Count} Snapchat Ads connections to check", connections.Count);

        foreach (var connection in connections)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                // Check if it's time to sync based on frequency
                var nextSyncTime = connection.LastSyncedAt?.AddHours(connection.SyncFrequencyHours)
                                   ?? DateTime.MinValue;

                if (DateTime.UtcNow < nextSyncTime)
                {
                    _logger.LogDebug("Skipping sync for {ShopDomain}, next sync at {NextSync}",
                        connection.ShopDomain, nextSyncTime);
                    continue;
                }

                _logger.LogInformation("Starting Snapchat Ads sync for {ShopDomain}", connection.ShopDomain);

                // Sync last 7 days of data
                var startDate = DateTime.UtcNow.AddDays(-7);
                var endDate = DateTime.UtcNow;

                var result = await snapchatAdsService.SyncCampaignsAsync(
                    connection.ShopDomain, startDate, endDate);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Snapchat Ads sync completed for {ShopDomain}: {Created} created, {Updated} updated",
                        connection.ShopDomain, result.RecordsCreated, result.RecordsUpdated);
                }
                else
                {
                    _logger.LogWarning(
                        "Snapchat Ads sync failed for {ShopDomain}: {Error}",
                        connection.ShopDomain, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing Snapchat Ads for {ShopDomain}", connection.ShopDomain);

                // Update error status
                connection.LastSyncError = ex.Message;
                await db.SaveChangesAsync(stoppingToken);
            }
        }
    }
}

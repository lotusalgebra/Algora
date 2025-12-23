using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.CustomerHub;

/// <summary>
/// Background service for loyalty program automation tasks.
/// Handles tier evaluation, points expiration, and birthday bonuses.
/// </summary>
public class LoyaltyBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoyaltyBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public LoyaltyBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<LoyaltyBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LoyaltyBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessLoyaltyTasksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoyaltyBackgroundService");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("LoyaltyBackgroundService stopped");
    }

    private async Task ProcessLoyaltyTasksAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var loyaltyService = scope.ServiceProvider.GetRequiredService<ILoyaltyService>();

        // Get all active loyalty programs
        var activePrograms = await db.LoyaltyPrograms
            .Where(p => p.IsActive)
            .Select(p => p.ShopDomain)
            .ToListAsync(stoppingToken);

        foreach (var shopDomain in activePrograms)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                // Process birthday bonuses (once per day check)
                if (DateTime.UtcNow.Hour == 0) // Run at midnight UTC
                {
                    await loyaltyService.ProcessBirthdayBonusAsync(shopDomain);
                }

                // Expire old points
                await loyaltyService.ExpirePointsAsync(shopDomain);

                // Evaluate tiers (could be done less frequently)
                if (DateTime.UtcNow.Minute < 5) // Run at the start of each hour
                {
                    await loyaltyService.EvaluateTiersAsync(shopDomain);
                }

                _logger.LogDebug("Processed loyalty tasks for {ShopDomain}", shopDomain);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing loyalty tasks for {ShopDomain}", shopDomain);
            }
        }
    }
}

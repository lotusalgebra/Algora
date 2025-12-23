using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Background service that processes pending automation steps and win-back triggers.
/// Runs every minute to check for enrollments that need to be processed.
/// </summary>
public class MarketingAutomationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketingAutomationBackgroundService> _logger;

    private static readonly TimeSpan ProcessingInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan WinbackCheckInterval = TimeSpan.FromHours(24);

    private DateTime _lastWinbackCheck = DateTime.MinValue;

    public MarketingAutomationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MarketingAutomationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Marketing Automation Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllShopsAsync(stoppingToken);

                // Check if it's time to run win-back detection
                if (DateTime.UtcNow - _lastWinbackCheck > WinbackCheckInterval)
                {
                    await ProcessWinbackForAllShopsAsync(stoppingToken);
                    _lastWinbackCheck = DateTime.UtcNow;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in Marketing Automation Background Service");
            }

            await Task.Delay(ProcessingInterval, stoppingToken);
        }

        _logger.LogInformation("Marketing Automation Background Service stopped");
    }

    private async Task ProcessAllShopsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var automationService = scope.ServiceProvider.GetRequiredService<IMarketingAutomationService>();

        // Get all shop domains that have active automation enrollments
        var shopDomains = await db.EmailAutomationEnrollments
            .Include(e => e.Automation)
            .Where(e => e.Status == "active" && e.NextStepAt <= DateTime.UtcNow)
            .Select(e => e.Automation.ShopDomain)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var shopDomain in shopDomains)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var processed = await automationService.ProcessPendingStepsAsync(shopDomain, cancellationToken);

                if (processed > 0)
                {
                    _logger.LogInformation("Processed {Count} automation steps for {ShopDomain}",
                        processed, shopDomain);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing automation steps for {ShopDomain}", shopDomain);
            }
        }
    }

    private async Task ProcessWinbackForAllShopsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting daily win-back campaign processing");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var automationService = scope.ServiceProvider.GetRequiredService<IMarketingAutomationService>();

        // Get all shop domains that have active win-back rules
        var shopDomains = await db.WinbackRules
            .Where(r => r.IsActive)
            .Select(r => r.ShopDomain)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var shopDomain in shopDomains)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var enrolled = await automationService.ProcessWinbackTriggersAsync(shopDomain, cancellationToken);

                if (enrolled > 0)
                {
                    _logger.LogInformation("Enrolled {Count} customers in win-back campaigns for {ShopDomain}",
                        enrolled, shopDomain);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing win-back triggers for {ShopDomain}", shopDomain);
            }
        }

        _logger.LogInformation("Completed daily win-back campaign processing");
    }
}

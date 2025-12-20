using Algora.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Background service that processes scheduled review request emails
/// </summary>
public class ReviewEmailBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReviewEmailBackgroundService> _logger;

    private const int PollingIntervalSeconds = 60; // Check every minute

    public ReviewEmailBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReviewEmailBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Review Email Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled review emails");
            }

            await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Review Email Background Service stopped");
    }

    private async Task ProcessScheduledEmailsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IReviewEmailService>();

        await emailService.ProcessScheduledEmailsAsync(stoppingToken);
    }
}

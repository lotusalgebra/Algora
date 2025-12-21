using Algora.Application.Interfaces;
using Algora.Infrastructure.Services.Communication;
using Microsoft.Extensions.DependencyInjection;

namespace Algora.Infrastructure.Extensions;

public static class CommunicationServiceExtensions
{
    public static IServiceCollection AddCommunicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailMarketingService, EmailMarketingService>();
        // services.AddScoped<IWhatsAppService, WhatsAppService>();
        // services.AddScoped<ISmsService, SmsService>();
        // services.AddScoped<INotificationService, NotificationService>();
        // services.AddScoped<ICommunicationSettingsService, CommunicationSettingsService>();

        return services;
    }
}


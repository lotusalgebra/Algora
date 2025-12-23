using Algora.Application.DTOs.CustomerHub;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for Facebook Messenger and Instagram DM integration.
/// </summary>
public interface ISocialMediaService
{
    // Settings
    Task<SocialMediaSettingsDto?> GetSettingsAsync(string shopDomain);
    Task<SocialMediaSettingsDto> SaveSettingsAsync(SaveSocialMediaSettingsDto dto);
    Task<bool> ValidateSettingsAsync(string shopDomain);

    // Facebook Messenger
    Task<IEnumerable<FacebookMessageDto>> GetFacebookMessagesAsync(string shopDomain, string? senderId = null, int? limit = null);
    Task<FacebookMessageDto> SendFacebookMessageAsync(SendFacebookMessageDto dto);
    Task HandleFacebookWebhookAsync(string payload, string signature);
    Task<string?> GetFacebookSenderNameAsync(string senderId, string accessToken);

    // Instagram DM
    Task<IEnumerable<InstagramMessageDto>> GetInstagramMessagesAsync(string shopDomain, string? senderId = null, int? limit = null);
    Task<InstagramMessageDto> SendInstagramMessageAsync(SendInstagramMessageDto dto);
    Task HandleInstagramWebhookAsync(string payload, string signature);

    // Webhook verification
    Task<string?> VerifyWebhookAsync(string shopDomain, string mode, string token, string challenge);

    // Message sync
    Task<int> SyncFacebookMessagesAsync(string shopDomain);
    Task<int> SyncInstagramMessagesAsync(string shopDomain);
}

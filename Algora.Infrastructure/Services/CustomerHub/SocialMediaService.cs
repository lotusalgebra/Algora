using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.CustomerHub;

/// <summary>
/// Service for Facebook Messenger and Instagram DM integration via Meta Graph API.
/// </summary>
public class SocialMediaService : ISocialMediaService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SocialMediaService> _logger;
    private const string GraphApiBaseUrl = "https://graph.facebook.com/v18.0";

    public SocialMediaService(
        AppDbContext db,
        HttpClient httpClient,
        ILogger<SocialMediaService> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SocialMediaSettingsDto?> GetSettingsAsync(string shopDomain)
    {
        var settings = await _db.SocialMediaSettings.FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);
        return settings != null ? MapSettingsToDto(settings) : null;
    }

    public async Task<SocialMediaSettingsDto> SaveSettingsAsync(SaveSocialMediaSettingsDto dto)
    {
        var settings = await _db.SocialMediaSettings.FirstOrDefaultAsync(s => s.ShopDomain == dto.ShopDomain);

        if (settings == null)
        {
            settings = new SocialMediaSettings
            {
                ShopDomain = dto.ShopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _db.SocialMediaSettings.Add(settings);
        }

        if (dto.FacebookPageId != null) settings.FacebookPageId = dto.FacebookPageId;
        if (dto.FacebookPageAccessToken != null) settings.FacebookPageAccessToken = dto.FacebookPageAccessToken;
        if (dto.InstagramAccountId != null) settings.InstagramAccountId = dto.InstagramAccountId;
        if (dto.MetaAppId != null) settings.MetaAppId = dto.MetaAppId;
        if (dto.MetaAppSecret != null) settings.MetaAppSecret = dto.MetaAppSecret;
        if (dto.WebhookVerifyToken != null) settings.WebhookVerifyToken = dto.WebhookVerifyToken;
        settings.IsActive = dto.IsActive;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapSettingsToDto(settings);
    }

    public async Task<bool> ValidateSettingsAsync(string shopDomain)
    {
        var settings = await _db.SocialMediaSettings.FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);
        if (settings == null || string.IsNullOrEmpty(settings.FacebookPageAccessToken))
            return false;

        try
        {
            var response = await _httpClient.GetAsync(
                $"{GraphApiBaseUrl}/me?access_token={settings.FacebookPageAccessToken}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating social media settings for {ShopDomain}", shopDomain);
            return false;
        }
    }

    public async Task<IEnumerable<FacebookMessageDto>> GetFacebookMessagesAsync(string shopDomain, string? senderId = null, int? limit = null)
    {
        var query = _db.FacebookMessages.Where(m => m.ShopDomain == shopDomain);

        if (!string.IsNullOrEmpty(senderId))
            query = query.Where(m => m.SenderId == senderId);

        query = query.OrderByDescending(m => m.SentAt).Take(limit ?? 100);

        var messages = await query.ToListAsync();
        return messages.Select(MapFacebookMessageToDto);
    }

    public async Task<FacebookMessageDto> SendFacebookMessageAsync(SendFacebookMessageDto dto)
    {
        var settings = await _db.SocialMediaSettings.FirstOrDefaultAsync(s => s.ShopDomain == dto.ShopDomain)
            ?? throw new InvalidOperationException($"Social media settings not found for {dto.ShopDomain}");

        object payload;
        if (dto.MessageType == "text")
        {
            payload = new
            {
                recipient = new { id = dto.RecipientId },
                message = new { text = dto.Content }
            };
        }
        else
        {
            payload = new
            {
                recipient = new { id = dto.RecipientId },
                message = new { attachment = new { type = dto.MessageType, payload = new { url = dto.MediaUrl } } }
            };
        }

        var response = await _httpClient.PostAsJsonAsync(
            $"{GraphApiBaseUrl}/{settings.FacebookPageId}/messages?access_token={settings.FacebookPageAccessToken}",
            payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to send Facebook message: {Error}", error);
            throw new InvalidOperationException($"Failed to send Facebook message: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var messageId = result.GetProperty("message_id").GetString()!;

        var message = new FacebookMessage
        {
            ShopDomain = dto.ShopDomain,
            FacebookMessageId = messageId,
            SenderId = settings.FacebookPageId!,
            RecipientId = dto.RecipientId,
            Direction = "outbound",
            MessageType = dto.MessageType,
            Content = dto.Content,
            MediaUrl = dto.MediaUrl,
            Status = "sent",
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.FacebookMessages.Add(message);
        await _db.SaveChangesAsync();

        return MapFacebookMessageToDto(message);
    }

    public async Task HandleFacebookWebhookAsync(string payload, string signature)
    {
        // Verify signature
        var settings = await _db.SocialMediaSettings.FirstOrDefaultAsync(s => s.IsActive);
        if (settings == null || string.IsNullOrEmpty(settings.MetaAppSecret))
        {
            _logger.LogWarning("No active social media settings found for webhook");
            return;
        }

        if (!VerifySignature(payload, signature, settings.MetaAppSecret))
        {
            _logger.LogWarning("Invalid Facebook webhook signature");
            return;
        }

        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(payload);
            var entries = data.GetProperty("entry");

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("messaging", out var messaging)) continue;

                foreach (var msg in messaging.EnumerateArray())
                {
                    await ProcessFacebookMessageEventAsync(settings.ShopDomain, msg);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Facebook webhook");
        }
    }

    public async Task<string?> GetFacebookSenderNameAsync(string senderId, string accessToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{GraphApiBaseUrl}/{senderId}?fields=first_name,last_name&access_token={accessToken}");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<JsonElement>();
                var firstName = data.TryGetProperty("first_name", out var fn) ? fn.GetString() : "";
                var lastName = data.TryGetProperty("last_name", out var ln) ? ln.GetString() : "";
                return $"{firstName} {lastName}".Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get Facebook sender name for {SenderId}", senderId);
        }
        return null;
    }

    public async Task<IEnumerable<InstagramMessageDto>> GetInstagramMessagesAsync(string shopDomain, string? senderId = null, int? limit = null)
    {
        var query = _db.InstagramMessages.Where(m => m.ShopDomain == shopDomain);

        if (!string.IsNullOrEmpty(senderId))
            query = query.Where(m => m.SenderId == senderId);

        query = query.OrderByDescending(m => m.SentAt).Take(limit ?? 100);

        var messages = await query.ToListAsync();
        return messages.Select(MapInstagramMessageToDto);
    }

    public async Task<InstagramMessageDto> SendInstagramMessageAsync(SendInstagramMessageDto dto)
    {
        var settings = await _db.SocialMediaSettings.FirstOrDefaultAsync(s => s.ShopDomain == dto.ShopDomain)
            ?? throw new InvalidOperationException($"Social media settings not found for {dto.ShopDomain}");

        object igPayload;
        if (dto.MessageType == "text")
        {
            igPayload = new
            {
                recipient = new { id = dto.RecipientId },
                message = new { text = dto.Content }
            };
        }
        else
        {
            igPayload = new
            {
                recipient = new { id = dto.RecipientId },
                message = new { attachment = new { type = dto.MessageType, payload = new { url = dto.MediaUrl } } }
            };
        }

        var response = await _httpClient.PostAsJsonAsync(
            $"{GraphApiBaseUrl}/{settings.InstagramAccountId}/messages?access_token={settings.FacebookPageAccessToken}",
            igPayload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to send Instagram message: {Error}", error);
            throw new InvalidOperationException($"Failed to send Instagram message: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var messageId = result.GetProperty("message_id").GetString()!;

        var message = new InstagramMessage
        {
            ShopDomain = dto.ShopDomain,
            InstagramMessageId = messageId,
            SenderId = settings.InstagramAccountId!,
            RecipientId = dto.RecipientId,
            Direction = "outbound",
            MessageType = dto.MessageType,
            Content = dto.Content,
            MediaUrl = dto.MediaUrl,
            Status = "sent",
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.InstagramMessages.Add(message);
        await _db.SaveChangesAsync();

        return MapInstagramMessageToDto(message);
    }

    public async Task HandleInstagramWebhookAsync(string payload, string signature)
    {
        // Instagram uses same webhook structure as Facebook Messenger
        await HandleFacebookWebhookAsync(payload, signature);
    }

    public Task<string?> VerifyWebhookAsync(string shopDomain, string mode, string token, string challenge)
    {
        var settings = _db.SocialMediaSettings.FirstOrDefault(s => s.ShopDomain == shopDomain);

        if (settings != null &&
            mode == "subscribe" &&
            token == settings.WebhookVerifyToken)
        {
            _logger.LogInformation("Webhook verified for {ShopDomain}", shopDomain);
            return Task.FromResult<string?>(challenge);
        }

        _logger.LogWarning("Webhook verification failed for {ShopDomain}", shopDomain);
        return Task.FromResult<string?>(null);
    }

    public async Task<int> SyncFacebookMessagesAsync(string shopDomain)
    {
        // TODO: Implement conversation sync from Facebook Graph API
        _logger.LogInformation("Syncing Facebook messages for {ShopDomain}", shopDomain);
        return 0;
    }

    public async Task<int> SyncInstagramMessagesAsync(string shopDomain)
    {
        // TODO: Implement conversation sync from Instagram Graph API
        _logger.LogInformation("Syncing Instagram messages for {ShopDomain}", shopDomain);
        return 0;
    }

    private async Task ProcessFacebookMessageEventAsync(string shopDomain, JsonElement msg)
    {
        var sender = msg.GetProperty("sender").GetProperty("id").GetString()!;
        var recipient = msg.GetProperty("recipient").GetProperty("id").GetString()!;
        var timestamp = msg.GetProperty("timestamp").GetInt64();

        if (msg.TryGetProperty("message", out var messageData))
        {
            var mid = messageData.GetProperty("mid").GetString()!;

            // Check if this is an echo (our own message)
            if (messageData.TryGetProperty("is_echo", out var isEcho) && isEcho.GetBoolean())
                return;

            var text = messageData.TryGetProperty("text", out var t) ? t.GetString() : null;
            string? mediaUrl = null;
            var messageType = "text";

            if (messageData.TryGetProperty("attachments", out var attachments))
            {
                var attachment = attachments.EnumerateArray().FirstOrDefault();
                if (attachment.ValueKind != JsonValueKind.Undefined)
                {
                    messageType = attachment.GetProperty("type").GetString() ?? "file";
                    if (attachment.TryGetProperty("payload", out var payload) &&
                        payload.TryGetProperty("url", out var url))
                    {
                        mediaUrl = url.GetString();
                    }
                }
            }

            var message = new FacebookMessage
            {
                ShopDomain = shopDomain,
                FacebookMessageId = mid,
                SenderId = sender,
                RecipientId = recipient,
                Direction = "inbound",
                MessageType = messageType,
                Content = text,
                MediaUrl = mediaUrl,
                Status = "delivered",
                SentAt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime,
                CreatedAt = DateTime.UtcNow
            };

            _db.FacebookMessages.Add(message);
            await _db.SaveChangesAsync();
        }
        else if (msg.TryGetProperty("delivery", out var delivery))
        {
            // Handle delivery confirmation
            if (delivery.TryGetProperty("mids", out var mids))
            {
                foreach (var mid in mids.EnumerateArray())
                {
                    var existing = await _db.FacebookMessages.FirstOrDefaultAsync(
                        m => m.FacebookMessageId == mid.GetString());
                    if (existing != null)
                    {
                        existing.DeliveredAt = DateTime.UtcNow;
                        existing.Status = "delivered";
                    }
                }
                await _db.SaveChangesAsync();
            }
        }
        else if (msg.TryGetProperty("read", out var read))
        {
            // Handle read confirmation
            var watermark = read.GetProperty("watermark").GetInt64();
            var readTime = DateTimeOffset.FromUnixTimeMilliseconds(watermark).UtcDateTime;

            var unreadMessages = await _db.FacebookMessages
                .Where(m => m.ShopDomain == shopDomain && m.SenderId == recipient && m.ReadAt == null && m.SentAt <= readTime)
                .ToListAsync();

            foreach (var m in unreadMessages)
            {
                m.ReadAt = DateTime.UtcNow;
                m.Status = "read";
            }
            await _db.SaveChangesAsync();
        }
    }

    private static bool VerifySignature(string payload, string signature, string appSecret)
    {
        if (string.IsNullOrEmpty(signature) || !signature.StartsWith("sha256="))
            return false;

        var expectedSignature = signature[7..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var actualSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

        return expectedSignature.Equals(actualSignature, StringComparison.OrdinalIgnoreCase);
    }

    private static SocialMediaSettingsDto MapSettingsToDto(SocialMediaSettings s) => new(
        s.Id,
        s.ShopDomain,
        s.FacebookPageId,
        !string.IsNullOrEmpty(s.FacebookPageAccessToken),
        s.InstagramAccountId,
        s.MetaAppId,
        !string.IsNullOrEmpty(s.MetaAppSecret),
        s.WebhookVerifyToken,
        s.IsActive,
        s.CreatedAt,
        s.UpdatedAt
    );

    private static FacebookMessageDto MapFacebookMessageToDto(FacebookMessage m) => new(
        m.Id, m.ShopDomain, m.FacebookMessageId, m.SenderId, m.SenderName, m.RecipientId,
        m.Direction, m.MessageType, m.Content, m.MediaUrl, m.Status,
        m.SentAt, m.DeliveredAt, m.ReadAt, m.CreatedAt
    );

    private static InstagramMessageDto MapInstagramMessageToDto(InstagramMessage m) => new(
        m.Id, m.ShopDomain, m.InstagramMessageId, m.SenderId, m.SenderUsername, m.RecipientId,
        m.Direction, m.MessageType, m.Content, m.MediaUrl, m.StoryId, m.Status,
        m.SentAt, m.DeliveredAt, m.ReadAt, m.CreatedAt
    );
}

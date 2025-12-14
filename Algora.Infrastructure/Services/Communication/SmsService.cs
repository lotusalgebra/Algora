using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Algora.Infrastructure.Services.Communication;

/// <summary>
/// Complete SMS service implementation supporting multiple providers.
/// Settings are loaded per-shop from the database.
/// </summary>
public partial class SmsService : ISmsService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly SmsOptions _options;
    private readonly ILogger<SmsService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public SmsService(
        AppDbContext db,
        IHttpClientFactory httpFactory,
        IOptions<SmsOptions> options,
        ILogger<SmsService> logger)
    {
        _db = db;
        _http = httpFactory.CreateClient();
        _options = options.Value;
        _logger = logger;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        switch (_options.Provider.ToLowerInvariant())
        {
            case "twilio":
                var credentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", credentials);
                break;

            case "nexmo":
            case "vonage":
                // Nexmo uses API key in request body
                break;

            case "messagebird":
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("AccessKey", _options.AuthToken);
                break;
        }
    }

    #region Templates

    public async Task<SmsTemplateDto?> GetTemplateAsync(int templateId)
    {
        var template = await _db.SmsTemplates.FindAsync(templateId);
        return template is null ? null : MapToDto(template);
    }

    public async Task<IEnumerable<SmsTemplateDto>> GetTemplatesAsync(string shopDomain)
    {
        return await _db.SmsTemplates.AsNoTracking()
            .Where(t => t.ShopDomain == shopDomain)
            .OrderBy(t => t.Name)
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<SmsTemplateDto> CreateTemplateAsync(string shopDomain, CreateSmsTemplateDto dto)
    {
        var template = new SmsTemplate
        {
            ShopDomain = shopDomain,
            Name = dto.Name,
            TemplateType = dto.TemplateType,
            Body = dto.Body,
            IsActive = true
        };

        _db.SmsTemplates.Add(template);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created SMS template {Name} for {Shop}", dto.Name, shopDomain);
        return MapToDto(template);
    }

    public async Task<SmsTemplateDto> UpdateTemplateAsync(int templateId, UpdateSmsTemplateDto dto)
    {
        var template = await _db.SmsTemplates.FindAsync(templateId)
            ?? throw new InvalidOperationException($"Template {templateId} not found");

        if (dto.Name is not null) template.Name = dto.Name;
        if (dto.Body is not null) template.Body = dto.Body;
        if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(int templateId)
    {
        var template = await _db.SmsTemplates.FindAsync(templateId);
        if (template is null) return false;

        _db.SmsTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Messages

    public async Task<SmsMessageDto> SendMessageAsync(string shopDomain, SendSmsMessageDto dto)
    {
        var phoneNumber = NormalizePhoneNumber(dto.PhoneNumber);
        var segmentCount = CalculateSegmentCount(dto.Body);

        var message = new SmsMessage
        {
            ShopDomain = shopDomain,
            CustomerId = dto.CustomerId,
            OrderId = dto.OrderId,
            PhoneNumber = phoneNumber,
            Body = dto.Body,
            Status = "pending",
            SegmentCount = segmentCount
        };

        _db.SmsMessages.Add(message);
        await _db.SaveChangesAsync();

        try
        {
            var result = await SendViaSmsProviderAsync(phoneNumber, dto.Body);

            message.ExternalMessageId = result.MessageId;
            message.Status = result.Success ? "sent" : "failed";
            message.SentAt = result.Success ? DateTime.UtcNow : null;
            message.Cost = result.Cost;
            message.ErrorCode = result.ErrorCode;
            message.ErrorMessage = result.ErrorMessage;

            if (result.Success)
            {
                _logger.LogInformation("Sent SMS to {Phone}, MessageId: {MessageId}", phoneNumber, result.MessageId);
            }
            else
            {
                _logger.LogWarning("Failed to send SMS to {Phone}: {Error}", phoneNumber, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            message.Status = "failed";
            message.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error sending SMS to {Phone}", phoneNumber);
        }

        await _db.SaveChangesAsync();
        return MapToDto(message);
    }

    public async Task<SmsMessageDto?> GetMessageAsync(int messageId)
    {
        var message = await _db.SmsMessages.FindAsync(messageId);
        return message is null ? null : MapToDto(message);
    }

    public async Task<PaginatedResult<SmsMessageDto>> GetMessagesAsync(string shopDomain, int page = 1, int pageSize = 50)
    {
        var query = _db.SmsMessages.AsNoTracking().Where(m => m.ShopDomain == shopDomain);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PaginatedResult<SmsMessageDto>.Create(items.Select(MapToDto), total, page, pageSize);
    }

    public async Task<int> SendBulkMessagesAsync(string shopDomain, SendBulkSmsDto dto)
    {
        var phoneNumbers = dto.PhoneNumbers.ToList();

        // If segment ID is provided, get phone numbers from segment
        if (dto.SegmentId.HasValue)
        {
            var segmentPhones = await GetSegmentPhoneNumbersAsync(shopDomain, dto.SegmentId.Value);
            phoneNumbers.AddRange(segmentPhones);
        }

        // Deduplicate
        phoneNumbers = phoneNumbers.Distinct().ToList();

        var successCount = 0;
        var delayMs = 1000 / _options.RateLimitPerSecond;

        foreach (var phone in phoneNumbers)
        {
            try
            {
                var result = await SendMessageAsync(shopDomain, new SendSmsMessageDto
                {
                    PhoneNumber = phone,
                    Body = dto.Body
                });

                if (result.Status == "sent") successCount++;

                // Rate limiting
                await Task.Delay(delayMs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send bulk SMS to {Phone}", phone);
            }
        }

        _logger.LogInformation("Bulk SMS completed: {Success}/{Total} sent", successCount, phoneNumbers.Count);
        return successCount;
    }

    #endregion

    #region Webhooks

    public async Task HandleDeliveryStatusAsync(string shopDomain, SmsDeliveryStatusPayload payload)
    {
        var message = await _db.SmsMessages
            .FirstOrDefaultAsync(m => m.ExternalMessageId == payload.MessageId);

        if (message is null)
        {
            _logger.LogWarning("SMS message not found for delivery status: {MessageId}", payload.MessageId);
            return;
        }

        var previousStatus = message.Status;
        message.Status = NormalizeStatus(payload.Status);

        if (message.Status == "delivered")
        {
            message.DeliveredAt = payload.Timestamp;
        }
        else if (message.Status == "failed")
        {
            message.ErrorCode = payload.ErrorCode;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated SMS {Id} status: {OldStatus} -> {NewStatus}",
            message.Id, previousStatus, message.Status);
    }

    #endregion

    #region Provider Integration

    private async Task<SmsProviderResult> SendViaSmsProviderAsync(string phoneNumber, string body)
    {
        return _options.Provider.ToLowerInvariant() switch
        {
            "twilio" => await SendViaTwilioAsync(phoneNumber, body),
            "nexmo" or "vonage" => await SendViaNexmoAsync(phoneNumber, body),
            "messagebird" => await SendViaMessageBirdAsync(phoneNumber, body),
            _ => throw new InvalidOperationException($"Unknown SMS provider: {_options.Provider}")
        };
    }

    private async Task<SmsProviderResult> SendViaTwilioAsync(string phoneNumber, string body)
    {
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json";

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = phoneNumber,
            ["From"] = _options.FromNumber,
            ["Body"] = body,
            ["StatusCallback"] = _options.WebhookUrl ?? ""
        });

        var response = await _http.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new SmsProviderResult
            {
                Success = true,
                MessageId = root.GetProperty("sid").GetString(),
                Cost = root.TryGetProperty("price", out var price) && price.ValueKind != JsonValueKind.Null
                    ? decimal.Parse(price.GetString()!) : null
            };
        }

        using var errorDoc = JsonDocument.Parse(json);
        return new SmsProviderResult
        {
            Success = false,
            ErrorCode = errorDoc.RootElement.TryGetProperty("code", out var code) ? code.GetInt32().ToString() : null,
            ErrorMessage = errorDoc.RootElement.TryGetProperty("message", out var msg) ? msg.GetString() : json
        };
    }

    private async Task<SmsProviderResult> SendViaNexmoAsync(string phoneNumber, string body)
    {
        var url = "https://rest.nexmo.com/sms/json";

        var payload = new
        {
            api_key = _options.AccountSid,
            api_secret = _options.AuthToken,
            from = _options.FromNumber,
            to = phoneNumber.TrimStart('+'),
            text = body
        };

        var response = await _http.PostAsJsonAsync(url, payload);
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var messages = doc.RootElement.GetProperty("messages");
        var firstMessage = messages.EnumerateArray().FirstOrDefault();

        if (firstMessage.ValueKind != JsonValueKind.Undefined)
        {
            var status = firstMessage.GetProperty("status").GetString();
            if (status == "0")
            {
                return new SmsProviderResult
                {
                    Success = true,
                    MessageId = firstMessage.GetProperty("message-id").GetString(),
                    Cost = firstMessage.TryGetProperty("message-price", out var price)
                        ? decimal.Parse(price.GetString()!) : null
                };
            }

            return new SmsProviderResult
            {
                Success = false,
                ErrorCode = status,
                ErrorMessage = firstMessage.TryGetProperty("error-text", out var err) ? err.GetString() : null
            };
        }

        return new SmsProviderResult { Success = false, ErrorMessage = "No response from Nexmo" };
    }

    private async Task<SmsProviderResult> SendViaMessageBirdAsync(string phoneNumber, string body)
    {
        var url = "https://rest.messagebird.com/messages";

        var payload = new
        {
            originator = _options.FromNumber,
            recipients = new[] { phoneNumber.TrimStart('+') },
            body
        };

        var response = await _http.PostAsJsonAsync(url, payload, JsonOptions);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(json);
            return new SmsProviderResult
            {
                Success = true,
                MessageId = doc.RootElement.GetProperty("id").GetString()
            };
        }

        using var errorDoc = JsonDocument.Parse(json);
        var errors = errorDoc.RootElement.TryGetProperty("errors", out var errArray)
            ? errArray.EnumerateArray().FirstOrDefault()
            : default;

        return new SmsProviderResult
        {
            Success = false,
            ErrorCode = errors.ValueKind != JsonValueKind.Undefined && errors.TryGetProperty("code", out var code)
                ? code.GetInt32().ToString() : null,
            ErrorMessage = errors.ValueKind != JsonValueKind.Undefined && errors.TryGetProperty("description", out var desc)
                ? desc.GetString() : json
        };
    }

    private record SmsProviderResult
    {
        public bool Success { get; init; }
        public string? MessageId { get; init; }
        public decimal? Cost { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
    }

    #endregion

    #region Helpers

    private async Task<List<string>> GetSegmentPhoneNumbersAsync(string shopDomain, int segmentId)
    {
        var phones = new List<string>();

        var members = await _db.CustomerSegmentMembers
            .Where(m => m.SegmentId == segmentId)
            .Select(m => new { m.CustomerId, m.SubscriberId })
            .ToListAsync();

        foreach (var member in members)
        {
            if (member.CustomerId.HasValue)
            {
                var customer = await _db.Customers.FindAsync(member.CustomerId.Value);
                if (!string.IsNullOrEmpty(customer?.Phone))
                    phones.Add(customer.Phone);
            }
            else if (member.SubscriberId.HasValue)
            {
                var subscriber = await _db.EmailSubscribers.FindAsync(member.SubscriberId.Value);
                if (!string.IsNullOrEmpty(subscriber?.Phone) && subscriber.SmsOptIn)
                    phones.Add(subscriber.Phone);
            }
        }

        return phones;
    }

    private static string NormalizePhoneNumber(string phone)
    {
        // Remove all non-digit characters except leading +
        var normalized = PhoneRegex().Replace(phone, "");
        // Ensure it starts with + for international format
        if (!normalized.StartsWith('+'))
            normalized = "+" + normalized;
        return normalized;
    }

    private static int CalculateSegmentCount(string body)
    {
        // GSM-7 encoding: 160 chars per segment, 153 for concatenated
        // UCS-2 encoding: 70 chars per segment, 67 for concatenated
        var isGsm7 = body.All(c => Gsm7Chars.Contains(c));
        var maxSingleSegment = isGsm7 ? 160 : 70;
        var maxConcatSegment = isGsm7 ? 153 : 67;

        if (body.Length <= maxSingleSegment) return 1;
        return (int)Math.Ceiling((double)body.Length / maxConcatSegment);
    }

    private static string NormalizeStatus(string providerStatus)
    {
        return providerStatus.ToLowerInvariant() switch
        {
            "delivered" or "delivery_success" => "delivered",
            "sent" or "accepted" or "queued" => "sent",
            "failed" or "undelivered" or "rejected" or "delivery_failed" => "failed",
            _ => providerStatus.ToLowerInvariant()
        };
    }

    [GeneratedRegex(@"[^\d+]")]
    private static partial Regex PhoneRegex();

    // GSM-7 character set
    private static readonly HashSet<char> Gsm7Chars =
    [
        '@', '£', '$', '¥', 'è', 'é', 'ù', 'ì', 'ò', 'Ç', '\n', 'Ø', 'ø', '\r', 'Å', 'å',
        'Δ', '_', 'Φ', 'Γ', 'Λ', 'Ω', 'Π', 'Ψ', 'Σ', 'Θ', 'Ξ', ' ', 'Æ', 'æ', 'ß', 'É',
        ' ', '!', '"', '#', '¤', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?',
        '¡', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
        'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'Ä', 'Ö', 'Ñ', 'Ü', '§',
        '¿', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
        'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'ä', 'ö', 'ñ', 'ü', 'à'
    ];

    #endregion

    #region Mappers

    private static SmsTemplateDto MapToDto(SmsTemplate t) => new()
    {
        Id = t.Id,
        ShopDomain = t.ShopDomain,
        Name = t.Name,
        TemplateType = t.TemplateType,
        Body = t.Body,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt
    };

    private static SmsMessageDto MapToDto(SmsMessage m) => new()
    {
        Id = m.Id,
        ShopDomain = m.ShopDomain,
        ExternalMessageId = m.ExternalMessageId,
        CustomerId = m.CustomerId,
        PhoneNumber = m.PhoneNumber,
        Body = m.Body,
        Status = m.Status,
        SegmentCount = m.SegmentCount,
        Cost = m.Cost,
        SentAt = m.SentAt,
        DeliveredAt = m.DeliveredAt,
        CreatedAt = m.CreatedAt
    };

    #endregion
}
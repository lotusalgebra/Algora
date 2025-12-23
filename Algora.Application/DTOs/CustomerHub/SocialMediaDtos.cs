namespace Algora.Application.DTOs.CustomerHub;

// ==================== Social Media Settings DTOs ====================

public record SocialMediaSettingsDto(
    int Id,
    string ShopDomain,
    string? FacebookPageId,
    bool HasFacebookToken,
    string? InstagramAccountId,
    string? MetaAppId,
    bool HasMetaAppSecret,
    string? WebhookVerifyToken,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record SaveSocialMediaSettingsDto(
    string ShopDomain,
    string? FacebookPageId = null,
    string? FacebookPageAccessToken = null,
    string? InstagramAccountId = null,
    string? MetaAppId = null,
    string? MetaAppSecret = null,
    string? WebhookVerifyToken = null,
    bool IsActive = false
);

// ==================== Facebook Message DTOs ====================

public record FacebookMessageDto(
    int Id,
    string ShopDomain,
    string FacebookMessageId,
    string SenderId,
    string? SenderName,
    string RecipientId,
    string Direction,
    string MessageType,
    string? Content,
    string? MediaUrl,
    string Status,
    DateTime SentAt,
    DateTime? DeliveredAt,
    DateTime? ReadAt,
    DateTime CreatedAt
);

public record SendFacebookMessageDto(
    string ShopDomain,
    string RecipientId,
    string Content,
    string MessageType = "text",
    string? MediaUrl = null
);

// ==================== Instagram Message DTOs ====================

public record InstagramMessageDto(
    int Id,
    string ShopDomain,
    string InstagramMessageId,
    string SenderId,
    string? SenderUsername,
    string RecipientId,
    string Direction,
    string MessageType,
    string? Content,
    string? MediaUrl,
    string? StoryId,
    string Status,
    DateTime SentAt,
    DateTime? DeliveredAt,
    DateTime? ReadAt,
    DateTime CreatedAt
);

public record SendInstagramMessageDto(
    string ShopDomain,
    string RecipientId,
    string Content,
    string MessageType = "text",
    string? MediaUrl = null
);

// ==================== Webhook DTOs ====================

public record MetaWebhookEntryDto(
    string Object,
    IEnumerable<MetaWebhookEventDto> Entry
);

public record MetaWebhookEventDto(
    string Id,
    long Time,
    IEnumerable<MetaMessagingEventDto>? Messaging
);

public record MetaMessagingEventDto(
    MetaParticipantDto Sender,
    MetaParticipantDto Recipient,
    long Timestamp,
    MetaMessageDto? Message,
    MetaDeliveryDto? Delivery,
    MetaReadDto? Read
);

public record MetaParticipantDto(
    string Id
);

public record MetaMessageDto(
    string Mid,
    string? Text,
    IEnumerable<MetaAttachmentDto>? Attachments,
    MetaQuickReplyDto? QuickReply,
    MetaReplyToDto? ReplyTo,
    bool? IsEcho
);

public record MetaAttachmentDto(
    string Type,
    MetaPayloadDto Payload
);

public record MetaPayloadDto(
    string? Url,
    string? StickerUrl
);

public record MetaQuickReplyDto(
    string Payload
);

public record MetaReplyToDto(
    string Mid
);

public record MetaDeliveryDto(
    IEnumerable<string> Mids,
    long Watermark
);

public record MetaReadDto(
    long Watermark
);

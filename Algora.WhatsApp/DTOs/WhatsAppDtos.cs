namespace Algora.WhatsApp.DTOs;

#region Template DTOs

public record WhatsAppTemplateDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ExternalTemplateId { get; init; }
    public string Language { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? HeaderType { get; init; }
    public string? HeaderContent { get; init; }
    public string Body { get; init; } = string.Empty;
    public string? Footer { get; init; }
    public string? Buttons { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
}

public record CreateWhatsAppTemplateDto
{
    public required string Name { get; init; }
    public string Language { get; init; } = "en";
    public string Category { get; init; } = "MARKETING"; // MARKETING, UTILITY, AUTHENTICATION
    public string? HeaderType { get; init; } // none, text, image, video, document
    public string? HeaderContent { get; init; }
    public required string Body { get; init; }
    public string? Footer { get; init; }
    public string? Buttons { get; init; } // JSON array of button objects
}

public record UpdateWhatsAppTemplateDto
{
    public string? Body { get; init; }
    public string? Footer { get; init; }
    public bool? IsActive { get; init; }
}

#endregion

#region Message DTOs

public record WhatsAppMessageDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string? ExternalMessageId { get; init; }
    public int? CustomerId { get; init; }
    public int? OrderId { get; init; }
    public int? ConversationId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string MessageType { get; init; } = string.Empty;
    public int? TemplateId { get; init; }
    public string? Content { get; init; }
    public string? MediaUrl { get; init; }
    public string? MediaMimeType { get; init; }
    public string? MediaCaption { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SendWhatsAppTemplateMessageDto
{
    public required string PhoneNumber { get; init; }
    public int TemplateId { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
    public int? CustomerId { get; init; }
    public int? OrderId { get; init; }
}

public record SendWhatsAppTextMessageDto
{
    public required string PhoneNumber { get; init; }
    public required string Content { get; init; }
    public int? CustomerId { get; init; }
    public int? OrderId { get; init; }
    public bool PreviewUrl { get; init; } = false;
}

public record SendWhatsAppMediaMessageDto
{
    public required string PhoneNumber { get; init; }
    public required string MediaType { get; init; } // image, video, audio, document
    public required string MediaUrl { get; init; }
    public string? Caption { get; init; }
    public string? Filename { get; init; } // Required for document type
    public int? CustomerId { get; init; }
    public int? OrderId { get; init; }
}

public record SendWhatsAppInteractiveMessageDto
{
    public required string PhoneNumber { get; init; }
    public required string InteractiveType { get; init; } // button, list, product, product_list
    public string? HeaderText { get; init; }
    public required string BodyText { get; init; }
    public string? FooterText { get; init; }
    public List<InteractiveButton>? Buttons { get; init; }
    public List<InteractiveListSection>? Sections { get; init; }
    public int? CustomerId { get; init; }
    public int? OrderId { get; init; }
}

public record InteractiveButton
{
    public required string Id { get; init; }
    public required string Title { get; init; }
}

public record InteractiveListSection
{
    public required string Title { get; init; }
    public List<InteractiveListRow> Rows { get; init; } = new();
}

public record InteractiveListRow
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
}

#endregion

#region Conversation DTOs

public record WhatsAppConversationDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string? ExternalConversationId { get; init; }
    public int? CustomerId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? AssignedTo { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public string? LastMessagePreview { get; init; }
    public int UnreadCount { get; init; }
    public bool IsBusinessInitiated { get; init; }
    public DateTime? WindowExpiresAt { get; init; }
    public bool IsWindowOpen => WindowExpiresAt.HasValue && WindowExpiresAt > DateTime.UtcNow;
    public DateTime CreatedAt { get; init; }
}

public record UpdateConversationDto
{
    public string? Status { get; init; }
    public string? AssignedTo { get; init; }
}

#endregion

#region Campaign DTOs

public record WhatsAppCampaignDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int TemplateId { get; init; }
    public string? TemplateName { get; init; }
    public int? SegmentId { get; init; }
    public string? SegmentName { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? ScheduledAt { get; init; }
    public DateTime? SentAt { get; init; }
    public int TotalRecipients { get; init; }
    public int TotalSent { get; init; }
    public int TotalDelivered { get; init; }
    public int TotalRead { get; init; }
    public int TotalFailed { get; init; }
    public double DeliveryRate => TotalSent > 0 ? (double)TotalDelivered / TotalSent * 100 : 0;
    public double ReadRate => TotalDelivered > 0 ? (double)TotalRead / TotalDelivered * 100 : 0;
    public DateTime CreatedAt { get; init; }
}

public record CreateWhatsAppCampaignDto
{
    public required string Name { get; init; }
    public int TemplateId { get; init; }
    public int? SegmentId { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
}

public record UpdateWhatsAppCampaignDto
{
    public string? Name { get; init; }
    public int? TemplateId { get; init; }
    public int? SegmentId { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public string? Status { get; init; }
}

#endregion

#region Webhook DTOs

/// <summary>
/// Facebook WhatsApp Business API webhook payload for incoming messages.
/// </summary>
public record WhatsAppWebhookPayload
{
    public string Object { get; init; } = string.Empty;
    public List<WebhookEntry> Entry { get; init; } = new();
}

public record WebhookEntry
{
    public string Id { get; init; } = string.Empty;
    public List<WebhookChange> Changes { get; init; } = new();
}

public record WebhookChange
{
    public string Field { get; init; } = string.Empty;
    public WebhookValue Value { get; init; } = new();
}

public record WebhookValue
{
    public string MessagingProduct { get; init; } = string.Empty;
    public WebhookMetadata? Metadata { get; init; }
    public List<WebhookContact>? Contacts { get; init; }
    public List<WebhookMessage>? Messages { get; init; }
    public List<WebhookStatus>? Statuses { get; init; }
}

public record WebhookMetadata
{
    public string DisplayPhoneNumber { get; init; } = string.Empty;
    public string PhoneNumberId { get; init; } = string.Empty;
}

public record WebhookContact
{
    public WebhookProfile? Profile { get; init; }
    public string WaId { get; init; } = string.Empty;
}

public record WebhookProfile
{
    public string Name { get; init; } = string.Empty;
}

public record WebhookMessage
{
    public string Id { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public string Timestamp { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public WebhookTextContent? Text { get; init; }
    public WebhookMediaContent? Image { get; init; }
    public WebhookMediaContent? Video { get; init; }
    public WebhookMediaContent? Audio { get; init; }
    public WebhookMediaContent? Document { get; init; }
    public WebhookInteractiveReply? Interactive { get; init; }
    public WebhookButtonReply? Button { get; init; }
}

public record WebhookTextContent
{
    public string Body { get; init; } = string.Empty;
}

public record WebhookMediaContent
{
    public string Id { get; init; } = string.Empty;
    public string? MimeType { get; init; }
    public string? Caption { get; init; }
    public string? Filename { get; init; }
}

public record WebhookInteractiveReply
{
    public string Type { get; init; } = string.Empty;
    public WebhookButtonReply? ButtonReply { get; init; }
    public WebhookListReply? ListReply { get; init; }
}

public record WebhookButtonReply
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}

public record WebhookListReply
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record WebhookStatus
{
    public string Id { get; init; } = string.Empty;
    public string RecipientId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Timestamp { get; init; } = string.Empty;
    public WebhookConversation? Conversation { get; init; }
    public WebhookPricing? Pricing { get; init; }
    public List<WebhookError>? Errors { get; init; }
}

public record WebhookConversation
{
    public string Id { get; init; } = string.Empty;
    public string? Origin { get; init; }
    public WebhookExpiration? ExpirationTimestamp { get; init; }
}

public record WebhookExpiration
{
    public string ExpirationTimestamp { get; init; } = string.Empty;
}

public record WebhookPricing
{
    public string Category { get; init; } = string.Empty;
    public string PricingModel { get; init; } = string.Empty;
}

public record WebhookError
{
    public int Code { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Message { get; init; }
    public WebhookErrorData? ErrorData { get; init; }
}

public record WebhookErrorData
{
    public string? Details { get; init; }
}

#endregion

#region API Response DTOs

public record WhatsAppApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
}

public record SendMessageResponse
{
    public string MessagingProduct { get; init; } = string.Empty;
    public List<MessageContact>? Contacts { get; init; }
    public List<SentMessage>? Messages { get; init; }
}

public record MessageContact
{
    public string Input { get; init; } = string.Empty;
    public string WaId { get; init; } = string.Empty;
}

public record SentMessage
{
    public string Id { get; init; } = string.Empty;
}

public record TemplateApiResponse
{
    public string Id { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
}

#endregion

namespace Algora.Application.DTOs.Communication;

public record WhatsAppTemplateDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ExternalTemplateId { get; init; }
    public string Language { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? Footer { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateWhatsAppTemplateDto
{
    public required string Name { get; init; }
    public string Language { get; init; } = "en";
    public string Category { get; init; } = "MARKETING";
    public string? HeaderType { get; init; }
    public string? HeaderContent { get; init; }
    public required string Body { get; init; }
    public string? Footer { get; init; }
    public string? Buttons { get; init; }
}

public record UpdateWhatsAppTemplateDto
{
    public string? Body { get; init; }
    public string? Footer { get; init; }
    public bool? IsActive { get; init; }
}

public record WhatsAppMessageDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string? ExternalMessageId { get; init; }
    public int? CustomerId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string MessageType { get; init; } = string.Empty;
    public string? Content { get; init; }
    public string Status { get; init; } = string.Empty;
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
}

public record WhatsAppConversationDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public int? CustomerId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? AssignedTo { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public string? LastMessagePreview { get; init; }
    public int UnreadCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record WhatsAppCampaignDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int TemplateId { get; init; }
    public string? TemplateName { get; init; }
    public int? SegmentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? SentAt { get; init; }
    public int TotalRecipients { get; init; }
    public int TotalSent { get; init; }
    public int TotalDelivered { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateWhatsAppCampaignDto
{
    public required string Name { get; init; }
    public int TemplateId { get; init; }
    public int? SegmentId { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
}

public record WhatsAppWebhookPayload
{
    public string MessageId { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public string? Type { get; init; }
    public string? Text { get; init; }
    public DateTime Timestamp { get; init; }
}

public record WhatsAppStatusPayload
{
    public string MessageId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string? ErrorCode { get; init; }
}
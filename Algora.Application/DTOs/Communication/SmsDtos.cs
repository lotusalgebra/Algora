namespace Algora.Application.DTOs.Communication;

public record SmsTemplateDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string TemplateType { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateSmsTemplateDto
{
    public required string Name { get; init; }
    public required string TemplateType { get; init; }
    public required string Body { get; init; }
}

public record UpdateSmsTemplateDto
{
    public string? Name { get; init; }
    public string? Body { get; init; }
    public bool? IsActive { get; init; }
}

public record SmsMessageDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string? ExternalMessageId { get; init; }
    public int? CustomerId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int SegmentCount { get; init; }
    public decimal? Cost { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SendSmsMessageDto
{
    public required string PhoneNumber { get; init; }
    public required string Body { get; init; }
    public int? CustomerId { get; init; }
    public int? OrderId { get; init; }
}

public record SendBulkSmsDto
{
    public IEnumerable<string> PhoneNumbers { get; init; } = [];
    public required string Body { get; init; }
    public int? SegmentId { get; init; }
}

public record SmsDeliveryStatusPayload
{
    public string MessageId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string? ErrorCode { get; init; }
}
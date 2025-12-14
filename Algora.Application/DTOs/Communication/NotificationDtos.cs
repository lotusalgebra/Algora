namespace Algora.Application.DTOs.Communication;

public record NotificationDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public int? CustomerId { get; init; }
    public int? OrderId { get; init; }
    public string NotificationType { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string? Body { get; init; }
    public string? Recipient { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SendEmailNotificationDto
{
    public required string ToEmail { get; init; }
    public string? ToName { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public bool IsHtml { get; init; } = true;
    public int? CustomerId { get; init; }
    public int? OrderId { get; init; }
    public string? FromName { get; init; }
    public string? FromEmail { get; init; }
}

public record SendSmsNotificationDto
{
    public required string PhoneNumber { get; init; }
    public required string Body { get; init; }
    public int? CustomerId { get; init; }
    public int? OrderId { get; init; }
}

public record SendWhatsAppNotificationDto
{
    public required string PhoneNumber { get; init; }
    public int TemplateId { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
    public int? CustomerId { get; init; }
    public int? OrderId { get; init; }
}
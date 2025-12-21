namespace Algora.Application.DTOs.Communication;

public record EmailSubscriberDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public int? CustomerId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public bool EmailOptIn { get; init; }
    public bool SmsOptIn { get; init; }
    public bool WhatsAppOptIn { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? UnsubscribedAt { get; init; }
    public string? Tags { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateEmailSubscriberDto
{
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public int? CustomerId { get; init; }
    public string Source { get; init; } = "manual";
    public bool EmailOptIn { get; init; } = true;
    public bool SmsOptIn { get; init; }
    public bool WhatsAppOptIn { get; init; }
    public string? Tags { get; init; }
}

public record UpdateEmailSubscriberDto
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public bool? EmailOptIn { get; init; }
    public bool? SmsOptIn { get; init; }
    public bool? WhatsAppOptIn { get; init; }
    public string? Tags { get; init; }
}
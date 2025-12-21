namespace Algora.Application.DTOs.Communication;

public record EmailListDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public bool DoubleOptIn { get; init; }
    public int SubscriberCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateEmailListDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsDefault { get; init; }
    public bool DoubleOptIn { get; init; }
}

public record UpdateEmailListDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
    public bool? DoubleOptIn { get; init; }
}
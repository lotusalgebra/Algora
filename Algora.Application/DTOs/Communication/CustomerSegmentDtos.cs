namespace Algora.Application.DTOs.Communication;

public record CustomerSegmentDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string SegmentType { get; init; } = string.Empty;
    public string? FilterCriteria { get; init; }
    public int MemberCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastCalculatedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateCustomerSegmentDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string SegmentType { get; init; } = "static";
    public string? FilterCriteria { get; init; }
}

public record UpdateCustomerSegmentDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? FilterCriteria { get; init; }
    public bool? IsActive { get; init; }
}
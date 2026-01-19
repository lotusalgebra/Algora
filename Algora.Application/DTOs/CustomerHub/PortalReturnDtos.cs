namespace Algora.Application.DTOs.CustomerHub;

/// <summary>
/// DTO for admin portal return request list view
/// </summary>
public record PortalReturnListDto(
    int Id,
    string CustomerEmail,
    string OrderId,
    string OrderNumber,
    string RequestType,
    string Status,
    string Reason,
    int ItemCount,
    decimal TotalValue,
    DateTime CreatedAt,
    DateTime? ResolvedAt
);

/// <summary>
/// DTO for admin portal return request detail view
/// </summary>
public class PortalReturnDetailDto
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? AdditionalComments { get; set; }
    public string PreferredResolution { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }
    public string? ReturnLabelUrl { get; set; }
    public string? ReturnTrackingNumber { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public List<PortalReturnItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for portal return request items
/// </summary>
public record PortalReturnItemDto(
    int Id,
    string LineItemId,
    string? VariantId,
    string Title,
    string? VariantTitle,
    string? Sku,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? ItemReason,
    string Condition
);

/// <summary>
/// DTO for updating portal return request status
/// </summary>
public class UpdatePortalReturnDto
{
    public string Status { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }
    public string? ReturnLabelUrl { get; set; }
    public decimal? RefundAmount { get; set; }
}

/// <summary>
/// Paginated result for portal returns
/// </summary>
public class PortalReturnPaginatedResult
{
    public List<PortalReturnListDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>
/// Portal return request statuses
/// </summary>
public static class PortalReturnStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = new[] { Pending, Approved, Rejected, Processing, Completed, Cancelled };
}

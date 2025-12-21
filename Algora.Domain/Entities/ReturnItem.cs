namespace Algora.Domain.Entities;

/// <summary>
/// Represents a line item in a return request.
/// </summary>
public class ReturnItem
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    // Parent return request

    /// <summary>
    /// Foreign key to the return request.
    /// </summary>
    public int ReturnRequestId { get; set; }

    /// <summary>
    /// Navigation property to the return request.
    /// </summary>
    public ReturnRequest ReturnRequest { get; set; } = null!;

    // Original order line

    /// <summary>
    /// Foreign key to the original order line.
    /// </summary>
    public int? OrderLineId { get; set; }

    /// <summary>
    /// Navigation property to the order line.
    /// </summary>
    public OrderLine? OrderLine { get; set; }

    // Product info (denormalized)

    /// <summary>
    /// Platform product ID (Shopify).
    /// </summary>
    public long? PlatformProductId { get; set; }

    /// <summary>
    /// Platform variant ID (Shopify).
    /// </summary>
    public long? PlatformVariantId { get; set; }

    /// <summary>
    /// Product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Variant title (e.g., "Size: M, Color: Blue").
    /// </summary>
    public string? VariantTitle { get; set; }

    /// <summary>
    /// SKU of the product.
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Product image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    // Return quantities

    /// <summary>
    /// Original quantity ordered.
    /// </summary>
    public int QuantityOrdered { get; set; }

    /// <summary>
    /// Quantity being returned.
    /// </summary>
    public int QuantityReturned { get; set; }

    // Financials

    /// <summary>
    /// Unit price of the item.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Refund amount for this item.
    /// </summary>
    public decimal RefundAmount { get; set; }

    // Item-specific reason

    /// <summary>
    /// Foreign key to item-specific return reason (optional override).
    /// </summary>
    public int? ReturnReasonId { get; set; }

    /// <summary>
    /// Navigation property to the return reason.
    /// </summary>
    public ReturnReason? ReturnReason { get; set; }

    /// <summary>
    /// Customer note for this specific item.
    /// </summary>
    public string? CustomerNote { get; set; }

    // Restock

    /// <summary>
    /// Whether this item should be restocked.
    /// </summary>
    public bool Restock { get; set; } = true;

    /// <summary>
    /// Whether this item has been restocked.
    /// </summary>
    public bool Restocked { get; set; }

    /// <summary>
    /// When this item was restocked.
    /// </summary>
    public DateTime? RestockedAt { get; set; }

    // Condition assessment (when received)

    /// <summary>
    /// Condition of the item when received: new, like_new, used, damaged.
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Notes about the item condition.
    /// </summary>
    public string? ConditionNote { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

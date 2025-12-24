namespace Algora.Domain.Entities;

/// <summary>
/// Represents a product exchange request.
/// </summary>
public class Exchange
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this exchange belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Unique exchange number (EXC-XXXXXX format).
    /// </summary>
    public string ExchangeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the original order.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Navigation property to the original order.
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Human-readable order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the customer.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Navigation property to the customer.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Customer's email address.
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Customer's name.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Current status: pending, approved, shipped, received, completed, cancelled.
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Foreign key to linked return request (if applicable).
    /// </summary>
    public int? ReturnRequestId { get; set; }

    /// <summary>
    /// Navigation property to linked return request.
    /// </summary>
    public ReturnRequest? ReturnRequest { get; set; }

    /// <summary>
    /// Foreign key to the new order created for exchange items.
    /// </summary>
    public int? NewOrderId { get; set; }

    /// <summary>
    /// Navigation property to the new order.
    /// </summary>
    public Order? NewOrder { get; set; }

    /// <summary>
    /// Price difference. Positive = customer pays more, Negative = customer gets refund.
    /// </summary>
    public decimal PriceDifference { get; set; }

    /// <summary>
    /// Currency for the price difference.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Admin notes about the exchange.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When the exchange was approved.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// When the exchange was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the exchange request was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the exchange was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Items in this exchange.
    /// </summary>
    public ICollection<ExchangeItem> Items { get; set; } = new List<ExchangeItem>();
}

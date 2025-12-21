namespace Algora.Domain.Entities;

/// <summary>
/// Represents a customer return request for an order.
/// </summary>
public class ReturnRequest
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this return belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    // Order relationship

    /// <summary>
    /// Foreign key to the order.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Navigation property to the order.
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Platform order ID (Shopify).
    /// </summary>
    public long PlatformOrderId { get; set; }

    /// <summary>
    /// Human-readable order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    // Customer info (denormalized)

    /// <summary>
    /// Foreign key to the customer.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Navigation property to the customer.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Customer email address.
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    // Return request details

    /// <summary>
    /// Unique return request number (RTN-XXXXXX format).
    /// </summary>
    public string RequestNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current status: pending, approved, rejected, shipped, received, refunded, cancelled.
    /// </summary>
    public string Status { get; set; } = "pending";

    // Reason

    /// <summary>
    /// Foreign key to the return reason.
    /// </summary>
    public int? ReturnReasonId { get; set; }

    /// <summary>
    /// Navigation property to the return reason.
    /// </summary>
    public ReturnReason? ReturnReason { get; set; }

    /// <summary>
    /// Reason code (denormalized).
    /// </summary>
    public string ReasonCode { get; set; } = string.Empty;

    /// <summary>
    /// Reason display text (denormalized).
    /// </summary>
    public string ReasonDescription { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes from the customer.
    /// </summary>
    public string? CustomerNote { get; set; }

    // Approval

    /// <summary>
    /// Whether this return was auto-approved.
    /// </summary>
    public bool IsAutoApproved { get; set; }

    /// <summary>
    /// Admin note when approving.
    /// </summary>
    public string? ApprovalNote { get; set; }

    /// <summary>
    /// Reason for rejection.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// When the return was approved.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// When the return was rejected.
    /// </summary>
    public DateTime? RejectedAt { get; set; }

    // Shipping (return label)

    /// <summary>
    /// Foreign key to the return label.
    /// </summary>
    public int? ReturnLabelId { get; set; }

    /// <summary>
    /// Navigation property to the return label.
    /// </summary>
    public ReturnLabel? ReturnLabel { get; set; }

    /// <summary>
    /// Tracking number (denormalized from label).
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Tracking URL (denormalized from label).
    /// </summary>
    public string? TrackingUrl { get; set; }

    /// <summary>
    /// Carrier name (denormalized from label).
    /// </summary>
    public string? TrackingCarrier { get; set; }

    /// <summary>
    /// When the customer shipped the return.
    /// </summary>
    public DateTime? ShippedAt { get; set; }

    /// <summary>
    /// When the return was received at the warehouse.
    /// </summary>
    public DateTime? ReceivedAt { get; set; }

    // Financials

    /// <summary>
    /// Total refund amount for all items.
    /// </summary>
    public decimal TotalRefundAmount { get; set; }

    /// <summary>
    /// Cost of the return shipping label (to the store).
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Currency for all amounts.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Foreign key to the refund (if processed).
    /// </summary>
    public int? RefundId { get; set; }

    /// <summary>
    /// Navigation property to the refund.
    /// </summary>
    public Refund? Refund { get; set; }

    /// <summary>
    /// When the refund was processed.
    /// </summary>
    public DateTime? RefundedAt { get; set; }

    // Return address

    /// <summary>
    /// Return warehouse address as JSON.
    /// </summary>
    public string? ReturnAddressJson { get; set; }

    // Timestamps

    /// <summary>
    /// When the customer submitted the request.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the return label expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // Navigation

    /// <summary>
    /// Items in this return request.
    /// </summary>
    public ICollection<ReturnItem> Items { get; set; } = new List<ReturnItem>();
}

namespace Algora.Domain.Entities;

/// <summary>
/// Tracks individual upsell events: impressions, clicks, and conversions.
/// Used for both reporting and A/B test analysis.
/// </summary>
public class UpsellConversion
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this event belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    // Source order that triggered the upsell
    /// <summary>
    /// Foreign key to the source order that triggered the upsell display.
    /// </summary>
    public int? SourceOrderId { get; set; }

    /// <summary>
    /// Navigation property to source order.
    /// </summary>
    public Order? SourceOrder { get; set; }

    /// <summary>
    /// Platform order ID of the source order.
    /// </summary>
    public long PlatformSourceOrderId { get; set; }

    // Customer tracking
    /// <summary>
    /// Session ID for consistent experiment assignment.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to customer if known.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Navigation property to customer.
    /// </summary>
    public Customer? Customer { get; set; }

    // Offer shown
    /// <summary>
    /// Foreign key to the upsell offer that was shown.
    /// </summary>
    public int UpsellOfferId { get; set; }

    /// <summary>
    /// Navigation property to upsell offer.
    /// </summary>
    public UpsellOffer UpsellOffer { get; set; } = null!;

    // Experiment tracking
    /// <summary>
    /// Foreign key to experiment if this is part of an A/B test.
    /// </summary>
    public int? ExperimentId { get; set; }

    /// <summary>
    /// Navigation property to experiment.
    /// </summary>
    public UpsellExperiment? Experiment { get; set; }

    /// <summary>
    /// Assigned variant: control, variant_a, variant_b.
    /// </summary>
    public string? AssignedVariant { get; set; }

    // Event tracking
    /// <summary>
    /// When the offer was shown to the customer.
    /// </summary>
    public DateTime ImpressionAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the customer clicked the offer (null if not clicked).
    /// </summary>
    public DateTime? ClickedAt { get; set; }

    /// <summary>
    /// When the customer completed a purchase (null if not converted).
    /// </summary>
    public DateTime? ConvertedAt { get; set; }

    // Conversion details
    /// <summary>
    /// Platform order ID of the conversion order.
    /// </summary>
    public long? ConversionOrderId { get; set; }

    /// <summary>
    /// Revenue from the conversion.
    /// </summary>
    public decimal? ConversionRevenue { get; set; }

    /// <summary>
    /// Quantity of products in the conversion.
    /// </summary>
    public int? ConversionQuantity { get; set; }

    // Cart URL tracking
    /// <summary>
    /// The generated cart URL that was shown.
    /// </summary>
    public string? GeneratedCartUrl { get; set; }

    /// <summary>
    /// Whether the cart URL was used (clicked).
    /// </summary>
    public bool CartUrlUsed { get; set; }

    // Attribution
    /// <summary>
    /// Attribution window in minutes (default 30).
    /// </summary>
    public int AttributionWindowMinutes { get; set; } = 30;

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

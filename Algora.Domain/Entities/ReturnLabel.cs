namespace Algora.Domain.Entities;

/// <summary>
/// Shippo return label data.
/// </summary>
public class ReturnLabel
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this label belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    // Shippo references

    /// <summary>
    /// Shippo transaction ID.
    /// </summary>
    public string ShippoTransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Shippo rate ID used to create this label.
    /// </summary>
    public string ShippoRateId { get; set; } = string.Empty;

    /// <summary>
    /// Shippo shipment ID.
    /// </summary>
    public string ShippoShipmentId { get; set; } = string.Empty;

    // Label details

    /// <summary>
    /// Tracking number for the shipment.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// URL to track the shipment.
    /// </summary>
    public string? TrackingUrl { get; set; }

    /// <summary>
    /// Carrier name (e.g., usps, fedex, ups).
    /// </summary>
    public string Carrier { get; set; } = string.Empty;

    /// <summary>
    /// Service level (e.g., usps_priority, fedex_ground).
    /// </summary>
    public string ServiceLevel { get; set; } = string.Empty;

    // Label file

    /// <summary>
    /// URL to download the label PDF.
    /// </summary>
    public string LabelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Label format (PDF, PNG, ZPL).
    /// </summary>
    public string LabelFormat { get; set; } = "PDF";

    // Cost

    /// <summary>
    /// Cost of the label.
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Currency of the cost.
    /// </summary>
    public string Currency { get; set; } = "USD";

    // Addresses (stored as JSON)

    /// <summary>
    /// Customer's address (from address) as JSON.
    /// </summary>
    public string FromAddressJson { get; set; } = string.Empty;

    /// <summary>
    /// Return warehouse address (to address) as JSON.
    /// </summary>
    public string ToAddressJson { get; set; } = string.Empty;

    /// <summary>
    /// Parcel dimensions and weight as JSON.
    /// </summary>
    public string? ParcelJson { get; set; }

    // Status

    /// <summary>
    /// Label status: created, used, expired, refunded.
    /// </summary>
    public string Status { get; set; } = "created";

    /// <summary>
    /// When the label expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the label was used (first scan).
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

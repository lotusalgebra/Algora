namespace Algora.Domain.Entities;

/// <summary>
/// Configurable return reason for a shop.
/// </summary>
public class ReturnReason
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain these reasons belong to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for the reason (e.g., wrong_size, defective).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display text shown to customers.
    /// </summary>
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>
    /// Optional description for the reason.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display order for sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this reason is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether customer must provide additional notes.
    /// </summary>
    public bool RequiresNote { get; set; }

    /// <summary>
    /// Whether this reason indicates a product defect (for analytics).
    /// </summary>
    public bool IsDefect { get; set; }

    /// <summary>
    /// Whether returns with this reason can be auto-approved.
    /// </summary>
    public bool EligibleForAutoApproval { get; set; } = true;

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

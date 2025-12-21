namespace Algora.Domain.Entities;

/// <summary>
/// Per-shop configuration for the return portal.
/// </summary>
public class ReturnSettings
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain these settings belong to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    // Feature toggles

    /// <summary>
    /// Whether the return feature is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether customers can self-service returns.
    /// </summary>
    public bool AllowSelfService { get; set; } = true;

    // Return policy

    /// <summary>
    /// Number of days after delivery that returns are accepted.
    /// </summary>
    public int ReturnWindowDays { get; set; } = 30;

    /// <summary>
    /// Whether delivery confirmation is required before allowing return.
    /// </summary>
    public bool RequireDeliveryConfirmation { get; set; } = true;

    /// <summary>
    /// Number of days before the return label expires.
    /// </summary>
    public int LabelExpirationDays { get; set; } = 14;

    // Auto-approval rules

    /// <summary>
    /// Whether auto-approval is enabled.
    /// </summary>
    public bool AutoApprovalEnabled { get; set; } = true;

    /// <summary>
    /// Maximum refund amount for auto-approval.
    /// </summary>
    public decimal AutoApprovalMaxAmount { get; set; } = 500.00m;

    /// <summary>
    /// Whether a return reason is required for auto-approval.
    /// </summary>
    public bool AutoApprovalRequireReason { get; set; } = true;

    // Shipping (Shippo)

    /// <summary>
    /// Shippo API key for this shop.
    /// </summary>
    public string? ShippoApiKey { get; set; }

    /// <summary>
    /// Whether the store pays for return shipping.
    /// </summary>
    public bool StorePayShipping { get; set; } = true;

    /// <summary>
    /// Default carrier for return labels.
    /// </summary>
    public string? DefaultCarrier { get; set; } = "usps";

    /// <summary>
    /// Default service level for return labels.
    /// </summary>
    public string? DefaultServiceLevel { get; set; } = "usps_priority";

    // Return address

    /// <summary>
    /// Return address name.
    /// </summary>
    public string? ReturnAddressName { get; set; }

    /// <summary>
    /// Return address company.
    /// </summary>
    public string? ReturnAddressCompany { get; set; }

    /// <summary>
    /// Return address street line 1.
    /// </summary>
    public string? ReturnAddressStreet1 { get; set; }

    /// <summary>
    /// Return address street line 2.
    /// </summary>
    public string? ReturnAddressStreet2 { get; set; }

    /// <summary>
    /// Return address city.
    /// </summary>
    public string? ReturnAddressCity { get; set; }

    /// <summary>
    /// Return address state/province.
    /// </summary>
    public string? ReturnAddressState { get; set; }

    /// <summary>
    /// Return address postal code.
    /// </summary>
    public string? ReturnAddressZip { get; set; }

    /// <summary>
    /// Return address country code.
    /// </summary>
    public string? ReturnAddressCountry { get; set; } = "US";

    /// <summary>
    /// Return address phone number.
    /// </summary>
    public string? ReturnAddressPhone { get; set; }

    /// <summary>
    /// Return address email.
    /// </summary>
    public string? ReturnAddressEmail { get; set; }

    // Notifications

    /// <summary>
    /// Whether email notifications are enabled.
    /// </summary>
    public bool EmailNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Whether SMS notifications are enabled.
    /// </summary>
    public bool SmsNotificationsEnabled { get; set; }

    /// <summary>
    /// Email address for admin notifications.
    /// </summary>
    public string? NotificationEmail { get; set; }

    // Branding

    /// <summary>
    /// Page title for the return portal.
    /// </summary>
    public string? PageTitle { get; set; }

    /// <summary>
    /// Return policy text displayed to customers.
    /// </summary>
    public string? PolicyText { get; set; }

    /// <summary>
    /// Logo URL for the return portal.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Primary brand color (hex).
    /// </summary>
    public string? PrimaryColor { get; set; }

    // Timestamps

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

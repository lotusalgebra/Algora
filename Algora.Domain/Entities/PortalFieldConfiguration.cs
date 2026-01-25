namespace Algora.Domain.Entities;

/// <summary>
/// Stores Customer Portal field configuration for forms (Registration, Profile, Checkout)
/// </summary>
public class PortalFieldConfiguration
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Page type: Registration, Profile, Checkout
    /// </summary>
    public string PageType { get; set; } = "Registration";

    /// <summary>
    /// Internal field name (lowercase, underscores)
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Field type: text, email, phone, number, date, select, checkbox, textarea, password
    /// </summary>
    public string FieldType { get; set; } = "text";

    /// <summary>
    /// Display label shown to users
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Placeholder text for the input
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Help text shown below the field
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Whether the field is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether the field is enabled/visible
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// System fields cannot be deleted (email, password, etc.)
    /// </summary>
    public bool IsSystemField { get; set; }

    /// <summary>
    /// Display order on the form
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Regex pattern for validation
    /// </summary>
    public string? ValidationRegex { get; set; }

    /// <summary>
    /// Custom validation error message
    /// </summary>
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// JSON array of options for select fields
    /// </summary>
    public string? SelectOptions { get; set; }

    /// <summary>
    /// Default value for the field
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Minimum length for text fields
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Maximum length for text fields
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Minimum value for number fields
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// Maximum value for number fields
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Custom CSS class for styling
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Column width in a 12-column grid (6 = half, 12 = full)
    /// </summary>
    public int ColumnWidth { get; set; } = 12;

    /// <summary>
    /// Timestamp when the field was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the field was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

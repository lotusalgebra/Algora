namespace Algora.Domain.Entities;

/// <summary>
/// Represents a saved label template design for product labels.
/// </summary>
public class LabelTemplate
{
    public int Id { get; set; }

    /// <summary>
    /// Shop domain this template belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// User-provided template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the template.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Standard label type (Avery5160, Thermal2x1, etc.) or "Custom".
    /// </summary>
    public string LabelType { get; set; } = "Avery5163";

    /// <summary>
    /// Custom width in inches (used when LabelType is "Custom").
    /// </summary>
    public float? CustomWidthInches { get; set; }

    /// <summary>
    /// Custom height in inches (used when LabelType is "Custom").
    /// </summary>
    public float? CustomHeightInches { get; set; }

    /// <summary>
    /// JSON serialized list of LabelFieldDefinition objects.
    /// </summary>
    public string FieldsJson { get; set; } = "[]";

    /// <summary>
    /// Whether this is the default template for the shop.
    /// </summary>
    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

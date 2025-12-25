using System.Text.Json.Serialization;

namespace Algora.Application.DTOs.Operations;

/// <summary>
/// Types of fields that can be placed on a label.
/// </summary>
public enum LabelFieldType
{
    ProductTitle,
    SKU,
    Barcode,
    Price,
    CompareAtPrice,
    VariantTitle,
    VariantOption1,
    VariantOption2,
    VariantOption3,
    Vendor,
    ProductType,
    Weight,
    InventoryQuantity,
    CustomText
}

/// <summary>
/// Represents a field positioned on the label canvas.
/// Positions and sizes are stored as percentages (0-100) for responsive scaling.
/// </summary>
public class LabelFieldDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public LabelFieldType FieldType { get; set; }

    // Position (percentage of canvas, 0-100)
    public float X { get; set; }
    public float Y { get; set; }

    // Size (percentage of canvas, 0-100)
    public float Width { get; set; } = 30;
    public float Height { get; set; } = 15;

    // Text styling
    public string FontFamily { get; set; } = "Arial";
    public float FontSize { get; set; } = 10;
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public string TextAlign { get; set; } = "left"; // left, center, right
    public string TextColor { get; set; } = "#000000";

    // For CustomText field type
    public string? CustomText { get; set; }

    // For Barcode field type
    public string BarcodeFormat { get; set; } = "Code128";

    // Price formatting
    public string PricePrefix { get; set; } = "$";
    public bool ShowCurrency { get; set; } = true;
}

/// <summary>
/// Label template data transfer object.
/// </summary>
public class LabelTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string LabelType { get; set; } = "Avery5163";
    public float? CustomWidthInches { get; set; }
    public float? CustomHeightInches { get; set; }
    public List<LabelFieldDefinition> Fields { get; set; } = new();
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new label template.
/// </summary>
public class CreateLabelTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string LabelType { get; set; } = "Avery5163";
    public float? CustomWidthInches { get; set; }
    public float? CustomHeightInches { get; set; }
    public List<LabelFieldDefinition> Fields { get; set; } = new();
    public bool IsDefault { get; set; }
}

/// <summary>
/// DTO for updating an existing label template.
/// </summary>
public class UpdateLabelTemplateRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string LabelType { get; set; } = "Avery5163";
    public float? CustomWidthInches { get; set; }
    public float? CustomHeightInches { get; set; }
    public List<LabelFieldDefinition> Fields { get; set; } = new();
    public bool IsDefault { get; set; }
}

/// <summary>
/// Request to generate labels for selected products.
/// </summary>
public class GenerateLabelsRequest
{
    public int TemplateId { get; set; }
    public List<LabelProductSelection> Products { get; set; } = new();
    public bool IncludeVariants { get; set; } = true;
}

/// <summary>
/// Product selection for label generation.
/// </summary>
public class LabelProductSelection
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int Copies { get; set; } = 1;
}

/// <summary>
/// Product data used for label preview and rendering.
/// </summary>
public class LabelPreviewData
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public decimal? Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string? VariantTitle { get; set; }
    public string? Option1 { get; set; }
    public string? Option2 { get; set; }
    public string? Option3 { get; set; }
    public string? Vendor { get; set; }
    public string? ProductType { get; set; }
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public int? InventoryQuantity { get; set; }
}

/// <summary>
/// Result of label PDF generation.
/// </summary>
public class LabelGenerationResult
{
    public bool Success { get; set; }
    public byte[]? PdfData { get; set; }
    public string? Error { get; set; }
    public int LabelCount { get; set; }
}

/// <summary>
/// Available field information for the designer palette.
/// </summary>
public class AvailableLabelField
{
    public LabelFieldType FieldType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

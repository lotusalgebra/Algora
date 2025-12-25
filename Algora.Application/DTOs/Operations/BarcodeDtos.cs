using Algora.Application.Interfaces;

namespace Algora.Application.DTOs.Operations;

// ==================== Barcode DTOs ====================

public record BarcodeDto(
    string Value,
    BarcodeFormat Format,
    byte[] ImageData,
    string Base64Image,
    string ContentType,
    int Width,
    int Height
);

public record GenerateBarcodeDto(
    BarcodeFormat Format = BarcodeFormat.Code128,
    string? Value = null,
    string? Prefix = null,
    int Width = 300,
    int Height = 100,
    bool IncludeText = true
);

// ==================== Label DTOs ====================

public record BarcodeLabelDto(
    string Barcode,
    string ProductTitle,
    string? Sku = null,
    decimal? Price = null,
    string? Currency = null,
    int Copies = 1
);

public record LabelLayoutDto(
    LabelSize Size = LabelSize.Standard,
    int LabelsPerRow = 2,
    int RowsPerPage = 5,
    float MarginTop = 10,
    float MarginLeft = 10,
    float LabelWidth = 90,
    float LabelHeight = 50,
    float HorizontalSpacing = 5,
    float VerticalSpacing = 5,
    bool ShowPrice = true,
    bool ShowSku = true,
    bool ShowProductTitle = true
);

public enum LabelSize
{
    Small,      // 1" x 0.5"
    Standard,   // 2" x 1"
    Large,      // 3" x 2"
    Custom
}

// ==================== Standard Label Types ====================

public enum LabelType
{
    // Avery Labels (for inkjet/laser printers - Letter 8.5" x 11")
    Avery5160,      // 1" x 2-5/8" - 30 per sheet (address labels)
    Avery5163,      // 2" x 4" - 10 per sheet (shipping labels)
    Avery5164,      // 3-1/3" x 4" - 6 per sheet (large shipping)
    Avery5167,      // 0.5" x 1.75" - 80 per sheet (return address)
    Avery5195,      // 2/3" x 1-3/4" - 60 per sheet (file folder)

    // Thermal Printer Labels (single label rolls)
    Thermal4x6,         // 4" x 6" - shipping labels (Zebra, DYMO 4XL)
    Thermal2x1,         // 2" x 1" - product labels (DYMO, Brother)
    Thermal3x2,         // 3" x 2" - medium product labels
    Thermal2_25x1_25,   // 2.25" x 1.25" - jewelry/small items

    Custom              // User-defined dimensions
}

public class LabelSizeConfig
{
    public LabelType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public float WidthInches { get; set; }
    public float HeightInches { get; set; }
    public int LabelsPerRow { get; set; }
    public int RowsPerPage { get; set; }
    public float MarginTopInches { get; set; }
    public float MarginLeftInches { get; set; }
    public float HorizontalGapInches { get; set; }
    public float VerticalGapInches { get; set; }
    public bool IsThermal { get; set; }

    // Computed properties
    public float WidthMm => WidthInches * 25.4f;
    public float HeightMm => HeightInches * 25.4f;
    public int TotalLabelsPerPage => LabelsPerRow * RowsPerPage;

    public static LabelSizeConfig GetPreset(LabelType type) => type switch
    {
        // Avery Labels (Letter size 8.5" x 11")
        LabelType.Avery5160 => new LabelSizeConfig
        {
            Type = type,
            Name = "Avery 5160 - Address Labels (30/sheet)",
            WidthInches = 2.625f,
            HeightInches = 1f,
            LabelsPerRow = 3,
            RowsPerPage = 10,
            MarginTopInches = 0.5f,
            MarginLeftInches = 0.1875f,
            HorizontalGapInches = 0.125f,
            VerticalGapInches = 0f,
            IsThermal = false
        },
        LabelType.Avery5163 => new LabelSizeConfig
        {
            Type = type,
            Name = "Avery 5163 - Shipping Labels (10/sheet)",
            WidthInches = 4f,
            HeightInches = 2f,
            LabelsPerRow = 2,
            RowsPerPage = 5,
            MarginTopInches = 0.5f,
            MarginLeftInches = 0.15625f,
            HorizontalGapInches = 0.1875f,
            VerticalGapInches = 0f,
            IsThermal = false
        },
        LabelType.Avery5164 => new LabelSizeConfig
        {
            Type = type,
            Name = "Avery 5164 - Large Shipping Labels (6/sheet)",
            WidthInches = 4f,
            HeightInches = 3.333f,
            LabelsPerRow = 2,
            RowsPerPage = 3,
            MarginTopInches = 0.5f,
            MarginLeftInches = 0.15625f,
            HorizontalGapInches = 0.1875f,
            VerticalGapInches = 0f,
            IsThermal = false
        },
        LabelType.Avery5167 => new LabelSizeConfig
        {
            Type = type,
            Name = "Avery 5167 - Return Address Labels (80/sheet)",
            WidthInches = 1.75f,
            HeightInches = 0.5f,
            LabelsPerRow = 4,
            RowsPerPage = 20,
            MarginTopInches = 0.5f,
            MarginLeftInches = 0.3125f,
            HorizontalGapInches = 0.3125f,
            VerticalGapInches = 0f,
            IsThermal = false
        },
        LabelType.Avery5195 => new LabelSizeConfig
        {
            Type = type,
            Name = "Avery 5195 - File Folder Labels (60/sheet)",
            WidthInches = 1.75f,
            HeightInches = 0.667f,
            LabelsPerRow = 4,
            RowsPerPage = 15,
            MarginTopInches = 0.5f,
            MarginLeftInches = 0.28125f,
            HorizontalGapInches = 0.3125f,
            VerticalGapInches = 0f,
            IsThermal = false
        },

        // Thermal Labels (single label per "page")
        LabelType.Thermal4x6 => new LabelSizeConfig
        {
            Type = type,
            Name = "4x6 Thermal Shipping Label",
            WidthInches = 4f,
            HeightInches = 6f,
            LabelsPerRow = 1,
            RowsPerPage = 1,
            MarginTopInches = 0f,
            MarginLeftInches = 0f,
            HorizontalGapInches = 0f,
            VerticalGapInches = 0f,
            IsThermal = true
        },
        LabelType.Thermal2x1 => new LabelSizeConfig
        {
            Type = type,
            Name = "2x1 Thermal Product Label",
            WidthInches = 2f,
            HeightInches = 1f,
            LabelsPerRow = 1,
            RowsPerPage = 1,
            MarginTopInches = 0f,
            MarginLeftInches = 0f,
            HorizontalGapInches = 0f,
            VerticalGapInches = 0f,
            IsThermal = true
        },
        LabelType.Thermal3x2 => new LabelSizeConfig
        {
            Type = type,
            Name = "3x2 Thermal Product Label",
            WidthInches = 3f,
            HeightInches = 2f,
            LabelsPerRow = 1,
            RowsPerPage = 1,
            MarginTopInches = 0f,
            MarginLeftInches = 0f,
            HorizontalGapInches = 0f,
            VerticalGapInches = 0f,
            IsThermal = true
        },
        LabelType.Thermal2_25x1_25 => new LabelSizeConfig
        {
            Type = type,
            Name = "2.25x1.25 Thermal Label (Jewelry/Small)",
            WidthInches = 2.25f,
            HeightInches = 1.25f,
            LabelsPerRow = 1,
            RowsPerPage = 1,
            MarginTopInches = 0f,
            MarginLeftInches = 0f,
            HorizontalGapInches = 0f,
            VerticalGapInches = 0f,
            IsThermal = true
        },

        // Custom - user provides dimensions
        LabelType.Custom => new LabelSizeConfig
        {
            Type = type,
            Name = "Custom Label Size",
            WidthInches = 2f,
            HeightInches = 1f,
            LabelsPerRow = 1,
            RowsPerPage = 1,
            IsThermal = false
        },

        _ => GetPreset(LabelType.Avery5163)
    };

    public static IEnumerable<LabelSizeConfig> GetAllPresets()
    {
        return Enum.GetValues<LabelType>()
            .Where(t => t != LabelType.Custom)
            .Select(GetPreset);
    }

    public static IEnumerable<LabelSizeConfig> GetAveryPresets()
    {
        return GetAllPresets().Where(c => !c.IsThermal);
    }

    public static IEnumerable<LabelSizeConfig> GetThermalPresets()
    {
        return GetAllPresets().Where(c => c.IsThermal);
    }
}

// ==================== Lookup DTOs ====================

public record ProductVariantBarcodeDto(
    int ProductId,
    string ProductTitle,
    int? ProductVariantId,
    string? VariantTitle,
    string? Sku,
    string Barcode,
    decimal Price,
    int InventoryQuantity,
    string? ImageUrl
);

// ==================== Bulk Operation DTOs ====================

public record BulkBarcodeGenerationDto(
    string ShopDomain,
    int[] ProductIds,
    BarcodeFormat Format = BarcodeFormat.Code128,
    bool OverwriteExisting = false,
    string? Prefix = null
);

public record BulkBarcodeResultDto(
    int TotalProducts,
    int SuccessCount,
    int SkippedCount,
    int FailedCount,
    List<BarcodeAssignmentResultDto> Results
);

public record BarcodeAssignmentResultDto(
    int ProductId,
    int? VariantId,
    string ProductTitle,
    string? Barcode,
    bool Success,
    string? Error
);

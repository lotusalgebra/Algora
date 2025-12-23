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

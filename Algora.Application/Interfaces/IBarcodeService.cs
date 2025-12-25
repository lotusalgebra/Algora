using Algora.Application.DTOs.Operations;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for generating and managing product barcodes.
/// </summary>
public interface IBarcodeService
{
    // Barcode generation
    Task<BarcodeDto> GenerateBarcodeAsync(GenerateBarcodeDto dto);
    Task<IEnumerable<BarcodeDto>> GenerateBarcodesForProductsAsync(string shopDomain, int[] productIds);
    string GenerateUniqueBarcode(BarcodeFormat format, string? prefix = null);

    // Label generation
    Task<byte[]> GenerateBarcodeLabelPdfAsync(BarcodeLabelDto dto);
    Task<byte[]> GenerateBulkLabelsPdfAsync(IEnumerable<BarcodeLabelDto> labels, LabelLayoutDto? layout = null);

    // Barcode assignment
    Task<bool> AssignBarcodeToVariantAsync(int variantId, string barcode);
    Task<ProductVariantBarcodeDto?> LookupByBarcodeAsync(string shopDomain, string barcode);

    // Validation
    bool ValidateBarcode(string barcode, BarcodeFormat format);
    bool IsValidCheckDigit(string barcode, BarcodeFormat format);
}

/// <summary>
/// Supported barcode formats.
/// </summary>
public enum BarcodeFormat
{
    Code128,
    EAN13,
    EAN8,
    UPCA,
    UPCE,
    QRCode,
    Code39,
    DataMatrix
}

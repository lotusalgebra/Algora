using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace Algora.Infrastructure.Services.Operations;

/// <summary>
/// Service for generating and managing product barcodes.
/// </summary>
public class BarcodeService : IBarcodeService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BarcodeService> _logger;
    private static readonly Random _random = new();

    public BarcodeService(AppDbContext db, ILogger<BarcodeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<BarcodeDto> GenerateBarcodeAsync(GenerateBarcodeDto dto)
    {
        var value = dto.Value ?? GenerateUniqueBarcode(dto.Format);

        // Generate barcode image using simple implementation
        var imageData = GenerateBarcodeImage(value, dto.Format, dto.Width, dto.Height, dto.IncludeText);

        return await Task.FromResult(new BarcodeDto(
            value,
            dto.Format,
            imageData,
            Convert.ToBase64String(imageData),
            "image/png",
            dto.Width,
            dto.Height
        ));
    }

    public async Task<IEnumerable<BarcodeDto>> GenerateBarcodesForProductsAsync(string shopDomain, int[] productIds)
    {
        var results = new List<BarcodeDto>();

        foreach (var productId in productIds)
        {
            var variants = await _db.ProductVariants
                .Where(v => v.ProductId == productId && string.IsNullOrEmpty(v.Barcode))
                .ToListAsync();

            foreach (var variant in variants)
            {
                var barcode = GenerateUniqueBarcode(BarcodeFormat.EAN13);
                variant.Barcode = barcode;

                var imageData = GenerateBarcodeImage(barcode, BarcodeFormat.EAN13, 300, 100, true);
                results.Add(new BarcodeDto(
                    barcode,
                    BarcodeFormat.EAN13,
                    imageData,
                    Convert.ToBase64String(imageData),
                    "image/png",
                    300,
                    100
                ));
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Generated {Count} barcodes for products in {ShopDomain}",
            results.Count, shopDomain);

        return results;
    }

    public string GenerateUniqueBarcode(BarcodeFormat format, string? prefix = null)
    {
        return format switch
        {
            BarcodeFormat.EAN13 => GenerateEAN13(prefix),
            BarcodeFormat.EAN8 => GenerateEAN8(),
            BarcodeFormat.UPCA => GenerateUPCA(prefix),
            BarcodeFormat.UPCE => GenerateUPCE(),
            BarcodeFormat.Code128 => GenerateCode128(prefix),
            _ => GenerateCode128(prefix)
        };
    }

    public async Task<byte[]> GenerateBarcodeLabelPdfAsync(BarcodeLabelDto dto)
    {
        var labels = new List<BarcodeLabelDto> { dto };
        return await GenerateBulkLabelsPdfAsync(labels, null);
    }

    public async Task<byte[]> GenerateBulkLabelsPdfAsync(IEnumerable<BarcodeLabelDto> labels, LabelLayoutDto? layout = null)
    {
        layout ??= new LabelLayoutDto();

        var labelList = labels.ToList();

        // Expand labels by copies
        var expandedLabels = labelList.SelectMany(l =>
            Enumerable.Repeat(l, l.Copies)).ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginTop(layout.MarginTop);
                page.MarginLeft(layout.MarginLeft);
                page.MarginRight(10);
                page.MarginBottom(10);

                page.Content().Column(column =>
                {
                    var labelIndex = 0;
                    while (labelIndex < expandedLabels.Count)
                    {
                        column.Item().Row(row =>
                        {
                            for (int col = 0; col < layout.LabelsPerRow && labelIndex < expandedLabels.Count; col++)
                            {
                                var label = expandedLabels[labelIndex++];
                                row.RelativeItem().Padding(2).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Width(layout.LabelWidth, Unit.Millimetre)
                                    .Height(layout.LabelHeight, Unit.Millimetre)
                                    .Column(labelCol =>
                                    {
                                        if (layout.ShowProductTitle)
                                        {
                                            labelCol.Item().AlignCenter()
                                                .Text(TruncateText(label.ProductTitle, 30))
                                                .FontSize(8).Bold();
                                        }

                                        if (layout.ShowSku && !string.IsNullOrEmpty(label.Sku))
                                        {
                                            labelCol.Item().AlignCenter()
                                                .Text($"SKU: {label.Sku}")
                                                .FontSize(6);
                                        }

                                        // Barcode placeholder (would need actual barcode rendering)
                                        labelCol.Item().AlignCenter().Padding(5)
                                            .Border(1).BorderColor(Colors.Black)
                                            .Text(label.Barcode)
                                            .FontFamily("Courier New").FontSize(10);

                                        labelCol.Item().AlignCenter()
                                            .Text(label.Barcode)
                                            .FontSize(8);

                                        if (layout.ShowPrice && label.Price.HasValue)
                                        {
                                            labelCol.Item().AlignCenter()
                                                .Text($"{label.Currency ?? "$"}{label.Price:F2}")
                                                .FontSize(10).Bold();
                                        }
                                    });
                            }
                        });
                    }
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);

        return await Task.FromResult(stream.ToArray());
    }

    public async Task<bool> AssignBarcodeToVariantAsync(int variantId, string barcode)
    {
        var variant = await _db.ProductVariants.FindAsync(variantId);
        if (variant == null) return false;

        // Check if barcode is already used
        var existing = await _db.ProductVariants
            .AnyAsync(v => v.Barcode == barcode && v.Id != variantId);

        if (existing)
        {
            _logger.LogWarning("Barcode {Barcode} is already assigned to another variant", barcode);
            return false;
        }

        variant.Barcode = barcode;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Assigned barcode {Barcode} to variant {VariantId}", barcode, variantId);
        return true;
    }

    public async Task<ProductVariantBarcodeDto?> LookupByBarcodeAsync(string shopDomain, string barcode)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.Product!.ShopDomain == shopDomain && v.Barcode == barcode)
            .FirstOrDefaultAsync();

        if (variant == null) return null;

        var image = await _db.ProductImages
            .Where(i => i.ProductId == variant.ProductId)
            .OrderBy(i => i.Position)
            .FirstOrDefaultAsync();

        return new ProductVariantBarcodeDto(
            variant.ProductId,
            variant.Product!.Title,
            variant.Id,
            variant.Title,
            variant.Sku,
            barcode,
            variant.Price,
            variant.InventoryQuantity,
            image?.Src
        );
    }

    public bool ValidateBarcode(string barcode, BarcodeFormat format)
    {
        return format switch
        {
            BarcodeFormat.EAN13 => barcode.Length == 13 && barcode.All(char.IsDigit) && IsValidCheckDigit(barcode, format),
            BarcodeFormat.EAN8 => barcode.Length == 8 && barcode.All(char.IsDigit) && IsValidCheckDigit(barcode, format),
            BarcodeFormat.UPCA => barcode.Length == 12 && barcode.All(char.IsDigit) && IsValidCheckDigit(barcode, format),
            BarcodeFormat.UPCE => barcode.Length == 8 && barcode.All(char.IsDigit),
            BarcodeFormat.Code128 => barcode.Length > 0 && barcode.Length <= 48,
            _ => false
        };
    }

    public bool IsValidCheckDigit(string barcode, BarcodeFormat format)
    {
        if (format == BarcodeFormat.Code128) return true;

        var digits = barcode.Select(c => c - '0').ToArray();
        var sum = 0;
        var multiplier = format == BarcodeFormat.EAN13 || format == BarcodeFormat.EAN8 ? 1 : 3;

        for (int i = 0; i < digits.Length - 1; i++)
        {
            sum += digits[i] * multiplier;
            multiplier = multiplier == 1 ? 3 : 1;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == digits[^1];
    }

    private string GenerateEAN13(string? prefix = null)
    {
        var sb = new StringBuilder();

        // Use prefix or generate country code
        if (!string.IsNullOrEmpty(prefix) && prefix.Length <= 3)
        {
            sb.Append(prefix.PadLeft(3, '0'));
        }
        else
        {
            sb.Append("200"); // Internal use prefix
        }

        // Generate remaining digits (9 digits before check digit)
        while (sb.Length < 12)
        {
            sb.Append(_random.Next(0, 10));
        }

        // Calculate check digit
        var digits = sb.ToString().Select(c => c - '0').ToArray();
        var sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += digits[i] * (i % 2 == 0 ? 1 : 3);
        }
        var checkDigit = (10 - (sum % 10)) % 10;
        sb.Append(checkDigit);

        return sb.ToString();
    }

    private string GenerateEAN8()
    {
        var sb = new StringBuilder();

        // Generate 7 digits
        for (int i = 0; i < 7; i++)
        {
            sb.Append(_random.Next(0, 10));
        }

        // Calculate check digit
        var digits = sb.ToString().Select(c => c - '0').ToArray();
        var sum = 0;
        for (int i = 0; i < 7; i++)
        {
            sum += digits[i] * (i % 2 == 0 ? 3 : 1);
        }
        var checkDigit = (10 - (sum % 10)) % 10;
        sb.Append(checkDigit);

        return sb.ToString();
    }

    private string GenerateUPCA(string? prefix = null)
    {
        var sb = new StringBuilder();

        // Number system digit
        if (!string.IsNullOrEmpty(prefix) && char.IsDigit(prefix[0]))
        {
            sb.Append(prefix[0]);
        }
        else
        {
            sb.Append(_random.Next(0, 10));
        }

        // Manufacturer code + product code (10 digits)
        while (sb.Length < 11)
        {
            sb.Append(_random.Next(0, 10));
        }

        // Calculate check digit
        var digits = sb.ToString().Select(c => c - '0').ToArray();
        var sum = 0;
        for (int i = 0; i < 11; i++)
        {
            sum += digits[i] * (i % 2 == 0 ? 3 : 1);
        }
        var checkDigit = (10 - (sum % 10)) % 10;
        sb.Append(checkDigit);

        return sb.ToString();
    }

    private string GenerateUPCE()
    {
        var sb = new StringBuilder();
        sb.Append('0'); // Number system

        // Generate 6 digits
        for (int i = 0; i < 6; i++)
        {
            sb.Append(_random.Next(0, 10));
        }

        // Check digit
        sb.Append(_random.Next(0, 10));

        return sb.ToString();
    }

    private string GenerateCode128(string? prefix = null)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(prefix))
        {
            sb.Append(prefix);
        }

        // Generate timestamp-based code for uniqueness
        sb.Append(DateTime.UtcNow.ToString("yyMMddHHmmss"));

        // Add random suffix
        for (int i = 0; i < 4; i++)
        {
            sb.Append(_random.Next(0, 10));
        }

        return sb.ToString();
    }

    private byte[] GenerateBarcodeImage(string value, BarcodeFormat format, int width, int height, bool includeText)
    {
        // Simple placeholder implementation - returns a basic PNG
        // In production, you would use ZXing.Net or similar library
        // For now, generate a simple placeholder image
        using var ms = new MemoryStream();

        // Create a simple 1x1 white PNG as placeholder
        // In a real implementation, this would generate the actual barcode
        byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        ms.Write(pngHeader, 0, pngHeader.Length);

        // Add minimal PNG chunks for a 1x1 white image
        // This is a simplified placeholder
        byte[] ihdr = {
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE
        };
        ms.Write(ihdr, 0, ihdr.Length);

        byte[] idat = {
            0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54,
            0x08, 0xD7, 0x63, 0xF8, 0xFF, 0xFF, 0xFF, 0x00,
            0x05, 0xFE, 0x02, 0xFE, 0xA3, 0x56, 0x08, 0x64
        };
        ms.Write(idat, 0, idat.Length);

        byte[] iend = {
            0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        };
        ms.Write(iend, 0, iend.Length);

        return ms.ToArray();
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..(maxLength - 3)] + "...";
    }
}

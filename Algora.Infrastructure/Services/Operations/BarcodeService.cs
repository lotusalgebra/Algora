using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System.Text;
using ZXing;
using ZXing.Common;
using AppBarcodeFormat = Algora.Application.Interfaces.BarcodeFormat;

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
                var barcode = GenerateUniqueBarcode(AppBarcodeFormat.EAN13);
                variant.Barcode = barcode;

                var imageData = GenerateBarcodeImage(barcode, AppBarcodeFormat.EAN13, 300, 100, true);
                results.Add(new BarcodeDto(
                    barcode,
                    AppBarcodeFormat.EAN13,
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

    public string GenerateUniqueBarcode(AppBarcodeFormat format, string? prefix = null)
    {
        return format switch
        {
            AppBarcodeFormat.EAN13 => GenerateEAN13(prefix),
            AppBarcodeFormat.EAN8 => GenerateEAN8(),
            AppBarcodeFormat.UPCA => GenerateUPCA(prefix),
            AppBarcodeFormat.UPCE => GenerateUPCE(),
            AppBarcodeFormat.Code128 => GenerateCode128(prefix),
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

                                        // Generate actual barcode image
                                        var barcodeImage = GenerateBarcodeImage(
                                            label.Barcode,
                                            AppBarcodeFormat.Code128,
                                            (int)(layout.LabelWidth * 2.5f),
                                            40,
                                            false
                                        );
                                        labelCol.Item().AlignCenter().Padding(2)
                                            .Image(barcodeImage);

                                        labelCol.Item().AlignCenter()
                                            .Text(label.Barcode)
                                            .FontFamily("Courier New").FontSize(8);

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

    public bool ValidateBarcode(string barcode, AppBarcodeFormat format)
    {
        return format switch
        {
            AppBarcodeFormat.EAN13 => barcode.Length == 13 && barcode.All(char.IsDigit) && IsValidCheckDigit(barcode, format),
            AppBarcodeFormat.EAN8 => barcode.Length == 8 && barcode.All(char.IsDigit) && IsValidCheckDigit(barcode, format),
            AppBarcodeFormat.UPCA => barcode.Length == 12 && barcode.All(char.IsDigit) && IsValidCheckDigit(barcode, format),
            AppBarcodeFormat.UPCE => barcode.Length == 8 && barcode.All(char.IsDigit),
            AppBarcodeFormat.Code128 => barcode.Length > 0 && barcode.Length <= 48,
            _ => false
        };
    }

    public bool IsValidCheckDigit(string barcode, AppBarcodeFormat format)
    {
        if (format == AppBarcodeFormat.Code128) return true;

        var digits = barcode.Select(c => c - '0').ToArray();
        var sum = 0;
        var multiplier = format == AppBarcodeFormat.EAN13 || format == AppBarcodeFormat.EAN8 ? 1 : 3;

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

    private byte[] GenerateBarcodeImage(string value, AppBarcodeFormat format, int width, int height, bool includeText)
    {
        try
        {
            // Map our BarcodeFormat to ZXing BarcodeFormat
            var zxingFormat = MapToZXingFormat(format);

            // Use ZXing.Net to generate a BitMatrix
            var barcodeWriter = new BarcodeWriterGeneric
            {
                Format = zxingFormat,
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 5,
                    PureBarcode = !includeText
                }
            };

            var bitMatrix = barcodeWriter.Encode(value);

            // Convert BitMatrix to SkiaSharp bitmap
            using var bitmap = BitMatrixToSKBitmap(bitMatrix);

            // If text should be included and it's a 1D barcode, add text below
            if (includeText && !Is2DBarcode(format))
            {
                return RenderBarcodeWithText(bitmap, value, bitMatrix.Width, bitMatrix.Height);
            }

            // Encode to PNG
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate barcode image for value: {Value}", value);
            return GenerateFallbackImage(width, height, value);
        }
    }

    private static SKBitmap BitMatrixToSKBitmap(BitMatrix matrix)
    {
        var width = matrix.Width;
        var height = matrix.Height;
        var bitmap = new SKBitmap(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, matrix[x, y] ? SKColors.Black : SKColors.White);
            }
        }

        return bitmap;
    }

    private static ZXing.BarcodeFormat MapToZXingFormat(AppBarcodeFormat format)
    {
        return format switch
        {
            AppBarcodeFormat.Code128 => ZXing.BarcodeFormat.CODE_128,
            AppBarcodeFormat.EAN13 => ZXing.BarcodeFormat.EAN_13,
            AppBarcodeFormat.EAN8 => ZXing.BarcodeFormat.EAN_8,
            AppBarcodeFormat.UPCA => ZXing.BarcodeFormat.UPC_A,
            AppBarcodeFormat.UPCE => ZXing.BarcodeFormat.UPC_E,
            AppBarcodeFormat.QRCode => ZXing.BarcodeFormat.QR_CODE,
            AppBarcodeFormat.Code39 => ZXing.BarcodeFormat.CODE_39,
            AppBarcodeFormat.DataMatrix => ZXing.BarcodeFormat.DATA_MATRIX,
            _ => ZXing.BarcodeFormat.CODE_128
        };
    }

    private static bool Is2DBarcode(AppBarcodeFormat format)
    {
        return format == AppBarcodeFormat.QRCode || format == AppBarcodeFormat.DataMatrix;
    }

    private byte[] RenderBarcodeWithText(SKBitmap barcodeBitmap, string text, int width, int height)
    {
        const int textHeight = 20;
        var totalHeight = height + textHeight;

        using var surface = SKSurface.Create(new SKImageInfo(width, totalHeight));
        var canvas = surface.Canvas;

        // White background
        canvas.Clear(SKColors.White);

        // Draw barcode
        canvas.DrawBitmap(barcodeBitmap, 0, 0);

        // Draw text
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Courier New", SKFontStyle.Normal)
        };

        var textWidth = paint.MeasureText(text);
        var x = (width - textWidth) / 2;
        canvas.DrawText(text, x, height + 15, paint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private byte[] GenerateFallbackImage(int width, int height, string text)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.White);

        // Draw a border
        using var borderPaint = new SKPaint
        {
            Color = SKColors.Gray,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        canvas.DrawRect(new SKRect(1, 1, width - 1, height - 1), borderPaint);

        // Draw text in center
        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 12,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };
        canvas.DrawText(text, width / 2f, height / 2f + 4, textPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// Generates a barcode image with standard label sizing
    /// </summary>
    public byte[] GenerateBarcodeForLabel(string value, AppBarcodeFormat format, LabelType labelType)
    {
        var config = LabelSizeConfig.GetPreset(labelType);

        // Calculate dimensions in pixels (96 DPI)
        var width = (int)(config.WidthInches * 96);
        var height = (int)(config.HeightInches * 96);

        // For very small labels, reduce barcode height to fit
        var barcodeHeight = Math.Min(height - 20, Is2DBarcode(format) ? width : 60);
        var barcodeWidth = Is2DBarcode(format) ? barcodeHeight : width - 20;

        return GenerateBarcodeImage(value, format, barcodeWidth, barcodeHeight, true);
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..(maxLength - 3)] + "...";
    }
}

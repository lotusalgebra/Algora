using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System.Text.Json;
using ZXing;
using ZXing.Common;

namespace Algora.Infrastructure.Services.Operations;

public class LabelDesignerService : ILabelDesignerService
{
    private readonly AppDbContext _db;
    private readonly ILogger<LabelDesignerService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public LabelDesignerService(AppDbContext db, ILogger<LabelDesignerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region Template CRUD

    public async Task<LabelTemplateDto> CreateTemplateAsync(string shopDomain, CreateLabelTemplateRequest request, CancellationToken ct = default)
    {
        // If setting as default, unset other defaults
        if (request.IsDefault)
        {
            await UnsetDefaultTemplatesAsync(shopDomain, ct);
        }

        var template = new LabelTemplate
        {
            ShopDomain = shopDomain,
            Name = request.Name,
            Description = request.Description,
            LabelType = request.LabelType,
            CustomWidthInches = request.CustomWidthInches,
            CustomHeightInches = request.CustomHeightInches,
            FieldsJson = JsonSerializer.Serialize(request.Fields, JsonOptions),
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        _db.LabelTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created label template '{Name}' for {ShopDomain}", template.Name, shopDomain);

        return MapToDto(template);
    }

    public async Task<LabelTemplateDto?> GetTemplateByIdAsync(string shopDomain, int templateId, CancellationToken ct = default)
    {
        var template = await _db.LabelTemplates
            .Where(t => t.ShopDomain == shopDomain && t.Id == templateId)
            .FirstOrDefaultAsync(ct);

        return template == null ? null : MapToDto(template);
    }

    public async Task<IEnumerable<LabelTemplateDto>> GetTemplatesAsync(string shopDomain, CancellationToken ct = default)
    {
        var templates = await _db.LabelTemplates
            .Where(t => t.ShopDomain == shopDomain)
            .OrderByDescending(t => t.IsDefault)
            .ThenByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .ToListAsync(ct);

        return templates.Select(MapToDto);
    }

    public async Task<LabelTemplateDto?> GetDefaultTemplateAsync(string shopDomain, CancellationToken ct = default)
    {
        var template = await _db.LabelTemplates
            .Where(t => t.ShopDomain == shopDomain && t.IsDefault)
            .FirstOrDefaultAsync(ct);

        return template == null ? null : MapToDto(template);
    }

    public async Task<LabelTemplateDto> UpdateTemplateAsync(string shopDomain, UpdateLabelTemplateRequest request, CancellationToken ct = default)
    {
        var template = await _db.LabelTemplates
            .Where(t => t.ShopDomain == shopDomain && t.Id == request.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new ArgumentException($"Template {request.Id} not found");

        if (request.IsDefault && !template.IsDefault)
        {
            await UnsetDefaultTemplatesAsync(shopDomain, ct);
        }

        template.Name = request.Name;
        template.Description = request.Description;
        template.LabelType = request.LabelType;
        template.CustomWidthInches = request.CustomWidthInches;
        template.CustomHeightInches = request.CustomHeightInches;
        template.FieldsJson = JsonSerializer.Serialize(request.Fields, JsonOptions);
        template.IsDefault = request.IsDefault;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Updated label template '{Name}' ({Id})", template.Name, template.Id);

        return MapToDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(string shopDomain, int templateId, CancellationToken ct = default)
    {
        var template = await _db.LabelTemplates
            .Where(t => t.ShopDomain == shopDomain && t.Id == templateId)
            .FirstOrDefaultAsync(ct);

        if (template == null) return false;

        _db.LabelTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted label template '{Name}' ({Id})", template.Name, templateId);

        return true;
    }

    public async Task<bool> SetDefaultTemplateAsync(string shopDomain, int templateId, CancellationToken ct = default)
    {
        await UnsetDefaultTemplatesAsync(shopDomain, ct);

        var template = await _db.LabelTemplates
            .Where(t => t.ShopDomain == shopDomain && t.Id == templateId)
            .FirstOrDefaultAsync(ct);

        if (template == null) return false;

        template.IsDefault = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return true;
    }

    private async Task UnsetDefaultTemplatesAsync(string shopDomain, CancellationToken ct)
    {
        var defaults = await _db.LabelTemplates
            .Where(t => t.ShopDomain == shopDomain && t.IsDefault)
            .ToListAsync(ct);

        foreach (var t in defaults)
        {
            t.IsDefault = false;
        }
    }

    #endregion

    #region Preview Data

    public async Task<LabelPreviewData?> GetPreviewDataAsync(string shopDomain, int productId, int? variantId = null, CancellationToken ct = default)
    {
        var query = _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.Product!.ShopDomain == shopDomain && v.ProductId == productId);

        if (variantId.HasValue)
        {
            query = query.Where(v => v.Id == variantId.Value);
        }

        var variant = await query.FirstOrDefaultAsync(ct);

        if (variant?.Product == null) return null;

        return new LabelPreviewData
        {
            ProductId = variant.ProductId,
            VariantId = variant.Id,
            ProductTitle = variant.Product.Title,
            SKU = variant.Sku,
            Barcode = variant.Barcode,
            Price = variant.Price,
            CompareAtPrice = variant.CompareAtPrice,
            VariantTitle = variant.Title,
            Option1 = variant.Option1,
            Option2 = variant.Option2,
            Option3 = variant.Option3,
            Vendor = variant.Product.Vendor,
            ProductType = variant.Product.ProductType,
            Weight = variant.Weight,
            WeightUnit = variant.WeightUnit,
            InventoryQuantity = variant.InventoryQuantity
        };
    }

    public async Task<List<LabelPreviewData>> GetPreviewDataForProductsAsync(string shopDomain, List<LabelProductSelection> products, CancellationToken ct = default)
    {
        var result = new List<LabelPreviewData>();

        foreach (var selection in products)
        {
            var data = await GetPreviewDataAsync(shopDomain, selection.ProductId, selection.VariantId, ct);
            if (data != null)
            {
                // Add copies
                for (int i = 0; i < selection.Copies; i++)
                {
                    result.Add(data);
                }
            }
        }

        return result;
    }

    #endregion

    #region Label Generation

    public async Task<LabelGenerationResult> GenerateLabelsPdfAsync(string shopDomain, GenerateLabelsRequest request, CancellationToken ct = default)
    {
        try
        {
            var template = await GetTemplateByIdAsync(shopDomain, request.TemplateId, ct);
            if (template == null)
            {
                return new LabelGenerationResult { Success = false, Error = "Template not found" };
            }

            var labelData = await GetPreviewDataForProductsAsync(shopDomain, request.Products, ct);
            if (labelData.Count == 0)
            {
                return new LabelGenerationResult { Success = false, Error = "No products selected" };
            }

            var labelConfig = GetLabelConfig(template);
            var pdfData = GeneratePdf(template, labelConfig, labelData);

            return new LabelGenerationResult
            {
                Success = true,
                PdfData = pdfData,
                LabelCount = labelData.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating labels PDF");
            return new LabelGenerationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<byte[]?> GenerateSingleLabelPreviewAsync(string shopDomain, int templateId, int productId, int? variantId = null, CancellationToken ct = default)
    {
        var template = await GetTemplateByIdAsync(shopDomain, templateId, ct);
        if (template == null) return null;

        var data = await GetPreviewDataAsync(shopDomain, productId, variantId, ct);
        if (data == null) return null;

        var labelConfig = GetLabelConfig(template);
        return GeneratePdf(template, labelConfig, new List<LabelPreviewData> { data });
    }

    private byte[] GeneratePdf(LabelTemplateDto template, LabelSizeConfig labelConfig, List<LabelPreviewData> labels)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                if (labelConfig.IsThermal)
                {
                    // Thermal labels: each label is its own page
                    page.Size(labelConfig.WidthInches, labelConfig.HeightInches, Unit.Inch);
                    page.Margin(0);
                }
                else
                {
                    // Sheet labels (Avery): Letter size with margins
                    page.Size(PageSizes.Letter);
                    page.MarginTop(labelConfig.MarginTopInches, Unit.Inch);
                    page.MarginLeft(labelConfig.MarginLeftInches, Unit.Inch);
                    page.MarginRight(0.25f, Unit.Inch);
                    page.MarginBottom(0.25f, Unit.Inch);
                }

                page.Content().Column(column =>
                {
                    if (labelConfig.IsThermal)
                    {
                        // One label per page for thermal
                        foreach (var label in labels)
                        {
                            column.Item()
                                .Width(labelConfig.WidthInches, Unit.Inch)
                                .Height(labelConfig.HeightInches, Unit.Inch)
                                .Canvas((object canvasObj, Size size) =>
                                {
                                    var canvas = (SKCanvas)canvasObj;
                                    RenderLabel(canvas, size, template.Fields, label);
                                });
                            column.Item().PageBreak();
                        }
                    }
                    else
                    {
                        // Grid layout for sheet labels
                        var labelIndex = 0;
                        while (labelIndex < labels.Count)
                        {
                            column.Item().Row(row =>
                            {
                                for (int col = 0; col < labelConfig.LabelsPerRow && labelIndex < labels.Count; col++)
                                {
                                    var label = labels[labelIndex++];

                                    if (col > 0)
                                    {
                                        row.ConstantItem(labelConfig.HorizontalGapInches, Unit.Inch);
                                    }

                                    row.ConstantItem(labelConfig.WidthInches, Unit.Inch)
                                        .Height(labelConfig.HeightInches, Unit.Inch)
                                        .Border(0.25f)
                                        .BorderColor(Colors.Grey.Lighten3)
                                        .Canvas((object canvasObj, Size size) =>
                                        {
                                            var canvas = (SKCanvas)canvasObj;
                                            RenderLabel(canvas, size, template.Fields, label);
                                        });
                                }
                            });

                            if (labelConfig.VerticalGapInches > 0)
                            {
                                column.Item().Height(labelConfig.VerticalGapInches, Unit.Inch);
                            }
                        }
                    }
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    private void RenderLabel(SKCanvas canvas, Size size, List<LabelFieldDefinition> fields, LabelPreviewData data)
    {
        // White background
        canvas.Clear(SKColors.White);

        var widthPx = size.Width;
        var heightPx = size.Height;

        foreach (var field in fields)
        {
            var value = GetFieldValue(field, data);
            if (string.IsNullOrEmpty(value) && field.FieldType != LabelFieldType.Barcode)
                continue;

            // Convert percentage positions to pixels
            var x = widthPx * (field.X / 100f);
            var y = heightPx * (field.Y / 100f);
            var fieldWidth = widthPx * (field.Width / 100f);
            var fieldHeight = heightPx * (field.Height / 100f);

            if (field.FieldType == LabelFieldType.Barcode && !string.IsNullOrEmpty(data.Barcode))
            {
                RenderBarcode(canvas, data.Barcode, x, y, fieldWidth, fieldHeight, field.BarcodeFormat);
            }
            else if (!string.IsNullOrEmpty(value))
            {
                RenderText(canvas, value, x, y, fieldWidth, fieldHeight, field);
            }
        }
    }

    private string GetFieldValue(LabelFieldDefinition field, LabelPreviewData data)
    {
        return field.FieldType switch
        {
            LabelFieldType.ProductTitle => data.ProductTitle,
            LabelFieldType.SKU => data.SKU ?? "",
            LabelFieldType.Barcode => data.Barcode ?? "",
            LabelFieldType.Price => FormatPrice(data.Price, field),
            LabelFieldType.CompareAtPrice => FormatPrice(data.CompareAtPrice, field),
            LabelFieldType.VariantTitle => data.VariantTitle ?? "",
            LabelFieldType.VariantOption1 => data.Option1 ?? "",
            LabelFieldType.VariantOption2 => data.Option2 ?? "",
            LabelFieldType.VariantOption3 => data.Option3 ?? "",
            LabelFieldType.Vendor => data.Vendor ?? "",
            LabelFieldType.ProductType => data.ProductType ?? "",
            LabelFieldType.Weight => FormatWeight(data.Weight, data.WeightUnit),
            LabelFieldType.InventoryQuantity => data.InventoryQuantity?.ToString() ?? "",
            LabelFieldType.CustomText => field.CustomText ?? "",
            _ => ""
        };
    }

    private string FormatPrice(decimal? price, LabelFieldDefinition field)
    {
        if (!price.HasValue) return "";
        var prefix = field.ShowCurrency ? field.PricePrefix : "";
        return $"{prefix}{price:F2}";
    }

    private string FormatWeight(decimal? weight, string? unit)
    {
        if (!weight.HasValue) return "";
        return $"{weight:F2} {unit ?? ""}".Trim();
    }

    private void RenderText(SKCanvas canvas, string text, float x, float y, float width, float height, LabelFieldDefinition field)
    {
        using var paint = new SKPaint
        {
            Color = SKColor.Parse(field.TextColor),
            TextSize = field.FontSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(
                field.FontFamily,
                field.IsBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                field.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright
            )
        };

        // Calculate text position based on alignment
        var textWidth = paint.MeasureText(text);
        var textX = field.TextAlign switch
        {
            "center" => x + (width - textWidth) / 2,
            "right" => x + width - textWidth,
            _ => x
        };

        // Center vertically
        var textY = y + (height + field.FontSize) / 2 - 2;

        // Truncate if too long
        if (textWidth > width)
        {
            while (textWidth > width && text.Length > 3)
            {
                text = text[..^4] + "...";
                textWidth = paint.MeasureText(text);
            }
        }

        canvas.DrawText(text, textX, textY, paint);
    }

    private void RenderBarcode(SKCanvas canvas, string value, float x, float y, float width, float height, string formatStr)
    {
        try
        {
            var format = formatStr switch
            {
                "EAN13" => ZXing.BarcodeFormat.EAN_13,
                "EAN8" => ZXing.BarcodeFormat.EAN_8,
                "UPCA" => ZXing.BarcodeFormat.UPC_A,
                "UPCE" => ZXing.BarcodeFormat.UPC_E,
                "QRCode" => ZXing.BarcodeFormat.QR_CODE,
                "Code39" => ZXing.BarcodeFormat.CODE_39,
                "DataMatrix" => ZXing.BarcodeFormat.DATA_MATRIX,
                _ => ZXing.BarcodeFormat.CODE_128
            };

            var barcodeWriter = new BarcodeWriterGeneric
            {
                Format = format,
                Options = new EncodingOptions
                {
                    Width = (int)width,
                    Height = (int)height,
                    Margin = 2,
                    PureBarcode = true
                }
            };

            var bitMatrix = barcodeWriter.Encode(value);

            // Convert to SKBitmap
            using var bitmap = new SKBitmap(bitMatrix.Width, bitMatrix.Height);
            for (int by = 0; by < bitMatrix.Height; by++)
            {
                for (int bx = 0; bx < bitMatrix.Width; bx++)
                {
                    bitmap.SetPixel(bx, by, bitMatrix[bx, by] ? SKColors.Black : SKColors.White);
                }
            }

            // Draw on canvas
            var destRect = new SKRect(x, y, x + width, y + height);
            canvas.DrawBitmap(bitmap, destRect);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to render barcode: {Value}", value);
            // Draw placeholder
            using var paint = new SKPaint
            {
                Color = SKColors.Gray,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            canvas.DrawRect(x, y, width, height, paint);

            using var textPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextSize = 10,
                TextAlign = SKTextAlign.Center
            };
            canvas.DrawText(value, x + width / 2, y + height / 2 + 4, textPaint);
        }
    }

    #endregion

    #region Helper Methods

    private LabelSizeConfig GetLabelConfig(LabelTemplateDto template)
    {
        if (template.LabelType == "Custom")
        {
            return new LabelSizeConfig
            {
                Type = LabelType.Custom,
                Name = "Custom",
                WidthInches = template.CustomWidthInches ?? 2f,
                HeightInches = template.CustomHeightInches ?? 1f,
                LabelsPerRow = 1,
                RowsPerPage = 1,
                IsThermal = true
            };
        }

        if (Enum.TryParse<LabelType>(template.LabelType, out var labelType))
        {
            return LabelSizeConfig.GetPreset(labelType);
        }

        return LabelSizeConfig.GetPreset(LabelType.Avery5163);
    }

    private LabelTemplateDto MapToDto(LabelTemplate template)
    {
        var fields = new List<LabelFieldDefinition>();

        try
        {
            fields = JsonSerializer.Deserialize<List<LabelFieldDefinition>>(template.FieldsJson, JsonOptions)
                ?? new List<LabelFieldDefinition>();
        }
        catch
        {
            _logger.LogWarning("Failed to deserialize fields for template {Id}", template.Id);
        }

        return new LabelTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            LabelType = template.LabelType,
            CustomWidthInches = template.CustomWidthInches,
            CustomHeightInches = template.CustomHeightInches,
            Fields = fields,
            IsDefault = template.IsDefault,
            CreatedAt = template.CreatedAt
        };
    }

    public IEnumerable<AvailableLabelField> GetAvailableFields()
    {
        return new List<AvailableLabelField>
        {
            new() { FieldType = LabelFieldType.ProductTitle, DisplayName = "Product Title", Icon = "fa-heading", Description = "The product name" },
            new() { FieldType = LabelFieldType.SKU, DisplayName = "SKU", Icon = "fa-barcode", Description = "Stock Keeping Unit" },
            new() { FieldType = LabelFieldType.Barcode, DisplayName = "Barcode", Icon = "fa-qrcode", Description = "Barcode image" },
            new() { FieldType = LabelFieldType.Price, DisplayName = "Price", Icon = "fa-dollar-sign", Description = "Current selling price" },
            new() { FieldType = LabelFieldType.CompareAtPrice, DisplayName = "Compare Price", Icon = "fa-tag", Description = "Original price for comparison" },
            new() { FieldType = LabelFieldType.VariantTitle, DisplayName = "Variant Title", Icon = "fa-layer-group", Description = "Variant name (e.g., Large / Blue)" },
            new() { FieldType = LabelFieldType.VariantOption1, DisplayName = "Option 1", Icon = "fa-list", Description = "First variant option" },
            new() { FieldType = LabelFieldType.VariantOption2, DisplayName = "Option 2", Icon = "fa-list", Description = "Second variant option" },
            new() { FieldType = LabelFieldType.VariantOption3, DisplayName = "Option 3", Icon = "fa-list", Description = "Third variant option" },
            new() { FieldType = LabelFieldType.Vendor, DisplayName = "Vendor", Icon = "fa-building", Description = "Brand or manufacturer" },
            new() { FieldType = LabelFieldType.ProductType, DisplayName = "Product Type", Icon = "fa-folder", Description = "Product category" },
            new() { FieldType = LabelFieldType.Weight, DisplayName = "Weight", Icon = "fa-weight", Description = "Product weight" },
            new() { FieldType = LabelFieldType.InventoryQuantity, DisplayName = "Inventory", Icon = "fa-boxes", Description = "Stock quantity" },
            new() { FieldType = LabelFieldType.CustomText, DisplayName = "Custom Text", Icon = "fa-font", Description = "Custom text content" }
        };
    }

    #endregion
}

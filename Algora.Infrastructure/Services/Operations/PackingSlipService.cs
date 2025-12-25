using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace Algora.Infrastructure.Services.Operations;

/// <summary>
/// Service for generating packing slips using QuestPDF.
/// </summary>
public class PackingSlipService : IPackingSlipService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PackingSlipService> _logger;

    public PackingSlipService(AppDbContext db, ILogger<PackingSlipService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PackingSlipResult> GeneratePackingSlipAsync(
        int orderId,
        PackingSlipSettings? settings = null,
        CancellationToken ct = default)
    {
        try
        {
            var data = await GetPackingSlipDataAsync(orderId, ct);
            if (data == null)
            {
                return new PackingSlipResult(orderId, string.Empty, Array.Empty<byte>(), false, "Order not found");
            }

            var pdfData = GeneratePdf(data, settings ?? new PackingSlipSettings());
            return new PackingSlipResult(orderId, data.OrderNumber, pdfData, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate packing slip for order {OrderId}", orderId);
            return new PackingSlipResult(orderId, string.Empty, Array.Empty<byte>(), false, ex.Message);
        }
    }

    public async Task<BulkPackingSlipResult> GenerateBulkPackingSlipsAsync(
        int[] orderIds,
        PackingSlipSettings? settings = null,
        bool combineIntoPdf = true,
        CancellationToken ct = default)
    {
        var results = new List<PackingSlipResult>();
        var successCount = 0;
        var failedCount = 0;

        foreach (var orderId in orderIds)
        {
            var result = await GeneratePackingSlipAsync(orderId, settings, ct);
            results.Add(result);

            if (result.Success)
                successCount++;
            else
                failedCount++;
        }

        byte[]? combinedPdf = null;
        if (combineIntoPdf && successCount > 0)
        {
            combinedPdf = CombinePdfs(results.Where(r => r.Success).Select(r => r.PdfData));
        }

        return new BulkPackingSlipResult(
            orderIds.Length,
            successCount,
            failedCount,
            combinedPdf,
            results
        );
    }

    public async Task<PackingSlipDto?> GetPackingSlipDataAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
            return null;

        var dto = new PackingSlipDto
        {
            OrderNumber = order.OrderNumber ?? $"#{order.Id}",
            OrderDate = order.OrderDate,
            CustomerName = order.Customer != null ? $"{order.Customer.FirstName} {order.Customer.LastName}".Trim() : "Customer",
            CustomerEmail = order.Customer?.Email ?? order.CustomerEmail ?? string.Empty,
            ShippingAddress = ParseAddress(order.ShippingAddress),
            BillingAddress = ParseAddress(order.BillingAddress),
            Items = order.Lines.Select(li => new PackingSlipItemDto
            {
                ProductTitle = li.ProductTitle,
                VariantTitle = li.VariantTitle,
                Sku = li.Sku ?? string.Empty,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                LineTotal = li.LineTotal
            }).ToList(),
            Subtotal = order.Subtotal,
            Tax = order.TaxTotal,
            ShippingCost = order.ShippingTotal,
            Discount = order.DiscountTotal,
            Total = order.GrandTotal,
            Currency = order.Currency ?? "USD",
            OrderBarcode = order.OrderNumber ?? order.Id.ToString(),
            Notes = order.Notes
        };

        return dto;
    }

    private static AddressInfo ParseAddress(string? addressJson)
    {
        if (string.IsNullOrEmpty(addressJson))
            return new AddressInfo();

        try
        {
            // Try to parse as JSON if it looks like JSON
            if (addressJson.StartsWith("{"))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(addressJson);
                var root = doc.RootElement;

                return new AddressInfo
                {
                    Name = GetJsonString(root, "name") ?? GetJsonString(root, "first_name") + " " + GetJsonString(root, "last_name"),
                    Company = GetJsonString(root, "company"),
                    Address1 = GetJsonString(root, "address1") ?? string.Empty,
                    Address2 = GetJsonString(root, "address2"),
                    City = GetJsonString(root, "city") ?? string.Empty,
                    Province = GetJsonString(root, "province") ?? GetJsonString(root, "province_code") ?? string.Empty,
                    PostalCode = GetJsonString(root, "zip") ?? string.Empty,
                    Country = GetJsonString(root, "country") ?? GetJsonString(root, "country_code") ?? string.Empty,
                    Phone = GetJsonString(root, "phone")
                };
            }

            // Otherwise treat as plain text address
            return new AddressInfo { Address1 = addressJson };
        }
        catch
        {
            return new AddressInfo { Address1 = addressJson };
        }
    }

    private static string? GetJsonString(System.Text.Json.JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private byte[] GeneratePdf(PackingSlipDto data, PackingSlipSettings settings)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, data, settings));
                page.Content().Element(c => ComposeContent(c, data, settings));
                page.Footer().Element(c => ComposeFooter(c, settings));
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    private void ComposeHeader(IContainer container, PackingSlipDto data, PackingSlipSettings settings)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    if (!string.IsNullOrEmpty(settings.CompanyName))
                    {
                        col.Item().Text(settings.CompanyName).Bold().FontSize(16);
                    }
                    else
                    {
                        col.Item().Text("PACKING SLIP").Bold().FontSize(16);
                    }

                    if (!string.IsNullOrEmpty(settings.CompanyAddress))
                    {
                        col.Item().Text(settings.CompanyAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"Order: {data.OrderNumber}").Bold().FontSize(14);
                    col.Item().Text($"Date: {data.OrderDate:MMM dd, yyyy}").FontSize(9);

                    if (settings.ShowBarcode && !string.IsNullOrEmpty(data.OrderBarcode))
                    {
                        var barcodeImage = GenerateOrderBarcode(data.OrderBarcode);
                        col.Item().PaddingTop(5).AlignRight().Width(120).Image(barcodeImage);
                    }
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container, PackingSlipDto data, PackingSlipSettings settings)
    {
        container.Column(column =>
        {
            // Addresses
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => ComposeAddress(c, "SHIP TO", data.ShippingAddress));
                row.ConstantItem(30);
                row.RelativeItem().Element(c => ComposeAddress(c, "BILL TO", data.BillingAddress));
            });

            column.Item().PaddingVertical(15);

            // Shipping info
            if (settings.ShowShippingInfo && !string.IsNullOrEmpty(data.ShippingMethod))
            {
                column.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Shipping Method: ").Bold();
                        text.Span(data.ShippingMethod);
                    });

                    if (!string.IsNullOrEmpty(data.TrackingNumber))
                    {
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Tracking: ").Bold();
                            text.Span(data.TrackingNumber);
                        });
                    }
                });

                column.Item().PaddingVertical(10);
            }

            // Items table
            column.Item().Element(c => ComposeItemsTable(c, data, settings));

            // Totals
            if (settings.ShowPrices)
            {
                column.Item().PaddingTop(10).AlignRight().Width(200).Element(c => ComposeTotals(c, data));
            }

            // Notes
            if (settings.ShowNotes && !string.IsNullOrEmpty(data.Notes))
            {
                column.Item().PaddingTop(20).Element(c =>
                {
                    c.Background(Colors.Yellow.Lighten4).Padding(10).Column(col =>
                    {
                        col.Item().Text("Notes:").Bold().FontSize(9);
                        col.Item().Text(data.Notes).FontSize(9);
                    });
                });
            }
        });
    }

    private void ComposeAddress(IContainer container, string title, AddressInfo address)
    {
        container.Column(column =>
        {
            column.Item().Text(title).Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
            column.Item().PaddingTop(5);

            if (!string.IsNullOrEmpty(address.Name))
                column.Item().Text(address.Name).Bold();

            if (!string.IsNullOrEmpty(address.Company))
                column.Item().Text(address.Company);

            if (!string.IsNullOrEmpty(address.Address1))
                column.Item().Text(address.Address1);

            if (!string.IsNullOrEmpty(address.Address2))
                column.Item().Text(address.Address2);

            var cityLine = string.Join(", ",
                new[] { address.City, address.Province, address.PostalCode }
                    .Where(s => !string.IsNullOrEmpty(s)));
            if (!string.IsNullOrEmpty(cityLine))
                column.Item().Text(cityLine);

            if (!string.IsNullOrEmpty(address.Country))
                column.Item().Text(address.Country);

            if (!string.IsNullOrEmpty(address.Phone))
                column.Item().Text($"Phone: {address.Phone}").FontSize(9);
        });
    }

    private void ComposeItemsTable(IContainer container, PackingSlipDto data, PackingSlipSettings settings)
    {
        container.Table(table =>
        {
            // Define columns
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(50);  // Qty
                columns.RelativeColumn(3);   // Product
                columns.RelativeColumn(1);   // SKU
                if (settings.ShowPrices)
                {
                    columns.ConstantColumn(70);  // Unit Price
                    columns.ConstantColumn(80);  // Total
                }
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                    .Text("QTY").Bold().FontColor(Colors.White).FontSize(9);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                    .Text("PRODUCT").Bold().FontColor(Colors.White).FontSize(9);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                    .Text("SKU").Bold().FontColor(Colors.White).FontSize(9);

                if (settings.ShowPrices)
                {
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).AlignRight()
                        .Text("PRICE").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).AlignRight()
                        .Text("TOTAL").Bold().FontColor(Colors.White).FontSize(9);
                }
            });

            // Items
            foreach (var item in data.Items)
            {
                var backgroundColor = data.Items.IndexOf(item) % 2 == 0
                    ? Colors.White
                    : Colors.Grey.Lighten4;

                table.Cell().Background(backgroundColor).Padding(5).AlignCenter()
                    .Text(item.Quantity.ToString()).FontSize(10);

                table.Cell().Background(backgroundColor).Padding(5).Column(col =>
                {
                    col.Item().Text(item.ProductTitle).FontSize(10);
                    if (!string.IsNullOrEmpty(item.VariantTitle))
                        col.Item().Text(item.VariantTitle).FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                table.Cell().Background(backgroundColor).Padding(5)
                    .Text(item.Sku).FontSize(9);

                if (settings.ShowPrices)
                {
                    table.Cell().Background(backgroundColor).Padding(5).AlignRight()
                        .Text($"{data.Currency} {item.UnitPrice:F2}").FontSize(9);
                    table.Cell().Background(backgroundColor).Padding(5).AlignRight()
                        .Text($"{data.Currency} {item.LineTotal:F2}").FontSize(9);
                }
            }
        });
    }

    private void ComposeTotals(IContainer container, PackingSlipDto data)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Subtotal:");
                row.ConstantItem(80).AlignRight().Text($"{data.Currency} {data.Subtotal:F2}");
            });

            if (data.Discount > 0)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Discount:");
                    row.ConstantItem(80).AlignRight().Text($"-{data.Currency} {data.Discount:F2}");
                });
            }

            if (data.ShippingCost > 0)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Shipping:");
                    row.ConstantItem(80).AlignRight().Text($"{data.Currency} {data.ShippingCost:F2}");
                });
            }

            if (data.Tax > 0)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Tax:");
                    row.ConstantItem(80).AlignRight().Text($"{data.Currency} {data.Tax:F2}");
                });
            }

            column.Item().PaddingTop(5).LineHorizontal(1);

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Total:").Bold();
                row.ConstantItem(80).AlignRight().Text($"{data.Currency} {data.Total:F2}").Bold();
            });
        });
    }

    private void ComposeFooter(IContainer container, PackingSlipSettings settings)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    if (!string.IsNullOrEmpty(settings.FooterMessage))
                    {
                        text.Span(settings.FooterMessage).FontSize(8);
                    }
                    else
                    {
                        text.Span("Thank you for your order!").FontSize(8);
                    }
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" of ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        });
    }

    private byte[] GenerateOrderBarcode(string orderNumber)
    {
        try
        {
            var barcodeWriter = new BarcodeWriterGeneric
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 240,
                    Height = 40,
                    Margin = 5,
                    PureBarcode = false
                }
            };

            var bitMatrix = barcodeWriter.Encode(orderNumber);
            using var bitmap = BitMatrixToSKBitmap(bitMatrix);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
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

    private byte[] CombinePdfs(IEnumerable<byte[]> pdfs)
    {
        // For simplicity, just return the first PDF or combine using QuestPDF
        // A more sophisticated implementation would merge PDFs
        var pdfList = pdfs.ToList();
        if (pdfList.Count == 0)
            return Array.Empty<byte>();

        if (pdfList.Count == 1)
            return pdfList[0];

        // Simple concatenation - in production use a proper PDF merger
        using var ms = new MemoryStream();
        foreach (var pdf in pdfList)
        {
            ms.Write(pdf, 0, pdf.Length);
        }
        return ms.ToArray();
    }
}

using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Generates professional PDF invoices using QuestPDF.
/// This service directly renders invoice data to PDF without requiring HTML conversion.
/// </summary>
public class QuestPdfInvoiceGeneratorService : IPdfGeneratorService
{
    private readonly ILogger<QuestPdfInvoiceGeneratorService> _logger;

    public QuestPdfInvoiceGeneratorService(ILogger<QuestPdfInvoiceGeneratorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GeneratePdfAsync(string html)
    {
        // This method is kept for interface compatibility but we recommend using GenerateInvoicePdfAsync
        _logger.LogWarning("GeneratePdfAsync called with HTML - using fallback text rendering");

        try
        {
            using var stream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Text(html);
                });
            }).GeneratePdf(stream);

            return Task.FromResult(stream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF");
            throw;
        }
    }

    public Task<byte[]> GenerateInvoicePdfAsync(InvoicePdfDto invoice)
    {
        if (invoice == null) throw new ArgumentNullException(nameof(invoice));

        try
        {
            using var stream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                    page.Header().Element(c => ComposeHeader(c, invoice));
                    page.Content().Element(c => ComposeContent(c, invoice));
                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf(stream);

            _logger.LogInformation("Generated PDF invoice for {InvoiceNumber}", invoice.InvoiceNumber);
            return Task.FromResult(stream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF invoice for {InvoiceNumber}", invoice.InvoiceNumber);
            throw;
        }
    }

    private void ComposeHeader(IContainer container, InvoicePdfDto invoice)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Algora")
                    .FontSize(28)
                    .Bold()
                    .FontColor(Colors.Purple.Darken2);

                column.Item().Text("E-Commerce Solutions")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Medium);

                column.Item().Text("Your trusted business partner")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Medium);
            });

            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text("INVOICE")
                    .FontSize(24)
                    .Bold()
                    .FontColor(Colors.Grey.Darken3);

                column.Item().Text(invoice.InvoiceNumber)
                    .FontSize(14)
                    .SemiBold()
                    .FontColor(Colors.Purple.Darken1);

                column.Item().Text($"Date: {invoice.InvoiceDate:MMMM dd, yyyy}")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Medium);

                if (invoice.Total > 0)
                {
                    column.Item().PaddingTop(8).Container()
                        .Background(Colors.Green.Darken1)
                        .Padding(6)
                        .AlignCenter()
                        .Text("PAID")
                        .FontSize(10)
                        .Bold()
                        .FontColor(Colors.White);
                }
            });
        });
    }

    private void ComposeContent(IContainer container, InvoicePdfDto invoice)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // Billing Information
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("BILL TO")
                        .FontSize(9)
                        .Bold()
                        .FontColor(Colors.Grey.Medium);

                    col.Item().PaddingTop(8).Text(invoice.CustomerName)
                        .FontSize(12)
                        .SemiBold();

                    col.Item().Text(invoice.CustomerEmail)
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrEmpty(invoice.BillingAddress))
                    {
                        col.Item().PaddingTop(4).Text(invoice.BillingAddress)
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    }
                });

                if (!string.IsNullOrEmpty(invoice.ShippingAddress))
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("SHIP TO")
                            .FontSize(9)
                            .Bold()
                            .FontColor(Colors.Grey.Medium);

                        col.Item().PaddingTop(8).Text(invoice.ShippingAddress)
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });
                }
            });

            // Line separator
            column.Item().PaddingVertical(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Items Table
            column.Item().Element(c => ComposeTable(c, invoice));

            // Totals
            column.Item().PaddingTop(20).AlignRight().Width(200).Column(totalsCol =>
            {
                totalsCol.Item().Background(Colors.Grey.Lighten4).Padding(12).Column(innerCol =>
                {
                    innerCol.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal")
                            .FontColor(Colors.Grey.Darken1);
                        row.RelativeItem().AlignRight().Text(invoice.Subtotal.ToString("C"))
                            .SemiBold();
                    });

                    if (invoice.Tax > 0)
                    {
                        innerCol.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("Tax")
                                .FontColor(Colors.Grey.Darken1);
                            row.RelativeItem().AlignRight().Text(invoice.Tax.ToString("C"))
                                .SemiBold();
                        });
                    }

                    innerCol.Item().PaddingTop(8).BorderTop(2).BorderColor(Colors.Grey.Darken3).PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text("Total")
                            .FontSize(12)
                            .Bold();
                        row.RelativeItem().AlignRight().Text(invoice.Total.ToString("C"))
                            .FontSize(12)
                            .Bold();
                    });
                });
            });
        });
    }

    private void ComposeTable(IContainer container, InvoicePdfDto invoice)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3); // Product
                columns.RelativeColumn(1); // Qty
                columns.RelativeColumn(1); // Price
                columns.RelativeColumn(1); // Total
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Background(Colors.Purple.Darken2).Padding(8)
                    .Text("Product").FontSize(10).Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Purple.Darken2).Padding(8).AlignCenter()
                    .Text("Qty").FontSize(10).Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Purple.Darken2).Padding(8).AlignRight()
                    .Text("Unit Price").FontSize(10).Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Purple.Darken2).Padding(8).AlignRight()
                    .Text("Total").FontSize(10).Bold().FontColor(Colors.White);
            });

            // Data rows
            var lines = invoice.Lines?.ToList() ?? new List<InvoiceLineDto>();
            if (lines.Count == 0)
            {
                table.Cell().ColumnSpan(4).Padding(20).AlignCenter()
                    .Text("No items").FontColor(Colors.Grey.Medium);
            }
            else
            {
                var isEven = false;
                foreach (var line in lines)
                {
                    var bgColor = isEven ? Colors.Grey.Lighten4 : Colors.White;

                    table.Cell().Background(bgColor).Padding(8)
                        .Text(line.ProductName).FontSize(10).SemiBold();
                    table.Cell().Background(bgColor).Padding(8).AlignCenter()
                        .Text(line.Quantity.ToString()).FontSize(10);
                    table.Cell().Background(bgColor).Padding(8).AlignRight()
                        .Text(line.Price.ToString("C")).FontSize(10);
                    table.Cell().Background(bgColor).Padding(8).AlignRight()
                        .Text(line.Total.ToString("C")).FontSize(10).SemiBold();

                    isEven = !isEven;
                }
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(20).Column(column =>
        {
            column.Item().AlignCenter().Text("Thank You For Your Business!")
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Grey.Darken3);

            column.Item().PaddingTop(8).AlignCenter().Text("If you have any questions about this invoice, please contact us.")
                .FontSize(10)
                .FontColor(Colors.Grey.Medium);

            column.Item().PaddingTop(12).AlignCenter().Row(row =>
            {
                row.AutoItem().Text("Email: support@algora.com")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);

                row.AutoItem().PaddingHorizontal(20).Text("|")
                    .FontColor(Colors.Grey.Lighten1);

                row.AutoItem().Text("Web: www.algora.com")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);
            });
        });
    }
}

using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Simple PDF generator using QuestPDF.
/// </summary>
/// <remarks>
/// Important:
/// - QuestPDF does not include a full HTML/CSS renderer out of the box.
///   This service currently inserts the provided HTML string as plain text into the PDF.
/// - For pixel-perfect HTML rendering you should convert HTML -> PDF using a renderer
///   that understands HTML and CSS (Playwright, wkhtmltopdf, Headless Chromium, or
///   a QuestPDF HTML plugin if you add one).
/// - This implementation protects the app from extremely large inputs by trimming
///   content to a reasonable maximum and logs duration/size for diagnostics.
/// </remarks>
public class PdfGeneratorService : IPdfGeneratorService
{
    private readonly ILogger<PdfGeneratorService> _logger;

    // Prevent extremely large HTML payloads from causing excessive memory usage.
    // Adjust this value to match your hosting environment and expectations.
    private const int MaxHtmlLength = 200_000;

    public PdfGeneratorService(ILogger<PdfGeneratorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a PDF document from the supplied HTML string.
    /// </summary>
    /// <param name="html">
    /// HTML content to render into the PDF.
    /// Note: the current implementation writes the raw HTML markup into the PDF as plain text
    /// because QuestPDF does not include an HTML parser by default. For true HTML/CSS rendering
    /// replace this with an external HTML->PDF renderer or add a QuestPDF HTML rendering extension.
    /// </param>
    /// <returns>
    /// A byte array containing the generated PDF document.
    /// </returns>
    public async Task<byte[]> GeneratePdfAsync(string html)
    {
        if (html is null) throw new ArgumentNullException(nameof(html));

        // Trim leading/trailing whitespace and protect from extremely large payloads.
        var input = html.Trim();
        if (input.Length == 0)
        {
            _logger.LogWarning("GeneratePdfAsync called with empty html content. Returning an empty PDF.");
            input = string.Empty;
        }

        if (input.Length > MaxHtmlLength)
        {
            _logger.LogWarning("HTML input length {Length} exceeds MaxHtmlLength {Max}. Trimming input.", input.Length, MaxHtmlLength);
            input = input.Substring(0, MaxHtmlLength);
        }

        var sw = Stopwatch.StartNew();

        try
        {
            // Generate PDF on a background thread because QuestPDF's rendering is synchronous.
            using var stream = new MemoryStream();
            await Task.Run(() =>
            {
                // Ensure license is set (safe to set multiple times).
                QuestPDF.Settings.License = LicenseType.Community;

                var doc = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);

                        // Simple rendering: write the HTML source as plain text.
                        // For better visual output, use an HTML->PDF renderer.
                        page.Content().Element(c =>
                        {
                            c.PaddingVertical(5);
                            c.Container().Column(column =>
                            {
                                // Header
                                column.Item().Text("Invoice / Document").Bold().FontSize(14);

                                // Add a small separator
                                column.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                                // Body: render the provided html string as preformatted text to preserve markup
                                column.Item().Text(text =>
                                {
                                    text.DefaultTextStyle(x => x.FontSize(9));
                                    text.Line(input);
                                });
                            });
                        });
                    });
                });

                // Generate PDF into the provided MemoryStream.
                doc.GeneratePdf(stream);
            }).ConfigureAwait(false);

            sw.Stop();
            var result = stream.ToArray();

            _logger.LogInformation("PDF generated successfully. InputLength={InputLength}, PdfBytes={Size}, ElapsedMs={ElapsedMs}",
                input.Length, result.Length, sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "PDF generation failed. InputLength={InputLength}, ElapsedMs={ElapsedMs}", input.Length, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Algora.Infrastructure.Services;

public class PdfGeneratorService : IPdfGeneratorService
{
    private readonly ILogger<PdfGeneratorService> _logger;

    public PdfGeneratorService(ILogger<PdfGeneratorService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates a PDF document from the supplied HTML string.
    /// </summary>
    /// <param name="html">
    /// HTML content to render into the PDF. Note: the current implementation writes the raw HTML
    /// markup into the PDF as plain text because QuestPDF does not include an HTML parser by default.
    /// To render HTML visually you must add an HTML rendering plugin for QuestPDF or use an external
    /// renderer (for example Playwright or wkhtmltopdf) to produce PDF bytes.
    /// </param>
    /// <returns>
    /// A byte array containing the generated PDF document. The returned bytes are non-empty when
    /// generation succeeds; callers should validate the result before saving or returning to clients.
    /// </returns>
    /// <remarks>
    /// - Implementation details: QuestPDF is used to create the document. The HTML string is currently
    ///   inserted as plain text into the PDF content area (no HTML/CSS rendering).
    /// - Exceptions thrown by the underlying PDF library are logged and re-thrown to the caller.
    /// - If you need full HTML/CSS fidelity, replace this implementation with a proper HTML->PDF pipeline
    ///   (QuestPDF HTML plugin or Playwright/wkhtmltopdf).
    /// </remarks>
    /// <exception cref="System.Exception">Thrown if PDF generation fails.</exception>
    public async Task<byte[]> GeneratePdfAsync(string html)
    {
        try
        {
            using var stream = new MemoryStream();
            await Task.Run(() =>
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var doc = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);

                        // Quick fallback: write the HTML source as plain text.
                        // This compiles and returns a PDF containing the HTML markup as text.
                        page.Content().Element(c =>
                        {
                            c.Text(html ?? string.Empty)
                             .FontSize(10)
                             .FontColor(Colors.Black);
                        });
                    });
                });

                doc.GeneratePdf(stream);
            });

            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF generation failed.");
            throw;
        }
    }
}
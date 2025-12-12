using System;
using System.Threading.Tasks;
using Algora.Application.Interfaces;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services
{
    /// <summary>
    /// Converts HTML to PDF using wkhtmltopdf via DinkToPdf.
    /// Expects wkhtmltopdf native binaries (libwkhtmltox) available at runtime.
    /// </summary>
    public sealed class WkHtmlToPdfGeneratorService : IPdfGeneratorService
    {
        private readonly ILogger<WkHtmlToPdfGeneratorService> _logger;
        private readonly IConverter _converter;

        public WkHtmlToPdfGeneratorService(ILogger<WkHtmlToPdfGeneratorService> logger, IConverter converter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public Task<byte[]> GeneratePdfAsync(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new ArgumentException("HTML content must not be empty.", nameof(html));

            try
            {
                var doc = new HtmlToPdfDocument()
                {
                    GlobalSettings = new GlobalSettings
                    {
                        ColorMode = ColorMode.Color,
                        Orientation = Orientation.Portrait,
                        PaperSize = PaperKind.A4,
                        Margins = new MarginSettings { Top = 15, Bottom = 15, Left = 12, Right = 12 },
                        DPI = 300
                    },
                    Objects =
                    {
                        new ObjectSettings
                        {
                            HtmlContent = html,
                            WebSettings = new WebSettings
                            {
                                DefaultEncoding = "utf-8",
                                LoadImages = true
                            },
                            LoadSettings = new LoadSettings
                            {
                                BlockLocalFileAccess = false
                            }
                        }
                    }
                };

                var pdf = _converter.Convert(doc);
                return Task.FromResult(pdf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate PDF from HTML (wkhtmltopdf)");
                throw;
            }
        }
    }
}
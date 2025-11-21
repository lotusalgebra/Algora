using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Converts rendered HTML into a PDF binary.
    /// Implementations may use a headless browser, wkhtmltopdf, PuppeteerSharp or any HTML-to-PDF library.
    /// </summary>
    public interface IPdfGeneratorService
    {
        /// <summary>
        /// Generates a PDF from the provided HTML string.
        /// </summary>
        /// <param name="html">
        /// Fully rendered HTML markup to convert to PDF. Caller should include any required styles
        /// (inline or linked with absolute URLs) and resources so the renderer can produce a faithful PDF.
        /// </param>
        /// <returns>
        /// A task that resolves to a byte array containing the generated PDF file data.
        /// The returned array can be written directly to the response stream or saved to disk.
        /// </returns>
        /// <remarks>
        /// Implementations should be resilient to large HTML payloads and handle I/O timeouts.
        /// Consider exposing a cancellation-aware overload if callers need to cancel long-running conversions.
        /// </remarks>
        Task<byte[]> GeneratePdfAsync(string html);
    }
}

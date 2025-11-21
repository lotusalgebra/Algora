using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Service responsible for rendering invoice templates into HTML.
    /// Implementations should convert an <see cref="InvoicePdfDto"/> into a fully rendered
    /// HTML string suitable for display or conversion to PDF.
    /// </summary>
    public interface IInvoiceTemplateService
    {
        /// <summary>
        /// Renders the provided invoice model using the specified template and returns
        /// the rendered HTML as a string.
        /// </summary>
        /// <param name="model">Invoice data including header, lines and totals.</param>
        /// <param name="templateName">
        /// Optional template identifier. Defaults to <c>"Default"</c>.
        /// Implementations may support multiple named templates (for example,
        /// "Compact", "Detailed", "TaxSummary"). If the template name is not found,
        /// implementations should fall back to a sensible default.
        /// </param>
        /// <returns>
        /// A task that resolves to the rendered invoice HTML. The HTML should include
        /// any inline styles or references required to produce a faithful PDF if needed.
        /// </returns>
        Task<string> RenderInvoiceHtmlAsync(InvoicePdfDto model, string templateName = "Default");
    }
}

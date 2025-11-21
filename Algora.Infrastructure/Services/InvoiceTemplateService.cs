using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using RazorLight;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Renders invoice templates to HTML using RazorLight.
/// <para>
/// Templates are expected to be embedded resources under the assembly that contains
/// this type. The service compiles and caches templates in memory for fast rendering.
/// </para>
/// </summary>
public class InvoiceTemplateService : IInvoiceTemplateService
{
    private readonly RazorLightEngine _engine;
    private readonly ILogger<InvoiceTemplateService> _logger;

    /// <summary>
    /// Creates a new <see cref="InvoiceTemplateService"/> instance.
    /// Initializes a <see cref="RazorLightEngine"/> that loads templates from embedded resources
    /// and uses an in-memory cache for compiled templates.
    /// </summary>
    /// <param name="logger">Logger used to record rendering errors and diagnostics.</param>
    public InvoiceTemplateService(ILogger<InvoiceTemplateService> logger)
    {
        _logger = logger;
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(InvoiceTemplateService))
            .UseMemoryCachingProvider()
            .Build();
    }

    /// <summary>
    /// Renders the specified invoice model using the named template and returns the resulting HTML.
    /// </summary>
    /// <param name="model">The invoice model used to populate the template.</param>
    /// <param name="templateName">
    /// Optional template name. Defaults to "Default".
    /// The method composes an embedded-resource key using the pattern:
    /// "Algora.Web.Views.InvoiceTemplates.{templateName}.cshtml".
    /// Ensure the template file is embedded in the assembly and matches this path.
    /// </param>
    /// <returns>Rendered HTML string.</returns>
    /// <exception cref="Exception">Re-throws exceptions after logging; callers should handle rendering failures.</exception>
    public async Task<string> RenderInvoiceHtmlAsync(InvoicePdfDto model, string templateName = "Default")
    {
        try
        {
            // Compose the resource key used by RazorLight when loading an embedded template.
            // Example key: "Algora.Web.Views.InvoiceTemplates.Default.cshtml"
            string templateKey = $"Algora.Web.Views.InvoiceTemplates.{templateName}.cshtml";

            // Compile (if not cached) and render the template with the provided model.
            string html = await _engine.CompileRenderAsync(templateKey, model);
            return html;
        }
        catch (Exception ex)
        {
            // Log the error with the template name to aid troubleshooting, then rethrow.
            _logger.LogError(ex, "Failed to render invoice template {TemplateName}", templateName);
            throw;
        }
    }
}

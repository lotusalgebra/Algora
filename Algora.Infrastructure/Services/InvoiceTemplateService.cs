using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using RazorLight;

namespace Algora.Infrastructure.Services;

public class InvoiceTemplateService : IInvoiceTemplateService
{
    private readonly RazorLightEngine _engine;
    private readonly ILogger<InvoiceTemplateService> _logger;

    public InvoiceTemplateService(ILogger<InvoiceTemplateService> logger)
    {
        _logger = logger;
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(InvoiceTemplateService))
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderInvoiceHtmlAsync(InvoicePdfDto model, string templateName = "Default")
    {
        try
        {
            string templateKey = $"Algora.Web.Views.InvoiceTemplates.{templateName}.cshtml";
            string html = await _engine.CompileRenderAsync(templateKey, model);
            return html;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render invoice template {TemplateName}", templateName);
            throw;
        }
    }
}

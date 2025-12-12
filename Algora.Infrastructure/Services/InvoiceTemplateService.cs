using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Renders invoice templates to HTML using the application's Razor view engine.
/// This supports Razor Pages and standard Views and does not require runtime Roslyn metadata.
/// </summary>
public class InvoiceTemplateService : IInvoiceTemplateService
{
    private readonly ILogger<InvoiceTemplateService> _logger;
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _contentRoot;

    public InvoiceTemplateService(
        ILogger<InvoiceTemplateService> logger,
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IWebHostEnvironment env,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _scopeFactory = scopeFactory;
        _contentRoot = env.ContentRootPath ?? AppContext.BaseDirectory;
    }

    public async Task<string> RenderInvoiceHtmlAsync(InvoicePdfDto model, string templateName = "Default")
    {
        
        if (model is null) throw new ArgumentNullException(nameof(model));
        templateName ??= "Default";

        var candidates = new[]
        {
            $"~/Pages/InvoiceTemplates/{templateName}.cshtml",
            $"~/Pages/{templateName}.cshtml",
            $"~/Views/InvoiceTemplates/{templateName}.cshtml",
            $"~/Views/{templateName}.cshtml"
        };

        // Create a scope so scoped MVC services (IViewBufferScope, etc.) are available.    
        using var scope = _scopeFactory.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var httpContext = new DefaultHttpContext { RequestServices = scopedProvider };
        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        foreach (var virtualPath in candidates)
        {
            var getViewResult = _viewEngine.GetView(executingFilePath: null, viewPath: virtualPath, isMainPage: true);
            if (getViewResult.Success)
            {
                _logger.LogDebug("Rendering invoice template from view path: {Path}", virtualPath);
                return await RenderViewAsync(getViewResult.View, actionContext, model);
            }

            // When using FindView, pass a logical name (use Path here to attempt conventional lookup)
            var findViewResult = _viewEngine.FindView(actionContext, virtualPath, isMainPage: true);
            if (findViewResult.Success)
            {
                _logger.LogDebug("Rendering invoice template found by FindView: {Path}", virtualPath);
                return await RenderViewAsync(findViewResult.View, actionContext, model);
            }
        }

        _logger.LogError("Invoice template '{TemplateName}' not found. Attempted paths: {Paths}", templateName, string.Join(", ", candidates));
        throw new FileNotFoundException($"Invoice template '{templateName}' not found. Attempted: {string.Join(", ", candidates)}");
    }

    private async Task<string> RenderViewAsync(IView view, Microsoft.AspNetCore.Mvc.ActionContext actionContext, object model)
    {
        await using var sw = new StringWriter(new StringBuilder());
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };

        var tempData = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);

        var viewContext = new ViewContext(
            actionContext,
            view,
            viewData,
            tempData,
            sw,
            new HtmlHelperOptions()
        );

        await view.RenderAsync(viewContext);
        return sw.ToString();
    }
}

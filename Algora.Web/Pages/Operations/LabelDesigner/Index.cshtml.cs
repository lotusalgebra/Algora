using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.Operations.LabelDesigner;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ILabelDesignerService _labelDesignerService;
    private readonly IShopContext _shopContext;
    private readonly AppDbContext _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ILabelDesignerService labelDesignerService,
        IShopContext shopContext,
        AppDbContext db,
        ILogger<IndexModel> logger)
    {
        _labelDesignerService = labelDesignerService;
        _shopContext = shopContext;
        _db = db;
        _logger = logger;
    }

    public List<LabelTemplateDto> Templates { get; set; } = new();
    public List<LabelSizeConfig> AveryPresets { get; set; } = new();
    public List<LabelSizeConfig> ThermalPresets { get; set; } = new();
    public List<AvailableLabelField> AvailableFields { get; set; } = new();
    public List<ProductVariantInfo> Products { get; set; } = new();

    public class ProductVariantInfo
    {
        public int ProductId { get; set; }
        public int VariantId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public string? VariantTitle { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
    }

    public async Task OnGetAsync()
    {
        var templatesTask = _labelDesignerService.GetTemplatesAsync(_shopContext.ShopDomain);
        var productsTask = LoadProductsAsync();

        await Task.WhenAll(templatesTask, productsTask);

        Templates = (await templatesTask).ToList();
        Products = await productsTask;

        AveryPresets = new List<LabelSizeConfig>
        {
            LabelSizeConfig.GetPreset(LabelType.Avery5160),
            LabelSizeConfig.GetPreset(LabelType.Avery5163),
            LabelSizeConfig.GetPreset(LabelType.Avery5164),
            LabelSizeConfig.GetPreset(LabelType.Avery5167),
            LabelSizeConfig.GetPreset(LabelType.Avery5195)
        };

        ThermalPresets = new List<LabelSizeConfig>
        {
            LabelSizeConfig.GetPreset(LabelType.Thermal4x6),
            LabelSizeConfig.GetPreset(LabelType.Thermal2x1),
            LabelSizeConfig.GetPreset(LabelType.Thermal3x2),
            LabelSizeConfig.GetPreset(LabelType.Thermal2_25x1_25)
        };

        AvailableFields = _labelDesignerService.GetAvailableFields().ToList();
    }

    private async Task<List<ProductVariantInfo>> LoadProductsAsync()
    {
        return await _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.Product!.ShopDomain == _shopContext.ShopDomain && v.Product.IsActive)
            .OrderBy(v => v.Product!.Title)
            .ThenBy(v => v.Title)
            .Take(100)
            .Select(v => new ProductVariantInfo
            {
                ProductId = v.ProductId,
                VariantId = v.Id,
                ProductTitle = v.Product!.Title,
                VariantTitle = v.Title,
                SKU = v.Sku,
                Barcode = v.Barcode,
                Price = v.Price
            })
            .ToListAsync();
    }

    // API Endpoints

    public async Task<IActionResult> OnGetTemplateAsync(int id)
    {
        var template = await _labelDesignerService.GetTemplateByIdAsync(_shopContext.ShopDomain, id);
        if (template == null) return NotFound();
        return new JsonResult(template);
    }

    public async Task<IActionResult> OnPostSaveTemplateAsync([FromBody] CreateLabelTemplateRequest request)
    {
        try
        {
            var template = await _labelDesignerService.CreateTemplateAsync(_shopContext.ShopDomain, request);
            return new JsonResult(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving template");
            return BadRequest(new { error = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostUpdateTemplateAsync([FromBody] UpdateLabelTemplateRequest request)
    {
        try
        {
            var template = await _labelDesignerService.UpdateTemplateAsync(_shopContext.ShopDomain, request);
            return new JsonResult(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template");
            return BadRequest(new { error = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostDeleteTemplateAsync(int id)
    {
        var success = await _labelDesignerService.DeleteTemplateAsync(_shopContext.ShopDomain, id);
        return new JsonResult(new { success });
    }

    public async Task<IActionResult> OnGetPreviewDataAsync(int productId, int? variantId)
    {
        var data = await _labelDesignerService.GetPreviewDataAsync(_shopContext.ShopDomain, productId, variantId);
        if (data == null) return NotFound();
        return new JsonResult(data);
    }

    public async Task<IActionResult> OnPostGeneratePdfAsync([FromBody] GenerateLabelsRequest request)
    {
        try
        {
            var result = await _labelDesignerService.GenerateLabelsPdfAsync(_shopContext.ShopDomain, request);

            if (!result.Success || result.PdfData == null)
            {
                return BadRequest(new { error = result.Error ?? "Failed to generate PDF" });
            }

            return File(result.PdfData, "application/pdf", $"labels-{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating labels PDF");
            return BadRequest(new { error = ex.Message });
        }
    }
}

using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.Operations.Barcodes;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IBarcodeService _barcodeService;
    private readonly IShopContext _shopContext;
    private readonly AppDbContext _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IBarcodeService barcodeService,
        IShopContext shopContext,
        AppDbContext db,
        ILogger<IndexModel> logger)
    {
        _barcodeService = barcodeService;
        _shopContext = shopContext;
        _db = db;
        _logger = logger;
    }

    [BindProperty]
    public GenerateInput Input { get; set; } = new();

    public List<SelectListItem> Products { get; set; } = new();
    public BarcodeDto? GeneratedBarcode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public List<LabelSizeConfig> AveryPresets { get; set; } = new();
    public List<LabelSizeConfig> ThermalPresets { get; set; } = new();

    public class GenerateInput
    {
        public BarcodeFormat Format { get; set; } = BarcodeFormat.EAN13;
        public string? Value { get; set; }
        public string? Prefix { get; set; }
        public int Width { get; set; } = 300;
        public int Height { get; set; } = 100;
        public bool IncludeText { get; set; } = true;

        // For assigning to product
        public int? ProductVariantId { get; set; }

        // Label printing options
        public LabelType LabelType { get; set; } = LabelType.Avery5163;
    }

    public async Task OnGetAsync()
    {
        await LoadProductsAsync();
        LoadLabelPresets();
    }

    private void LoadLabelPresets()
    {
        AveryPresets = LabelSizeConfig.GetAveryPresets().ToList();
        ThermalPresets = LabelSizeConfig.GetThermalPresets().ToList();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        try
        {
            var dto = new GenerateBarcodeDto(
                Input.Format,
                Input.Value,
                Input.Prefix,
                Input.Width,
                Input.Height,
                Input.IncludeText
            );

            GeneratedBarcode = await _barcodeService.GenerateBarcodeAsync(dto);
            SuccessMessage = $"Barcode generated: {GeneratedBarcode.Value}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating barcode");
            ErrorMessage = "Failed to generate barcode. Please try again.";
        }

        await LoadProductsAsync();
        LoadLabelPresets();
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(int variantId, string barcode)
    {
        try
        {
            var success = await _barcodeService.AssignBarcodeToVariantAsync(variantId, barcode);
            if (success)
            {
                TempData["SuccessMessage"] = $"Barcode {barcode} assigned to product.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to assign barcode. It may already be in use.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning barcode");
            TempData["ErrorMessage"] = "Failed to assign barcode.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGenerateBulkAsync(int[] productIds)
    {
        try
        {
            var results = await _barcodeService.GenerateBarcodesForProductsAsync(_shopContext.ShopDomain, productIds);
            TempData["SuccessMessage"] = $"Generated {results.Count()} barcodes.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating bulk barcodes");
            TempData["ErrorMessage"] = "Failed to generate barcodes.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGenerateLabelsAsync(LabelType labelType = LabelType.Avery5163)
    {
        try
        {
            // Get products with barcodes
            var variants = await _db.ProductVariants
                .Include(v => v.Product)
                .Where(v => v.Product!.ShopDomain == _shopContext.ShopDomain && !string.IsNullOrEmpty(v.Barcode))
                .Take(100)
                .ToListAsync();

            var labels = variants.Select(v => new BarcodeLabelDto(
                v.Barcode!,
                v.Product!.Title,
                v.Sku,
                v.Price,
                null,
                1
            )).ToList();

            if (labels.Count == 0)
            {
                TempData["ErrorMessage"] = "No products with barcodes found.";
                return RedirectToPage();
            }

            // Get label configuration
            var labelConfig = LabelSizeConfig.GetPreset(labelType);

            // Create layout from label config
            var layout = new LabelLayoutDto(
                LabelSize.Custom,
                labelConfig.LabelsPerRow,
                labelConfig.RowsPerPage,
                labelConfig.MarginTopInches * 25.4f,  // Convert to mm
                labelConfig.MarginLeftInches * 25.4f,
                labelConfig.WidthMm,
                labelConfig.HeightMm,
                labelConfig.HorizontalGapInches * 25.4f,
                labelConfig.VerticalGapInches * 25.4f,
                true,  // ShowPrice
                true,  // ShowSku
                true   // ShowProductTitle
            );

            var pdf = await _barcodeService.GenerateBulkLabelsPdfAsync(labels, layout);

            var fileName = $"barcode-labels-{labelConfig.Type}.pdf";
            return File(pdf, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating labels PDF");
            TempData["ErrorMessage"] = "Failed to generate labels.";
            return RedirectToPage();
        }
    }

    private async Task LoadProductsAsync()
    {
        var variants = await _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.Product!.ShopDomain == _shopContext.ShopDomain)
            .OrderBy(v => v.Product!.Title)
            .ThenBy(v => v.Title)
            .Take(200)
            .ToListAsync();

        Products = variants.Select(v => new SelectListItem
        {
            Value = v.Id.ToString(),
            Text = string.IsNullOrEmpty(v.Title) || v.Title == "Default Title"
                ? $"{v.Product!.Title} {(string.IsNullOrEmpty(v.Barcode) ? "" : $"[{v.Barcode}]")}"
                : $"{v.Product!.Title} - {v.Title} {(string.IsNullOrEmpty(v.Barcode) ? "" : $"[{v.Barcode}]")}"
        }).ToList();
    }
}

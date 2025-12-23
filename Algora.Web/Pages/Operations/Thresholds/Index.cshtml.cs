using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.Operations.Thresholds;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ILocationService _locationService;
    private readonly ISupplierService _supplierService;
    private readonly IShopContext _shopContext;
    private readonly AppDbContext _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ILocationService locationService,
        ISupplierService supplierService,
        IShopContext shopContext,
        AppDbContext db,
        ILogger<IndexModel> logger)
    {
        _locationService = locationService;
        _supplierService = supplierService;
        _shopContext = shopContext;
        _db = db;
        _logger = logger;
    }

    public List<ProductThresholdViewModel> Products { get; set; } = new();
    public List<SelectListItem> Suppliers { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public ThresholdInput Input { get; set; } = new();

    public class ProductThresholdViewModel
    {
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? VariantTitle { get; set; }
        public string? Sku { get; set; }
        public int CurrentStock { get; set; }
        public ProductInventoryThresholdDto? Threshold { get; set; }
    }

    public class ThresholdInput
    {
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int? LowStockThreshold { get; set; }
        public int? CriticalStockThreshold { get; set; }
        public int? ReorderPoint { get; set; }
        public int? ReorderQuantity { get; set; }
        public int? SafetyStockDays { get; set; }
        public int? LeadTimeDays { get; set; }
        public int? PreferredSupplierId { get; set; }
        public bool AutoReorderEnabled { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        try
        {
            var dto = new SetProductThresholdDto(
                _shopContext.ShopDomain,
                Input.ProductId,
                Input.ProductVariantId,
                Input.LowStockThreshold,
                Input.CriticalStockThreshold,
                Input.ReorderPoint,
                Input.ReorderQuantity,
                Input.SafetyStockDays,
                Input.LeadTimeDays,
                Input.PreferredSupplierId,
                Input.AutoReorderEnabled
            );

            await _locationService.SetProductThresholdAsync(dto);
            TempData["SuccessMessage"] = "Threshold settings saved.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving threshold");
            TempData["ErrorMessage"] = "Failed to save threshold settings.";
        }

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        if (TempData["SuccessMessage"] != null)
            SuccessMessage = TempData["SuccessMessage"]?.ToString();
        if (TempData["ErrorMessage"] != null)
            ErrorMessage = TempData["ErrorMessage"]?.ToString();

        var shopDomain = _shopContext.ShopDomain;

        // Load suppliers
        var suppliers = await _supplierService.GetSuppliersAsync(shopDomain);
        Suppliers = suppliers.Where(s => s.IsActive)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToList();

        // Load product variants with thresholds
        var variants = await _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.Product!.ShopDomain == shopDomain)
            .OrderBy(v => v.Product!.Title)
            .ThenBy(v => v.Title)
            .Take(100)
            .ToListAsync();

        var thresholds = await _db.ProductInventoryThresholds
            .Include(t => t.PreferredSupplier)
            .Where(t => t.ShopDomain == shopDomain)
            .ToDictionaryAsync(t => (t.ProductId, t.ProductVariantId));

        Products = variants.Select(v =>
        {
            var hasThreshold = thresholds.TryGetValue((v.ProductId, v.Id), out var t);
            return new ProductThresholdViewModel
            {
                ProductId = v.ProductId,
                ProductVariantId = v.Id,
                Title = v.Product!.Title,
                VariantTitle = (string.IsNullOrEmpty(v.Title) || v.Title == "Default Title") ? null : v.Title,
                Sku = v.Sku,
                CurrentStock = v.InventoryQuantity,
                Threshold = hasThreshold && t != null
                    ? new ProductInventoryThresholdDto(
                        t.Id,
                        t.ShopDomain,
                        t.ProductId,
                        v.Product.Title,
                        t.ProductVariantId,
                        v.Title,
                        t.LowStockThreshold,
                        t.CriticalStockThreshold,
                        t.ReorderPoint,
                        t.ReorderQuantity,
                        t.SafetyStockDays,
                        t.LeadTimeDays,
                        t.PreferredSupplierId,
                        t.PreferredSupplier?.Name,
                        t.AutoReorderEnabled,
                        t.CreatedAt,
                        t.UpdatedAt
                    )
                    : null
            };
        }).ToList();
    }
}

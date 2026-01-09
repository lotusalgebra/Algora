using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Algora.Web.Pages.Operations.PurchaseOrders;

[Authorize]
[RequireFeature(FeatureCodes.PurchaseOrders)]
public class CreateModel : PageModel
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ISupplierService _supplierService;
    private readonly ILocationService _locationService;
    private readonly IShopContext _shopContext;
    private readonly AppDbContext _db;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IPurchaseOrderService purchaseOrderService,
        ISupplierService supplierService,
        ILocationService locationService,
        IShopContext shopContext,
        AppDbContext db,
        ILogger<CreateModel> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _supplierService = supplierService;
        _locationService = locationService;
        _shopContext = shopContext;
        _db = db;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> Suppliers { get; set; } = new();
    public List<SelectListItem> Locations { get; set; } = new();
    public List<SelectListItem> Products { get; set; } = new();
    public List<SuggestedPurchaseOrderDto> SuggestedOrders { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        public int SupplierId { get; set; }

        public int? LocationId { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public List<LineItemInput> Lines { get; set; } = new();
    }

    public class LineItemInput
    {
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int QuantityOrdered { get; set; } = 1;
        public decimal UnitCost { get; set; }
    }

    public async Task OnGetAsync(int? supplierId = null)
    {
        await LoadDropdownsAsync();

        if (supplierId.HasValue)
        {
            Input.SupplierId = supplierId.Value;
        }

        // Get suggested orders for display
        SuggestedOrders = (await _purchaseOrderService.GenerateSuggestedOrdersAsync(_shopContext.ShopDomain)).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadDropdownsAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.Lines.Count == 0 || Input.Lines.All(l => l.QuantityOrdered <= 0))
        {
            ErrorMessage = "Please add at least one line item.";
            return Page();
        }

        try
        {
            var dto = new CreatePurchaseOrderDto(
                _shopContext.ShopDomain,
                Input.SupplierId,
                Input.LocationId,
                Input.Notes,
                Input.ExpectedDeliveryDate,
                Input.Lines.Where(l => l.QuantityOrdered > 0).Select(l => new CreatePurchaseOrderLineDto(
                    l.ProductId,
                    l.ProductVariantId,
                    l.QuantityOrdered,
                    l.UnitCost
                )).ToList()
            );

            var order = await _purchaseOrderService.CreatePurchaseOrderAsync(dto);
            TempData["SuccessMessage"] = $"Purchase order {order.OrderNumber} created successfully.";
            return RedirectToPage("Details", new { id = order.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase order");
            ErrorMessage = "Failed to create purchase order. Please try again.";
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        var shopDomain = _shopContext.ShopDomain;

        // Load suppliers
        var suppliers = await _supplierService.GetSuppliersAsync(shopDomain);
        Suppliers = suppliers.Where(s => s.IsActive)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToList();

        // Load locations
        var locations = await _locationService.GetLocationsAsync(shopDomain);
        Locations = locations.Where(l => l.IsActive)
            .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
            .ToList();

        // Load products with variants via join
        var variants = await _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.Product!.ShopDomain == shopDomain)
            .OrderBy(v => v.Product!.Title)
            .ThenBy(v => v.Title)
            .Take(500)
            .ToListAsync();

        Products = variants.Select(v => new SelectListItem
        {
            Value = $"{v.ProductId}|{v.Id}",
            Text = string.IsNullOrEmpty(v.Title) || v.Title == "Default Title"
                ? $"{v.Product!.Title} (${v.Price:N2})"
                : $"{v.Product!.Title} - {v.Title} (${v.Price:N2})"
        }).ToList();
    }
}

using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.Operations.PackingSlips;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IPackingSlipService _packingSlipService;
    private readonly IShopContext _shopContext;
    private readonly AppDbContext _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPackingSlipService packingSlipService,
        IShopContext shopContext,
        AppDbContext db,
        ILogger<IndexModel> logger)
    {
        _packingSlipService = packingSlipService;
        _shopContext = shopContext;
        _db = db;
        _logger = logger;
    }

    public List<OrderSummary> RecentOrders { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public PackingSlipSettings Settings { get; set; } = new();

    public class OrderSummary
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
        public string? FulfillmentStatus { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadRecentOrdersAsync();
    }

    public async Task<IActionResult> OnPostGenerateAsync(int orderId)
    {
        try
        {
            var result = await _packingSlipService.GeneratePackingSlipAsync(orderId, Settings);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to generate packing slip.";
                return RedirectToPage();
            }

            return File(result.PdfData, "application/pdf", $"packing-slip-{result.OrderNumber}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating packing slip for order {OrderId}", orderId);
            TempData["ErrorMessage"] = "Failed to generate packing slip.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostGenerateBulkAsync(int[] orderIds)
    {
        try
        {
            if (orderIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select at least one order.";
                return RedirectToPage();
            }

            var result = await _packingSlipService.GenerateBulkPackingSlipsAsync(orderIds, Settings, true);

            if (result.SuccessCount == 0)
            {
                TempData["ErrorMessage"] = "No packing slips could be generated.";
                return RedirectToPage();
            }

            if (result.CombinedPdfData != null && result.CombinedPdfData.Length > 0)
            {
                return File(result.CombinedPdfData, "application/pdf", $"packing-slips-{DateTime.Now:yyyyMMdd}.pdf");
            }

            TempData["ErrorMessage"] = "Failed to generate combined PDF.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating bulk packing slips");
            TempData["ErrorMessage"] = "Failed to generate packing slips.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnGetPreviewAsync(int orderId)
    {
        try
        {
            var data = await _packingSlipService.GetPackingSlipDataAsync(orderId);
            if (data == null)
            {
                return NotFound();
            }

            return new JsonResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting packing slip preview for order {OrderId}", orderId);
            return BadRequest();
        }
    }

    private async Task LoadRecentOrdersAsync()
    {
        RecentOrders = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .Where(o => o.ShopDomain == _shopContext.ShopDomain)
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .Select(o => new OrderSummary
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber ?? $"#{o.Id}",
                CreatedAt = o.CreatedAt,
                CustomerName = o.Customer != null ?
                    $"{o.Customer.FirstName} {o.Customer.LastName}".Trim() : "Customer",
                ItemCount = o.Lines.Count,
                Total = o.GrandTotal,
                FulfillmentStatus = o.FulfillmentStatus
            })
            .ToListAsync();
    }
}

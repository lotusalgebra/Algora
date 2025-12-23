using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Algora.Web.Pages.Operations.PurchaseOrders;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IPurchaseOrderService purchaseOrderService,
        ILogger<DetailsModel> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _logger = logger;
    }

    public PurchaseOrderDto? Order { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public ReceiveInput ReceiveData { get; set; } = new();

    public class ReceiveInput
    {
        public List<ReceiveLineInput> Lines { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class ReceiveLineInput
    {
        public int LineId { get; set; }
        public int QuantityReceived { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
            if (TempData["ErrorMessage"] != null)
                ErrorMessage = TempData["ErrorMessage"]?.ToString();

            Order = await _purchaseOrderService.GetPurchaseOrderAsync(id);
            if (Order == null)
            {
                return NotFound();
            }

            // Initialize receive data with current quantities
            ReceiveData.Lines = Order.Lines.Select(l => new ReceiveLineInput
            {
                LineId = l.Id,
                QuantityReceived = l.QuantityOrdered - l.QuantityReceived
            }).ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading purchase order {OrderId}", id);
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostSendAsync(int id)
    {
        try
        {
            await _purchaseOrderService.SendToSupplierAsync(id);
            TempData["SuccessMessage"] = "Order sent to supplier.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending order {OrderId}", id);
            TempData["ErrorMessage"] = "Failed to send order.";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id, DateTime? expectedDelivery)
    {
        try
        {
            await _purchaseOrderService.MarkAsConfirmedAsync(id, expectedDelivery);
            TempData["SuccessMessage"] = "Order confirmed by supplier.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming order {OrderId}", id);
            TempData["ErrorMessage"] = "Failed to confirm order.";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostShippedAsync(int id, string? trackingNumber)
    {
        try
        {
            await _purchaseOrderService.MarkAsShippedAsync(id, trackingNumber);
            TempData["SuccessMessage"] = "Order marked as shipped.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking order {OrderId} as shipped", id);
            TempData["ErrorMessage"] = "Failed to mark order as shipped.";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReceiveAsync(int id)
    {
        try
        {
            var dto = new ReceiveItemsDto(
                ReceiveData.Lines.Select(l => new ReceiveLineDto(l.LineId, l.QuantityReceived)).ToList(),
                ReceiveData.Notes
            );

            await _purchaseOrderService.ReceiveItemsAsync(id, dto);
            TempData["SuccessMessage"] = "Items received and inventory updated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving items for order {OrderId}", id);
            TempData["ErrorMessage"] = "Failed to receive items.";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, string? reason)
    {
        try
        {
            await _purchaseOrderService.CancelPurchaseOrderAsync(id, reason ?? "Cancelled by user");
            TempData["SuccessMessage"] = "Order cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            TempData["ErrorMessage"] = "Failed to cancel order.";
        }
        return RedirectToPage(new { id });
    }
}

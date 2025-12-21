using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Orders
{
    public class DeleteModel : PageModel
    {
        private readonly IShopifyOrderService _orderService;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(IShopifyOrderService orderService, ILogger<DeleteModel> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        public OrderDto? Order { get; set; }
        public string? ErrorMessage { get; set; }

        [BindProperty]
        public string Action { get; set; } = "close";

        public async Task<IActionResult> OnGetAsync(long id)
        {
            try
            {
                Order = await _orderService.GetByIdAsync(id);
                if (Order == null)
                {
                    return NotFound();
                }
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order {OrderId} for deletion", id);
                ErrorMessage = $"Error loading order: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            try
            {
                var order = await _orderService.GetByIdAsync(id);
                var orderName = order?.Name ?? $"#{id}";

                if (Action == "cancel")
                {
                    await _orderService.CancelAsync(id);
                    _logger.LogInformation("Order {OrderId} cancelled successfully", id);
                    TempData["SuccessMessage"] = $"Order {orderName} cancelled successfully!";
                }
                else
                {
                    await _orderService.CloseAsync(id);
                    _logger.LogInformation("Order {OrderId} closed successfully", id);
                    TempData["SuccessMessage"] = $"Order {orderName} closed successfully!";
                }

                return RedirectToPage("/Orders/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order {OrderId}", id);
                ErrorMessage = $"Error processing order: {ex.Message}";

                // Reload order for display
                try
                {
                    Order = await _orderService.GetByIdAsync(id);
                }
                catch { }

                return Page();
            }
        }
    }
}

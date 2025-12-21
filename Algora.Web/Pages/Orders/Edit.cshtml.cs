using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Algora.Web.Pages.Orders
{
    public class EditModel : PageModel
    {
        private readonly IShopifyOrderService _orderService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(IShopifyOrderService orderService, ILogger<EditModel> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [BindProperty]
        public OrderEditInput Order { get; set; } = new();

        public OrderDto? OriginalOrder { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            try
            {
                OriginalOrder = await _orderService.GetByIdAsync(id);
                if (OriginalOrder == null)
                {
                    return NotFound();
                }

                Order = new OrderEditInput
                {
                    Id = OriginalOrder.Id,
                    Name = OriginalOrder.Name,
                    Email = OriginalOrder.Email,
                    Note = OriginalOrder.Note,
                    Tags = OriginalOrder.Tags,
                    FinancialStatus = OriginalOrder.FinancialStatus,
                    FulfillmentStatus = OriginalOrder.FulfillmentStatus,
                    ShippingName = OriginalOrder.ShippingAddress?.Name,
                    ShippingAddress1 = OriginalOrder.ShippingAddress?.Address1,
                    ShippingAddress2 = OriginalOrder.ShippingAddress?.Address2,
                    ShippingCity = OriginalOrder.ShippingAddress?.City,
                    ShippingProvince = OriginalOrder.ShippingAddress?.Province,
                    ShippingCountry = OriginalOrder.ShippingAddress?.Country,
                    ShippingZip = OriginalOrder.ShippingAddress?.Zip,
                    ShippingPhone = OriginalOrder.ShippingAddress?.Phone
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order {OrderId}", id);
                ErrorMessage = $"Error loading order: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload original order for display
                try { OriginalOrder = await _orderService.GetByIdAsync(Order.Id); } catch { }
                return Page();
            }

            try
            {
                var input = new UpdateOrderInput
                {
                    OrderId = Order.Id,
                    Email = Order.Email,
                    Note = Order.Note,
                    Tags = Order.Tags,
                    ShippingName = Order.ShippingName,
                    ShippingAddress1 = Order.ShippingAddress1,
                    ShippingAddress2 = Order.ShippingAddress2,
                    ShippingCity = Order.ShippingCity,
                    ShippingProvince = Order.ShippingProvince,
                    ShippingCountry = Order.ShippingCountry,
                    ShippingZip = Order.ShippingZip,
                    ShippingPhone = Order.ShippingPhone
                };

                var updatedOrder = await _orderService.UpdateAsync(input);
                _logger.LogInformation("Order updated successfully: {OrderId}", Order.Id);

                TempData["SuccessMessage"] = $"Order {updatedOrder?.Name ?? "#" + Order.Id} updated successfully!";
                return RedirectToPage("/Orders/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", Order.Id);
                ErrorMessage = $"Error updating order: {ex.Message}";

                // Reload original order for display
                try { OriginalOrder = await _orderService.GetByIdAsync(Order.Id); } catch { }
                return Page();
            }
        }
    }

    public class OrderEditInput
    {
        public long Id { get; set; }
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }

        public string? Note { get; set; }
        public string? Tags { get; set; }
        public string? FinancialStatus { get; set; }
        public string? FulfillmentStatus { get; set; }

        // Shipping Address
        public string? ShippingName { get; set; }
        public string? ShippingAddress1 { get; set; }
        public string? ShippingAddress2 { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingProvince { get; set; }
        public string? ShippingCountry { get; set; }
        public string? ShippingZip { get; set; }
        public string? ShippingPhone { get; set; }
    }
}

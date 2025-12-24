using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = Algora.Domain.Entities.Customer;
using OrderEntity = Algora.Domain.Entities.Order;
using OrderLineEntity = Algora.Domain.Entities.OrderLine;
using FulfillmentEntity = Algora.Domain.Entities.Fulfillment;

namespace Algora.Web.Pages.CustomerHub.Portal;

public class OrdersModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IShopContext _shopContext;
    private readonly ILogger<OrdersModel> _logger;

    public OrdersModel(
        AppDbContext context,
        IShopContext shopContext,
        ILogger<OrdersModel> logger)
    {
        _context = context;
        _shopContext = shopContext;
        _logger = logger;
    }

    public CustomerEntity? Customer { get; set; }
    public List<OrderWithFulfillment> Orders { get; set; } = new();
    public OrderWithFulfillment? SelectedOrder { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? OrderId { get; set; }

    public class OrderWithFulfillment
    {
        public OrderEntity Order { get; set; } = null!;
        public List<OrderLineEntity> OrderLines { get; set; } = new();
        public List<FulfillmentEntity> Fulfillments { get; set; } = new();
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Email))
        {
            return RedirectToPage("./Index");
        }

        await LoadDataAsync();
        return Page();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;

            Customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.Email == Email);

            if (Customer == null)
            {
                ErrorMessage = "Customer not found.";
                return;
            }

            var orders = await _context.Orders
                .Where(o => o.ShopDomain == shopDomain && o.CustomerId == Customer.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            foreach (var order in orders)
            {
                var orderLines = await _context.OrderLines
                    .Where(ol => ol.OrderId == order.Id)
                    .ToListAsync();

                var fulfillments = await _context.Fulfillments
                    .Where(f => f.OrderId == order.Id)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                Orders.Add(new OrderWithFulfillment
                {
                    Order = order,
                    OrderLines = orderLines,
                    Fulfillments = fulfillments
                });
            }

            // Load selected order details
            if (OrderId.HasValue)
            {
                SelectedOrder = Orders.FirstOrDefault(o => o.Order.Id == OrderId.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading orders for {Email}", Email);
            ErrorMessage = "Unable to load orders. Please try again.";
        }
    }

    public string GetStatusColor(string? status) => status?.ToLower() switch
    {
        "fulfilled" => "bg-green-100 text-green-700",
        "partial" => "bg-yellow-100 text-yellow-700",
        "unfulfilled" => "bg-orange-100 text-orange-700",
        "paid" => "bg-green-100 text-green-700",
        "pending" => "bg-yellow-100 text-yellow-700",
        "refunded" => "bg-red-100 text-red-700",
        "success" => "bg-green-100 text-green-700",
        "in_transit" => "bg-blue-100 text-blue-700",
        "delivered" => "bg-green-100 text-green-700",
        _ => "bg-gray-100 text-gray-700"
    };

    public string GetShipmentIcon(string? status) => status?.ToLower() switch
    {
        "pending" => "fas fa-clock",
        "in_transit" => "fas fa-truck",
        "out_for_delivery" => "fas fa-truck",
        "delivered" => "fas fa-check-circle",
        "success" => "fas fa-check-circle",
        _ => "fas fa-box"
    };
}

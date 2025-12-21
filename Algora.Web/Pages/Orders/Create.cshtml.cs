using Algora.Application.DTOs;
using Algora.Application.DTOs.Order;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Algora.Web.Pages.Orders
{
    public class CreateModel : PageModel
    {
        private readonly IShopifyOrderService _orderService;
        private readonly ShopifyProductGraphService _productService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(IShopifyOrderService orderService, ShopifyProductGraphService productService, ILogger<CreateModel> logger)
        {
            _orderService = orderService;
            _productService = productService;
            _logger = logger;
        }

        [BindProperty]
        public OrderInput Order { get; set; } = new();

        [BindProperty]
        public List<LineItemInput> LineItems { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        // Products loaded from Shopify for selection
        public List<ProductSelectItem> AvailableProducts { get; set; } = new();
        public string ProductsJson { get; set; } = "[]";

        public async Task OnGetAsync()
        {
            // Initialize with one default line item
            if (LineItems.Count == 0)
            {
                LineItems.Add(new LineItemInput());
            }

            // Load products from Shopify
            await LoadProductsAsync();
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                _logger.LogInformation("Loading products from Shopify using GetAllProductsAsync...");
                // Use GetAllProductsAsync which works on /products page
                var products = await _productService.GetAllProductsAsync();
                _logger.LogInformation("Loaded {Count} products from Shopify", products.Count);

                AvailableProducts = products.Select(p => new ProductSelectItem
                {
                    ProductId = p.Id,
                    Title = p.Title ?? "",
                    DisplayName = p.Title ?? "",
                    Price = p.Price,
                    Vendor = p.Vendor
                }).ToList();

                _logger.LogInformation("Created {Count} product select items", AvailableProducts.Count);

                // Serialize products to JSON for JavaScript
                ProductsJson = JsonSerializer.Serialize(AvailableProducts, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products for order creation");
                ErrorMessage = $"Error loading products: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadProductsAsync();
                return Page();
            }

            try
            {
                var billingAddress = new AddressDto
                {
                    Name = Order.BillingName,
                    Address1 = Order.BillingAddress1,
                    Address2 = Order.BillingAddress2,
                    City = Order.BillingCity,
                    Province = Order.BillingProvince,
                    Country = Order.BillingCountry,
                    Zip = Order.BillingZip,
                    Phone = Order.BillingPhone
                };

                var shippingAddress = Order.SameAsShipping ? billingAddress : new AddressDto
                {
                    Name = Order.ShippingName,
                    Address1 = Order.ShippingAddress1,
                    Address2 = Order.ShippingAddress2,
                    City = Order.ShippingCity,
                    Province = Order.ShippingProvince,
                    Country = Order.ShippingCountry,
                    Zip = Order.ShippingZip,
                    Phone = Order.ShippingPhone
                };

                var customer = new CustomerDto
                {
                    FirstName = Order.CustomerFirstName,
                    LastName = Order.CustomerLastName,
                    Email = Order.CustomerEmail,
                    Phone = Order.CustomerPhone
                };

                var lineItems = LineItems
                    .Where(li => !string.IsNullOrWhiteSpace(li.Title))
                    .Select(li => new LineItemDto
                    {
                        Title = li.Title,
                        Quantity = li.Quantity,
                        Price = li.Price,
                        VariantId = li.VariantId
                    }).ToList();

                var orderDto = new OrderDto
                {
                    Email = Order.CustomerEmail,
                    FinancialStatus = Order.FinancialStatus,
                    Customer = customer,
                    BillingAddress = billingAddress,
                    ShippingAddress = shippingAddress,
                    LineItems = lineItems,
                    TotalPrice = lineItems.Sum(li => li.Price * li.Quantity)
                };

                var createdOrder = await _orderService.CreateAsync(orderDto);
                _logger.LogInformation("Order created successfully: {OrderId}", createdOrder?.Id);

                TempData["SuccessMessage"] = $"Order {createdOrder?.Name ?? "#" + createdOrder?.Id} created successfully!";
                return RedirectToPage("/Orders/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                ErrorMessage = $"Error creating order: {ex.Message}";
                await LoadProductsAsync();
                return Page();
            }
        }
    }

    public class OrderInput
    {
        // Customer Information
        [Required(ErrorMessage = "First name is required")]
        public string CustomerFirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        public string CustomerLastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string CustomerEmail { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        // Billing Address
        public string? BillingName { get; set; }
        public string? BillingAddress1 { get; set; }
        public string? BillingAddress2 { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingProvince { get; set; }
        public string? BillingCountry { get; set; }
        public string? BillingZip { get; set; }
        public string? BillingPhone { get; set; }

        // Shipping Address
        public bool SameAsShipping { get; set; } = true;
        public string? ShippingName { get; set; }
        public string? ShippingAddress1 { get; set; }
        public string? ShippingAddress2 { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingProvince { get; set; }
        public string? ShippingCountry { get; set; }
        public string? ShippingZip { get; set; }
        public string? ShippingPhone { get; set; }

        // Order Status
        public string FinancialStatus { get; set; } = "pending";
    }

    public class LineItemInput
    {
        public string? Title { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        [Range(0, 999999.99, ErrorMessage = "Price must be between 0 and 999999.99")]
        public decimal Price { get; set; }

        public string? VariantId { get; set; }
        public long? ProductId { get; set; }
    }

    public class ProductSelectItem
    {
        public long ProductId { get; set; }
        public string? VariantId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? VariantTitle { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Sku { get; set; }
        public string? Vendor { get; set; }
    }
}

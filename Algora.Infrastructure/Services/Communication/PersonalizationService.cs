using System.Text.RegularExpressions;
using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Communication;

/// <summary>
/// Service for personalizing email/SMS content with dynamic tokens.
/// </summary>
public class PersonalizationService : IPersonalizationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PersonalizationService> _logger;

    private static readonly Regex TokenPattern = new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

    private static readonly Dictionary<string, PersonalizationTokenDto> AvailableTokens = new()
    {
        // Customer tokens
        ["customer.first_name"] = new("{{customer.first_name}}", "Customer's first name", "Customer", "John"),
        ["customer.last_name"] = new("{{customer.last_name}}", "Customer's last name", "Customer", "Doe"),
        ["customer.email"] = new("{{customer.email}}", "Customer's email address", "Customer", "john@example.com"),
        ["customer.total_spent"] = new("{{customer.total_spent}}", "Customer's total spent", "Customer", "$523.50"),
        ["customer.order_count"] = new("{{customer.order_count}}", "Customer's total order count", "Customer", "5"),

        // Order tokens
        ["order.number"] = new("{{order.number}}", "Order number", "Order", "#1234"),
        ["order.total"] = new("{{order.total}}", "Order total amount", "Order", "$99.99"),
        ["order.items"] = new("{{order.items}}", "Order line items", "Order", "Product A x2, Product B x1"),
        ["order.date"] = new("{{order.date}}", "Order date", "Order", "Dec 22, 2025"),

        // Cart tokens (for abandoned cart)
        ["cart.recovery_url"] = new("{{cart.recovery_url}}", "Cart recovery URL", "Cart", "https://shop.com/cart/recover/abc123"),
        ["cart.items"] = new("{{cart.items}}", "Cart items", "Cart", "Blue T-Shirt x1, Jeans x2"),
        ["cart.total"] = new("{{cart.total}}", "Cart total", "Cart", "$149.99"),
        ["cart.item_count"] = new("{{cart.item_count}}", "Number of items in cart", "Cart", "3"),

        // Shop tokens
        ["shop.name"] = new("{{shop.name}}", "Shop name", "Shop", "My Awesome Store"),
        ["shop.url"] = new("{{shop.url}}", "Shop URL", "Shop", "https://myawesomestore.myshopify.com"),

        // Date tokens
        ["date.today"] = new("{{date.today}}", "Today's date", "Date", "Dec 22, 2025"),
        ["date.year"] = new("{{date.year}}", "Current year", "Date", "2025"),
    };

    public PersonalizationService(AppDbContext db, ILogger<PersonalizationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<PersonalizationTokenDto> GetAvailableTokens()
    {
        return AvailableTokens.Values.ToList();
    }

    public async Task<string> PersonalizeContentAsync(string content, PersonalizationContextDto context)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var result = TokenPattern.Replace(content, match =>
        {
            var token = match.Groups[1].Value.ToLowerInvariant().Trim();
            return GetTokenValue(token, context);
        });

        return await Task.FromResult(result);
    }

    private string GetTokenValue(string token, PersonalizationContextDto context)
    {
        return token switch
        {
            // Customer tokens
            "customer.first_name" => context.CustomerFirstName ?? "Customer",
            "customer.last_name" => context.CustomerLastName ?? "",
            "customer.email" => context.CustomerEmail ?? "",
            "customer.total_spent" => context.CustomerTotalSpent?.ToString("C") ?? "$0.00",
            "customer.order_count" => context.CustomerOrderCount?.ToString() ?? "0",

            // Order tokens
            "order.number" => context.OrderNumber ?? "",
            "order.total" => context.OrderTotal?.ToString("C") ?? "",
            "order.items" => context.OrderItems ?? "",
            "order.date" => DateTime.UtcNow.ToString("MMM dd, yyyy"),

            // Cart tokens
            "cart.recovery_url" => context.CartRecoveryUrl ?? "",
            "cart.items" => context.CartItems ?? "",
            "cart.total" => context.CartTotal?.ToString("C") ?? "",
            "cart.item_count" => "0", // Would need to be calculated

            // Shop tokens
            "shop.name" => context.ShopName ?? "",
            "shop.url" => $"https://{context.ShopDomain}",

            // Date tokens
            "date.today" => DateTime.UtcNow.ToString("MMM dd, yyyy"),
            "date.year" => DateTime.UtcNow.Year.ToString(),

            // Custom tokens
            _ => context.CustomTokens?.GetValueOrDefault(token) ?? $"{{{{token}}}}"
        };
    }

    public async Task<PersonalizationContextDto> BuildContextForEnrollmentAsync(int enrollmentId)
    {
        var enrollment = await _db.EmailAutomationEnrollments
            .Include(e => e.Automation)
            .Include(e => e.Customer)
                .ThenInclude(c => c!.Orders)
            .Include(e => e.Order)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment == null)
            return new PersonalizationContextDto("", "", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == enrollment.Automation.ShopDomain);

        // Calculate customer stats from orders
        var customerTotalSpent = enrollment.Customer?.Orders?.Sum(o => o.GrandTotal) ?? 0;
        var customerOrderCount = enrollment.Customer?.Orders?.Count ?? 0;

        return new PersonalizationContextDto(
            ShopDomain: enrollment.Automation.ShopDomain,
            ShopName: shop?.ShopName ?? enrollment.Automation.ShopDomain,
            CustomerId: enrollment.CustomerId,
            CustomerEmail: enrollment.Email,
            CustomerFirstName: enrollment.Customer?.FirstName,
            CustomerLastName: enrollment.Customer?.LastName,
            CustomerTotalSpent: customerTotalSpent,
            CustomerOrderCount: customerOrderCount,
            OrderId: enrollment.OrderId,
            OrderNumber: enrollment.Order?.OrderNumber,
            OrderTotal: enrollment.Order?.GrandTotal,
            OrderItems: enrollment.Order != null ? await BuildOrderItemsString(enrollment.Order.Id) : null,
            CheckoutId: enrollment.AbandonedCheckoutId,
            CartRecoveryUrl: null, // Would need to be built from checkout data
            CartItems: null,
            CartTotal: null,
            CustomTokens: null
        );
    }

    public async Task<PersonalizationContextDto> BuildContextForAbandonedCartAsync(
        string shopDomain,
        AbandonedCartTriggerDto cartData)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);
        var customer = await _db.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.Email == cartData.Email);

        var cartItems = string.Join(", ", cartData.LineItems.Select(li =>
            string.IsNullOrEmpty(li.VariantTitle)
                ? $"{li.Title} x{li.Quantity}"
                : $"{li.Title} ({li.VariantTitle}) x{li.Quantity}"));

        // Calculate customer stats from orders
        var customerTotalSpent = customer?.Orders?.Sum(o => o.GrandTotal) ?? 0;
        var customerOrderCount = customer?.Orders?.Count ?? 0;

        return new PersonalizationContextDto(
            ShopDomain: shopDomain,
            ShopName: shop?.ShopName ?? shopDomain,
            CustomerId: customer?.Id,
            CustomerEmail: cartData.Email,
            CustomerFirstName: cartData.CustomerFirstName,
            CustomerLastName: cartData.CustomerLastName,
            CustomerTotalSpent: customerTotalSpent,
            CustomerOrderCount: customerOrderCount,
            OrderId: null,
            OrderNumber: null,
            OrderTotal: null,
            OrderItems: null,
            CheckoutId: cartData.CheckoutId,
            CartRecoveryUrl: cartData.RecoveryUrl,
            CartItems: cartItems,
            CartTotal: cartData.CartTotal,
            CustomTokens: null
        );
    }

    public async Task<PersonalizationContextDto> BuildContextForOrderAsync(
        string shopDomain,
        int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c!.Orders)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return new PersonalizationContextDto(shopDomain, "", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);
        var orderItems = await BuildOrderItemsString(orderId);

        // Calculate customer stats from orders
        var customerTotalSpent = order.Customer?.Orders?.Sum(o => o.GrandTotal) ?? 0;
        var customerOrderCount = order.Customer?.Orders?.Count ?? 0;

        return new PersonalizationContextDto(
            ShopDomain: shopDomain,
            ShopName: shop?.ShopName ?? shopDomain,
            CustomerId: order.CustomerId,
            CustomerEmail: order.CustomerEmail ?? order.Customer?.Email,
            CustomerFirstName: order.Customer?.FirstName,
            CustomerLastName: order.Customer?.LastName,
            CustomerTotalSpent: customerTotalSpent,
            CustomerOrderCount: customerOrderCount,
            OrderId: order.Id,
            OrderNumber: order.OrderNumber,
            OrderTotal: order.GrandTotal,
            OrderItems: orderItems,
            CheckoutId: null,
            CartRecoveryUrl: null,
            CartItems: null,
            CartTotal: null,
            CustomTokens: null
        );
    }

    public async Task<PersonalizationContextDto> BuildContextForCustomerAsync(
        string shopDomain,
        int customerId)
    {
        var customer = await _db.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null)
            return new PersonalizationContextDto(shopDomain, "", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);

        // Calculate customer stats from orders
        var customerTotalSpent = customer.Orders?.Sum(o => o.GrandTotal) ?? 0;
        var customerOrderCount = customer.Orders?.Count ?? 0;

        return new PersonalizationContextDto(
            ShopDomain: shopDomain,
            ShopName: shop?.ShopName ?? shopDomain,
            CustomerId: customer.Id,
            CustomerEmail: customer.Email,
            CustomerFirstName: customer.FirstName,
            CustomerLastName: customer.LastName,
            CustomerTotalSpent: customerTotalSpent,
            CustomerOrderCount: customerOrderCount,
            OrderId: null,
            OrderNumber: null,
            OrderTotal: null,
            OrderItems: null,
            CheckoutId: null,
            CartRecoveryUrl: null,
            CartItems: null,
            CartTotal: null,
            CustomTokens: null
        );
    }

    public List<string> ValidateTokens(string content)
    {
        var invalidTokens = new List<string>();
        var matches = TokenPattern.Matches(content);

        foreach (Match match in matches)
        {
            var token = match.Groups[1].Value.ToLowerInvariant().Trim();
            if (!AvailableTokens.ContainsKey(token))
            {
                invalidTokens.Add(match.Value);
            }
        }

        return invalidTokens;
    }

    public string PreviewWithSampleData(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        return TokenPattern.Replace(content, match =>
        {
            var token = match.Groups[1].Value.ToLowerInvariant().Trim();
            if (AvailableTokens.TryGetValue(token, out var tokenDto))
            {
                return tokenDto.SampleValue ?? match.Value;
            }
            return match.Value;
        });
    }

    private async Task<string> BuildOrderItemsString(int orderId)
    {
        var lines = await _db.OrderLines
            .Where(ol => ol.OrderId == orderId)
            .Select(ol => new { ol.ProductTitle, ol.VariantTitle, ol.Quantity })
            .ToListAsync();

        return string.Join(", ", lines.Select(l =>
            string.IsNullOrEmpty(l.VariantTitle)
                ? $"{l.ProductTitle} x{l.Quantity}"
                : $"{l.ProductTitle} ({l.VariantTitle}) x{l.Quantity}"));
    }
}

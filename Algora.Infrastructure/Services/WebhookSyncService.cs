using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for syncing Shopify webhook data to the local database.
/// </summary>
public class WebhookSyncService : IWebhookSyncService
{
    private readonly AppDbContext _db;
    private readonly ILogger<WebhookSyncService> _logger;
    private readonly ILoyaltyService _loyaltyService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public WebhookSyncService(AppDbContext db, ILogger<WebhookSyncService> logger, ILoyaltyService loyaltyService)
    {
        _db = db;
        _logger = logger;
        _loyaltyService = loyaltyService;
    }

    #region Order Webhooks

    public async Task SyncOrderCreatedAsync(string shopDomain, string payload)
    {
        try
        {
            var shopifyOrder = JsonSerializer.Deserialize<ShopifyOrderPayload>(payload, JsonOptions);
            if (shopifyOrder == null)
            {
                _logger.LogWarning("Failed to deserialize order create payload for shop {Shop}", shopDomain);
                return;
            }

            // Check if order already exists
            var existingOrder = await _db.Orders
                .FirstOrDefaultAsync(o => o.PlatformOrderId == shopifyOrder.Id && o.ShopDomain == shopDomain);

            if (existingOrder != null)
            {
                _logger.LogInformation("Order {OrderId} already exists, updating instead", shopifyOrder.Id);
                await UpdateOrderFromPayload(existingOrder, shopifyOrder);
                await _db.SaveChangesAsync();
            }
            else
            {
                var order = MapToOrder(shopDomain, shopifyOrder);

                // Try to link customer
                if (shopifyOrder.Customer?.Id > 0)
                {
                    var customer = await _db.Customers
                        .FirstOrDefaultAsync(c => c.PlatformCustomerId == shopifyOrder.Customer.Id && c.ShopDomain == shopDomain);
                    if (customer != null)
                    {
                        order.CustomerId = customer.Id;
                    }
                }

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Created order {OrderNumber} for shop {Shop}", order.OrderNumber, shopDomain);

                // Award loyalty points for the new order
                try
                {
                    await _loyaltyService.ProcessOrderPointsAsync(order.Id);
                    _logger.LogInformation("Processed loyalty points for order {OrderNumber}", order.OrderNumber);
                }
                catch (Exception loyaltyEx)
                {
                    // Log but don't fail the webhook - loyalty is non-critical
                    _logger.LogWarning(loyaltyEx, "Failed to process loyalty points for order {OrderNumber}", order.OrderNumber);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing order create for shop {Shop}", shopDomain);
            throw;
        }
    }

    public async Task SyncOrderUpdatedAsync(string shopDomain, string payload)
    {
        try
        {
            var shopifyOrder = JsonSerializer.Deserialize<ShopifyOrderPayload>(payload, JsonOptions);
            if (shopifyOrder == null)
            {
                _logger.LogWarning("Failed to deserialize order update payload for shop {Shop}", shopDomain);
                return;
            }

            var existingOrder = await _db.Orders
                .FirstOrDefaultAsync(o => o.PlatformOrderId == shopifyOrder.Id && o.ShopDomain == shopDomain);

            if (existingOrder != null)
            {
                await UpdateOrderFromPayload(existingOrder, shopifyOrder);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Updated order {OrderNumber} for shop {Shop}", existingOrder.OrderNumber, shopDomain);
            }
            else
            {
                // Order doesn't exist locally, create it
                await SyncOrderCreatedAsync(shopDomain, payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing order update for shop {Shop}", shopDomain);
            throw;
        }
    }

    public async Task SyncOrderCancelledAsync(string shopDomain, string payload)
    {
        try
        {
            var shopifyOrder = JsonSerializer.Deserialize<ShopifyOrderPayload>(payload, JsonOptions);
            if (shopifyOrder == null) return;

            var existingOrder = await _db.Orders
                .FirstOrDefaultAsync(o => o.PlatformOrderId == shopifyOrder.Id && o.ShopDomain == shopDomain);

            if (existingOrder != null)
            {
                existingOrder.FinancialStatus = "cancelled";
                existingOrder.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Cancelled order {OrderNumber} for shop {Shop}", existingOrder.OrderNumber, shopDomain);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing order cancel for shop {Shop}", shopDomain);
            throw;
        }
    }

    public async Task SyncOrderFulfilledAsync(string shopDomain, string payload)
    {
        try
        {
            var shopifyOrder = JsonSerializer.Deserialize<ShopifyOrderPayload>(payload, JsonOptions);
            if (shopifyOrder == null) return;

            var existingOrder = await _db.Orders
                .FirstOrDefaultAsync(o => o.PlatformOrderId == shopifyOrder.Id && o.ShopDomain == shopDomain);

            if (existingOrder != null)
            {
                existingOrder.FulfillmentStatus = shopifyOrder.FulfillmentStatus ?? "fulfilled";
                existingOrder.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Fulfilled order {OrderNumber} for shop {Shop}", existingOrder.OrderNumber, shopDomain);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing order fulfillment for shop {Shop}", shopDomain);
            throw;
        }
    }

    private Order MapToOrder(string shopDomain, ShopifyOrderPayload payload)
    {
        return new Order
        {
            PlatformOrderId = payload.Id,
            ShopDomain = shopDomain,
            OrderNumber = payload.Name ?? payload.OrderNumber?.ToString() ?? payload.Id.ToString(),
            CustomerEmail = payload.Email,
            Subtotal = payload.SubtotalPrice ?? 0,
            TaxTotal = payload.TotalTax ?? 0,
            ShippingTotal = payload.TotalShippingPriceSet?.ShopMoney?.Amount ?? 0,
            DiscountTotal = payload.TotalDiscounts ?? 0,
            GrandTotal = payload.TotalPrice ?? 0,
            Currency = payload.Currency ?? "USD",
            FinancialStatus = payload.FinancialStatus ?? "pending",
            FulfillmentStatus = payload.FulfillmentStatus ?? "unfulfilled",
            BillingAddress = payload.BillingAddress != null ? JsonSerializer.Serialize(payload.BillingAddress) : null,
            ShippingAddress = payload.ShippingAddress != null ? JsonSerializer.Serialize(payload.ShippingAddress) : null,
            Notes = payload.Note,
            OrderDate = payload.CreatedAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    private Task UpdateOrderFromPayload(Order order, ShopifyOrderPayload payload)
    {
        order.CustomerEmail = payload.Email ?? order.CustomerEmail;
        order.Subtotal = payload.SubtotalPrice ?? order.Subtotal;
        order.TaxTotal = payload.TotalTax ?? order.TaxTotal;
        order.DiscountTotal = payload.TotalDiscounts ?? order.DiscountTotal;
        order.GrandTotal = payload.TotalPrice ?? order.GrandTotal;
        order.FinancialStatus = payload.FinancialStatus ?? order.FinancialStatus;
        order.FulfillmentStatus = payload.FulfillmentStatus ?? order.FulfillmentStatus;
        order.Notes = payload.Note ?? order.Notes;
        order.UpdatedAt = DateTime.UtcNow;

        if (payload.BillingAddress != null)
            order.BillingAddress = JsonSerializer.Serialize(payload.BillingAddress);
        if (payload.ShippingAddress != null)
            order.ShippingAddress = JsonSerializer.Serialize(payload.ShippingAddress);

        return Task.CompletedTask;
    }

    #endregion

    #region Customer Webhooks

    public async Task SyncCustomerCreatedAsync(string shopDomain, string payload)
    {
        try
        {
            var shopifyCustomer = JsonSerializer.Deserialize<ShopifyCustomerPayload>(payload, JsonOptions);
            if (shopifyCustomer == null)
            {
                _logger.LogWarning("Failed to deserialize customer create payload for shop {Shop}", shopDomain);
                return;
            }

            // Check if customer already exists
            var existingCustomer = await _db.Customers
                .FirstOrDefaultAsync(c => c.PlatformCustomerId == shopifyCustomer.Id && c.ShopDomain == shopDomain);

            if (existingCustomer != null)
            {
                _logger.LogInformation("Customer {CustomerId} already exists, updating instead", shopifyCustomer.Id);
                UpdateCustomerFromPayload(existingCustomer, shopifyCustomer);
            }
            else
            {
                var customer = MapToCustomer(shopDomain, shopifyCustomer);
                _db.Customers.Add(customer);
                _logger.LogInformation("Created customer {Email} for shop {Shop}", customer.Email, shopDomain);
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing customer create for shop {Shop}", shopDomain);
            throw;
        }
    }

    public async Task SyncCustomerUpdatedAsync(string shopDomain, string payload)
    {
        try
        {
            var shopifyCustomer = JsonSerializer.Deserialize<ShopifyCustomerPayload>(payload, JsonOptions);
            if (shopifyCustomer == null)
            {
                _logger.LogWarning("Failed to deserialize customer update payload for shop {Shop}", shopDomain);
                return;
            }

            var existingCustomer = await _db.Customers
                .FirstOrDefaultAsync(c => c.PlatformCustomerId == shopifyCustomer.Id && c.ShopDomain == shopDomain);

            if (existingCustomer != null)
            {
                UpdateCustomerFromPayload(existingCustomer, shopifyCustomer);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Updated customer {Email} for shop {Shop}", existingCustomer.Email, shopDomain);
            }
            else
            {
                // Customer doesn't exist locally, create it
                await SyncCustomerCreatedAsync(shopDomain, payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing customer update for shop {Shop}", shopDomain);
            throw;
        }
    }

    public async Task SyncCustomerDeletedAsync(string shopDomain, string payload)
    {
        try
        {
            var shopifyCustomer = JsonSerializer.Deserialize<ShopifyCustomerPayload>(payload, JsonOptions);
            if (shopifyCustomer == null) return;

            var existingCustomer = await _db.Customers
                .FirstOrDefaultAsync(c => c.PlatformCustomerId == shopifyCustomer.Id && c.ShopDomain == shopDomain);

            if (existingCustomer != null)
            {
                _db.Customers.Remove(existingCustomer);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Deleted customer {Email} for shop {Shop}", existingCustomer.Email, shopDomain);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing customer delete for shop {Shop}", shopDomain);
            throw;
        }
    }

    private Customer MapToCustomer(string shopDomain, ShopifyCustomerPayload payload)
    {
        var address = payload.DefaultAddress;
        return new Customer
        {
            PlatformCustomerId = payload.Id,
            ShopDomain = shopDomain,
            FirstName = payload.FirstName ?? string.Empty,
            LastName = payload.LastName ?? string.Empty,
            Email = payload.Email ?? string.Empty,
            Phone = payload.Phone,
            City = address?.City,
            State = address?.Province,
            PostalCode = address?.Zip,
            Country = address?.Country,
            ShippingAddress = address != null ? JsonSerializer.Serialize(address) : null,
            CreatedAt = payload.CreatedAt ?? DateTime.UtcNow
        };
    }

    private void UpdateCustomerFromPayload(Customer customer, ShopifyCustomerPayload payload)
    {
        customer.FirstName = payload.FirstName ?? customer.FirstName;
        customer.LastName = payload.LastName ?? customer.LastName;
        customer.Email = payload.Email ?? customer.Email;
        customer.Phone = payload.Phone ?? customer.Phone;
        customer.UpdatedAt = DateTime.UtcNow;

        if (payload.DefaultAddress != null)
        {
            customer.City = payload.DefaultAddress.City ?? customer.City;
            customer.State = payload.DefaultAddress.Province ?? customer.State;
            customer.PostalCode = payload.DefaultAddress.Zip ?? customer.PostalCode;
            customer.Country = payload.DefaultAddress.Country ?? customer.Country;
            customer.ShippingAddress = JsonSerializer.Serialize(payload.DefaultAddress);
        }
    }

    #endregion

    #region Product Webhooks

    public async Task SyncProductCreatedAsync(string shopDomain, string payload)
    {
        try
        {
            var shopifyProduct = JsonSerializer.Deserialize<ShopifyProductPayload>(payload, JsonOptions);
            if (shopifyProduct == null)
            {
                _logger.LogWarning("Failed to deserialize product create payload for shop {Shop}", shopDomain);
                return;
            }

            // Check if product already exists
            var existingProduct = await _db.Products
                .FirstOrDefaultAsync(p => p.PlatformProductId == shopifyProduct.Id && p.ShopDomain == shopDomain);

            if (existingProduct != null)
            {
                _logger.LogInformation("Product {ProductId} already exists, updating instead", shopifyProduct.Id);
                UpdateProductFromPayload(existingProduct, shopifyProduct);
            }
            else
            {
                var product = MapToProduct(shopDomain, shopifyProduct);
                _db.Products.Add(product);
                _logger.LogInformation("Created product {Title} for shop {Shop}", product.Title, shopDomain);
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing product create for shop {Shop}", shopDomain);
            throw;
        }
    }

    public async Task SyncProductUpdatedAsync(string shopDomain, string payload)
    {
        try
        {
            var shopifyProduct = JsonSerializer.Deserialize<ShopifyProductPayload>(payload, JsonOptions);
            if (shopifyProduct == null)
            {
                _logger.LogWarning("Failed to deserialize product update payload for shop {Shop}", shopDomain);
                return;
            }

            var existingProduct = await _db.Products
                .FirstOrDefaultAsync(p => p.PlatformProductId == shopifyProduct.Id && p.ShopDomain == shopDomain);

            if (existingProduct != null)
            {
                UpdateProductFromPayload(existingProduct, shopifyProduct);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Updated product {Title} for shop {Shop}", existingProduct.Title, shopDomain);
            }
            else
            {
                // Product doesn't exist locally, create it
                await SyncProductCreatedAsync(shopDomain, payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing product update for shop {Shop}", shopDomain);
            throw;
        }
    }

    public async Task SyncProductDeletedAsync(string shopDomain, string payload)
    {
        try
        {
            var shopifyProduct = JsonSerializer.Deserialize<ShopifyProductPayload>(payload, JsonOptions);
            if (shopifyProduct == null) return;

            var existingProduct = await _db.Products
                .FirstOrDefaultAsync(p => p.PlatformProductId == shopifyProduct.Id && p.ShopDomain == shopDomain);

            if (existingProduct != null)
            {
                existingProduct.IsActive = false;
                existingProduct.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Marked product {Title} as inactive for shop {Shop}", existingProduct.Title, shopDomain);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing product delete for shop {Shop}", shopDomain);
            throw;
        }
    }

    private Product MapToProduct(string shopDomain, ShopifyProductPayload payload)
    {
        var firstVariant = payload.Variants?.FirstOrDefault();
        return new Product
        {
            PlatformProductId = payload.Id,
            ShopDomain = shopDomain,
            Title = payload.Title ?? string.Empty,
            Description = payload.BodyHtml,
            Vendor = payload.Vendor,
            ProductType = payload.ProductType,
            Tags = payload.Tags,
            Sku = firstVariant?.Sku,
            Price = firstVariant?.Price ?? 0,
            CompareAtPrice = firstVariant?.CompareAtPrice,
            InventoryQuantity = firstVariant?.InventoryQuantity ?? 0,
            IsActive = payload.Status == "active",
            CreatedAt = payload.CreatedAt ?? DateTime.UtcNow
        };
    }

    private void UpdateProductFromPayload(Product product, ShopifyProductPayload payload)
    {
        product.Title = payload.Title ?? product.Title;
        product.Description = payload.BodyHtml ?? product.Description;
        product.Vendor = payload.Vendor ?? product.Vendor;
        product.ProductType = payload.ProductType ?? product.ProductType;
        product.Tags = payload.Tags ?? product.Tags;
        product.IsActive = payload.Status == "active";
        product.UpdatedAt = DateTime.UtcNow;

        var firstVariant = payload.Variants?.FirstOrDefault();
        if (firstVariant != null)
        {
            product.Sku = firstVariant.Sku ?? product.Sku;
            product.Price = firstVariant.Price ?? product.Price;
            product.CompareAtPrice = firstVariant.CompareAtPrice ?? product.CompareAtPrice;
            product.InventoryQuantity = firstVariant.InventoryQuantity ?? product.InventoryQuantity;
        }
    }

    #endregion

    #region Shopify Webhook Payload Classes

    private class ShopifyOrderPayload
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public long? OrderNumber { get; set; }
        public string? Email { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? SubtotalPrice { get; set; }
        public decimal? TotalTax { get; set; }
        public decimal? TotalDiscounts { get; set; }
        public string? Currency { get; set; }
        public string? FinancialStatus { get; set; }
        public string? FulfillmentStatus { get; set; }
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
        public ShopifyAddressPayload? BillingAddress { get; set; }
        public ShopifyAddressPayload? ShippingAddress { get; set; }
        public ShopifyCustomerPayload? Customer { get; set; }
        public ShopifyPriceSet? TotalShippingPriceSet { get; set; }
    }

    private class ShopifyCustomerPayload
    {
        public long Id { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public DateTime? CreatedAt { get; set; }
        public ShopifyAddressPayload? DefaultAddress { get; set; }
    }

    private class ShopifyProductPayload
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? BodyHtml { get; set; }
        public string? Vendor { get; set; }
        public string? ProductType { get; set; }
        public string? Tags { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<ShopifyVariantPayload>? Variants { get; set; }
    }

    private class ShopifyVariantPayload
    {
        public long Id { get; set; }
        public string? Sku { get; set; }
        public decimal? Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public int? InventoryQuantity { get; set; }
    }

    private class ShopifyAddressPayload
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? Zip { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
    }

    private class ShopifyPriceSet
    {
        public ShopifyMoney? ShopMoney { get; set; }
    }

    private class ShopifyMoney
    {
        public decimal Amount { get; set; }
        public string? CurrencyCode { get; set; }
    }

    #endregion
}

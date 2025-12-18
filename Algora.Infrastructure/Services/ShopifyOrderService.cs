using System;
using System.Collections.Generic;
using System.Linq;
using Algora.Application.DTOs;
using Algora.Application.DTOs.Order;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using ShopifySharp;

namespace Algora.Infrastructure.Shopify;

public class ShopifyOrderService : IShopifyOrderService
{
    private readonly IShopContext _context;
    private readonly ILogger<ShopifyOrderService> _logger;

    public ShopifyOrderService(IShopContext context, ILogger<ShopifyOrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private OrderService CreateOrderService() =>
        new OrderService(_context.ShopDomain, _context.AccessToken);

    private DraftOrderService CreateDraftOrderService() =>
        new DraftOrderService(_context.ShopDomain, _context.AccessToken);

    /// <summary>
    /// Sanitizes phone number to E.164 format for Shopify.
    /// Returns null if phone is empty/invalid to avoid Shopify validation errors.
    /// </summary>
    private static string? SanitizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        // Remove all non-digit characters except leading +
        var sanitized = phone.Trim();

        // Extract just the digits
        var digits = new string(sanitized.Where(char.IsDigit).ToArray());

        // Return null if no digits or too short
        if (string.IsNullOrEmpty(digits) || digits.Length < 10) return null;

        // Check if original had a + prefix (international format)
        if (sanitized.StartsWith("+"))
        {
            // Already has country code, just ensure clean format
            return "+" + digits;
        }

        // For numbers without + prefix:
        // - 10 digits: assume US/Canada, add +1
        // - 11 digits starting with 1: assume US/Canada with country code
        // - Other lengths: add + and hope for the best
        if (digits.Length == 10)
        {
            // US/Canada format without country code - add +1
            return "+1" + digits;
        }
        else if (digits.Length == 11 && digits.StartsWith("1"))
        {
            // US/Canada format with country code included
            return "+" + digits;
        }
        else if (digits.Length >= 11)
        {
            // Assume country code is included
            return "+" + digits;
        }

        // If we get here, number is invalid
        return null;
    }

    public async Task<IEnumerable<OrderDto>> GetAllAsync(int limit = 25)
    {
        _logger.LogInformation("Fetching orders for shop: {ShopDomain}", _context.ShopDomain);

        var service = CreateOrderService();

        // Fetch all orders regardless of status - include orders from all time
        var filter = new ShopifySharp.Filters.OrderListFilter
        {
            Limit = limit,
            Status = "any", // "any" includes open, closed, and cancelled orders
            CreatedAtMin = DateTime.UtcNow.AddYears(-2) // Include orders from last 2 years
        };

        _logger.LogInformation("Calling Shopify API with filter: Status={Status}, Limit={Limit}, CreatedAtMin={CreatedAtMin}",
            filter.Status, filter.Limit, filter.CreatedAtMin);

        try
        {
            var orders = await service.ListAsync(filter);

            // `ListResult<Order>` has an `Items` collection; enumerate that (null-safe).
            var items = orders?.Items ?? Enumerable.Empty<Order>();
            _logger.LogInformation("Retrieved {Count} orders from Shopify", items.Count());

            foreach (var o in items.Take(3))
            {
                _logger.LogInformation("Order: {Id} - {Name} - {Status} - {FinancialStatus}",
                    o.Id, o.Name, o.ClosedAt.HasValue ? "closed" : "open", o.FinancialStatus);
            }

            return items.Select(o => new OrderDto
            {
                Id = o.Id ?? 0,
                Name = o.Name,
                Email = o.Email,
                FinancialStatus = o.FinancialStatus,
                FulfillmentStatus = o.FulfillmentStatus,
                TotalPrice = o.TotalPrice ?? 0m,
                CreatedAt = o.CreatedAt?.DateTime ?? DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders from Shopify API");
            throw;
        }
    }

    public async Task<OrderDto?> GetByIdAsync(long id)
    {
        var service = CreateOrderService();
        var o = await service.GetAsync(id);
        if (o == null) return null;

        var customer = o.Customer == null ? null : new CustomerDto
        {
            Id = o.Customer.Id ?? 0,
            FirstName = o.Customer.FirstName,
            LastName = o.Customer.LastName,
            Email = o.Customer.Email,
            Phone = o.Customer.Phone,
            VerifiedEmail = o.Customer.VerifiedEmail ?? false,
            Tags = o.Customer.Tags
        };

        var billing = o.BillingAddress == null ? null : new AddressDto
        {
            Name = o.BillingAddress.Name,
            Address1 = o.BillingAddress.Address1,
            Address2 = o.BillingAddress.Address2,
            City = o.BillingAddress.City,
            Province = o.BillingAddress.Province,
            Country = o.BillingAddress.Country,
            Zip = o.BillingAddress.Zip,
            Phone = o.BillingAddress.Phone
        };

        var shipping = o.ShippingAddress == null ? null : new AddressDto
        {
            Name = o.ShippingAddress.Name,
            Address1 = o.ShippingAddress.Address1,
            Address2 = o.ShippingAddress.Address2,
            City = o.ShippingAddress.City,
            Province = o.ShippingAddress.Province,
            Country = o.ShippingAddress.Country,
            Zip = o.ShippingAddress.Zip,
            Phone = o.ShippingAddress.Phone
        };

        var items = (o.LineItems ?? Enumerable.Empty<LineItem>()).Select(li => new LineItemDto
        {
            Title = li.Title,
            Quantity = li.Quantity ?? 0,
            Price = li.Price ?? 0m
        }).ToList();

        return new OrderDto
        {
            Id = o.Id ?? 0,
            Name = o.Name,
            Email = o.Email,
            FinancialStatus = o.FinancialStatus,
            FulfillmentStatus = o.FulfillmentStatus,
            TotalPrice = o.TotalPrice ?? 0m,
            CreatedAt = o.CreatedAt?.DateTime ?? DateTime.Now,
            Note = o.Note,
            Tags = o.Tags,
            Customer = customer,
            BillingAddress = billing,
            ShippingAddress = shipping,
            LineItems = items
        };
    }

    public async Task<OrderDto?> CreateAsync(OrderDto dto)
    {
        var service = CreateOrderService();

        // Map line items from DTO
        var lineItems = (dto.LineItems ?? Enumerable.Empty<LineItemDto>())
            .Where(li => !string.IsNullOrWhiteSpace(li.Title))
            .Select(li => new LineItem
            {
                Title = li.Title,
                Quantity = li.Quantity,
                Price = li.Price
            }).ToList();

        // If no line items provided, create a default one
        if (!lineItems.Any())
        {
            lineItems.Add(new LineItem
            {
                Title = "Manual order item",
                Quantity = 1,
                Price = dto.TotalPrice
            });
        }

        var order = new Order
        {
            Email = dto.Email,
            FinancialStatus = dto.FinancialStatus,
            LineItems = lineItems,
            Customer = dto.Customer == null ? null : new Customer
            {
                FirstName = dto.Customer.FirstName,
                LastName = dto.Customer.LastName,
                Email = dto.Customer.Email,
                Phone = SanitizePhone(dto.Customer.Phone)
            },
            BillingAddress = dto.BillingAddress == null ? null : new Address
            {
                Name = dto.BillingAddress.Name,
                Address1 = dto.BillingAddress.Address1,
                Address2 = dto.BillingAddress.Address2,
                City = dto.BillingAddress.City,
                Province = dto.BillingAddress.Province,
                Country = dto.BillingAddress.Country,
                Zip = dto.BillingAddress.Zip,
                Phone = SanitizePhone(dto.BillingAddress.Phone)
            },
            ShippingAddress = dto.ShippingAddress == null ? null : new Address
            {
                Name = dto.ShippingAddress.Name,
                Address1 = dto.ShippingAddress.Address1,
                Address2 = dto.ShippingAddress.Address2,
                City = dto.ShippingAddress.City,
                Province = dto.ShippingAddress.Province,
                Country = dto.ShippingAddress.Country,
                Zip = dto.ShippingAddress.Zip,
                Phone = SanitizePhone(dto.ShippingAddress.Phone)
            }
        };

        _logger.LogInformation("Creating order with {ItemCount} line items for customer: {Email}", lineItems.Count, dto.Email);
        _logger.LogInformation("Phone numbers - Customer: {CustomerPhone}, Billing: {BillingPhone}, Shipping: {ShippingPhone}",
            order.Customer?.Phone ?? "(null)",
            order.BillingAddress?.Phone ?? "(null)",
            order.ShippingAddress?.Phone ?? "(null)");

        var created = await service.CreateAsync(order);

        _logger.LogInformation("Order created successfully: {OrderId} - {OrderName}", created.Id, created.Name);

        return new OrderDto
        {
            Id = created.Id ?? 0,
            Name = created.Name ?? "",
            Email = created.Email ?? "",
            FinancialStatus = created.FinancialStatus ?? "",
            FulfillmentStatus = created.FulfillmentStatus ?? "",
            TotalPrice = created.TotalPrice ?? 0m,
            CreatedAt = created.CreatedAt?.DateTime ?? DateTime.Now
        };
    }

    public async Task CancelAsync(long id)
    {
        var service = CreateOrderService();
        await service.CancelAsync(id);
        _logger.LogInformation("Order {Id} cancelled successfully.", id);
    }

    public async Task SendInvoiceAsync(long orderId)
    {
        var order = await GetByIdAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("Order not found for invoice: {OrderId}", orderId);
            return;
        }

        var draftService = CreateDraftOrderService();
        var draft = new DraftOrder
        {
            Email = order.Email,
            LineItems = (order.LineItems ?? Enumerable.Empty<LineItemDto>()).Select(li => new DraftLineItem
            {
                Title = li.Title,
                Quantity = li.Quantity,
                Price = li.Price
            }).ToList(),
            Customer = new Customer
            {
                Email = order.Customer?.Email,
                FirstName = order.Customer?.FirstName,
                LastName = order.Customer?.LastName
            },
            BillingAddress = order.BillingAddress == null ? null : new Address
            {
                Address1 = order.BillingAddress?.Address1,
                City = order.BillingAddress?.City,
                Province = order.BillingAddress?.Province,
                Country = order.BillingAddress?.Country,
                Zip = order.BillingAddress?.Zip
            },
            ShippingAddress = order.ShippingAddress == null ? null : new Address
            {
                Address1 = order.ShippingAddress?.Address1,
                City = order.ShippingAddress?.City,
                Province = order.ShippingAddress?.Province,
                Country = order.ShippingAddress?.Country,
                Zip = order.ShippingAddress?.Zip
            }
        };

        var created = await draftService.CreateAsync(draft);
        // created.Id is nullable long? - pass value
        if (created.Id.HasValue)
            await draftService.SendInvoiceAsync(created.Id.Value);

        _logger.LogInformation("Invoice sent for Order #{OrderId} to {Email}", orderId, order.Email);
    }

    public async Task<OrderDto?> UpdateAsync(UpdateOrderInput input)
    {
        var service = CreateOrderService();

        // First get the existing order
        var existing = await service.GetAsync(input.OrderId);
        if (existing == null)
        {
            _logger.LogWarning("Order not found for update: {OrderId}", input.OrderId);
            return null;
        }

        // Build update object - only include fields that are being updated
        var updateOrder = new Order
        {
            Id = input.OrderId,
            Email = input.Email ?? existing.Email,
            Note = input.Note ?? existing.Note,
            Tags = input.Tags ?? existing.Tags
        };

        // Update shipping address if provided
        if (!string.IsNullOrEmpty(input.ShippingAddress1) || !string.IsNullOrEmpty(input.ShippingCity))
        {
            updateOrder.ShippingAddress = new Address
            {
                Name = input.ShippingName ?? existing.ShippingAddress?.Name,
                Address1 = input.ShippingAddress1 ?? existing.ShippingAddress?.Address1,
                Address2 = input.ShippingAddress2 ?? existing.ShippingAddress?.Address2,
                City = input.ShippingCity ?? existing.ShippingAddress?.City,
                Province = input.ShippingProvince ?? existing.ShippingAddress?.Province,
                Country = input.ShippingCountry ?? existing.ShippingAddress?.Country,
                Zip = input.ShippingZip ?? existing.ShippingAddress?.Zip,
                Phone = SanitizePhone(input.ShippingPhone ?? existing.ShippingAddress?.Phone)
            };
        }

        _logger.LogInformation("Updating order {OrderId}", input.OrderId);

        var updated = await service.UpdateAsync(input.OrderId, updateOrder);

        _logger.LogInformation("Order {OrderId} updated successfully", input.OrderId);

        return new OrderDto
        {
            Id = updated.Id ?? 0,
            Name = updated.Name ?? "",
            Email = updated.Email ?? "",
            FinancialStatus = updated.FinancialStatus ?? "",
            FulfillmentStatus = updated.FulfillmentStatus ?? "",
            TotalPrice = updated.TotalPrice ?? 0m,
            CreatedAt = updated.CreatedAt?.DateTime ?? DateTime.Now,
            Note = updated.Note,
            Tags = updated.Tags
        };
    }

    public async Task CloseAsync(long id)
    {
        var service = CreateOrderService();
        await service.CloseAsync(id);
        _logger.LogInformation("Order {Id} closed successfully.", id);
    }
}

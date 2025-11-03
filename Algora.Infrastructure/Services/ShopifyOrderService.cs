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

    public async Task<IEnumerable<OrderDto>> GetAllAsync(int limit = 25)
    {
        var service = CreateOrderService();

        var filter = new ShopifySharp.Filters.OrderListFilter
        {
            Limit = limit,
            Fields = "id,name,email,total_price,financial_status,fulfillment_status,created_at"
        };

        var orders = await service.ListAsync(filter);

        // `ListResult<Order>` has an `Items` collection; enumerate that (null-safe).
        var items = orders?.Items ?? Enumerable.Empty<Order>();
        return items.Select(o => new OrderDto
        {
            Id = o.Id ?? 0,
            Name = o.Name,
            Email = o.Email,
            FinancialStatus = o.FinancialStatus,
            FulfillmentStatus = o.FulfillmentStatus,
            // TotalPrice is decimal? on the Shopify type — use the value or fallback to 0m.
            TotalPrice = o.TotalPrice ?? 0m,
            CreatedAt = o.CreatedAt?.DateTime ?? DateTime.Now
        });
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
            Customer = customer,
            BillingAddress = billing,
            ShippingAddress = shipping,
            LineItems = items
        };
    }

    public async Task<OrderDto?> CreateAsync(OrderDto dto)
    {
        var service = CreateOrderService();

        var order = new Order
        {
            Email = dto.Email,
            FinancialStatus = dto.FinancialStatus,
            LineItems = new List<LineItem>
            {
                new LineItem
                {
                    Title = "Sample product",
                    Quantity = 1,
                    Price = dto.TotalPrice
                }
            }
        };

        var created = await service.CreateAsync(order);

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
}

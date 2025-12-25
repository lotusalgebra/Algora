using Algora.Chatbot.Application.DTOs;

namespace Algora.Chatbot.Application.Interfaces.Shopify;

public interface IShopifyOrderService
{
    Task<ShopifyOrderDto?> GetOrderAsync(string shopDomain, long orderId, CancellationToken cancellationToken = default);
    Task<ShopifyOrderDto?> GetOrderByNumberAsync(string shopDomain, string orderNumber, CancellationToken cancellationToken = default);
    Task<List<ShopifyOrderDto>> GetCustomerOrdersAsync(string shopDomain, string customerEmail, int limit = 10, CancellationToken cancellationToken = default);
}

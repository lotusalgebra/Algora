using Algora.Chatbot.Application.DTOs;

namespace Algora.Chatbot.Application.Interfaces.Shopify;

public interface IShopifyCustomerService
{
    Task<ShopifyCustomerDto?> GetCustomerAsync(string shopDomain, long customerId, CancellationToken cancellationToken = default);
    Task<ShopifyCustomerDto?> GetCustomerByEmailAsync(string shopDomain, string email, CancellationToken cancellationToken = default);
}

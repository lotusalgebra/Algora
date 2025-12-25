using Algora.Chatbot.Application.DTOs;

namespace Algora.Chatbot.Application.Interfaces.Shopify;

public interface IShopifyProductService
{
    Task<ShopifyProductDto?> GetProductAsync(string shopDomain, long productId, CancellationToken cancellationToken = default);
    Task<List<ShopifyProductDto>> SearchProductsAsync(string shopDomain, string query, int limit = 10, CancellationToken cancellationToken = default);
    Task<List<ShopifyProductDto>> GetRecommendedProductsAsync(string shopDomain, long? relatedProductId = null, int limit = 5, CancellationToken cancellationToken = default);
}

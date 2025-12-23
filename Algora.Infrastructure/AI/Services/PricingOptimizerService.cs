using System.Text.Json;
using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces.AI;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.AI.Services;

public class PricingOptimizerService : IPricingOptimizerService
{
    private readonly AppDbContext _db;
    private readonly IAiTextProvider _aiProvider;
    private readonly ILogger<PricingOptimizerService> _logger;

    public PricingOptimizerService(
        AppDbContext db,
        IAiTextProvider aiProvider,
        ILogger<PricingOptimizerService> logger)
    {
        _db = db;
        _aiProvider = aiProvider;
        _logger = logger;
    }

    public async Task<PricingOptimizationResponse> GetSuggestionAsync(int productId, CancellationToken ct = default)
    {
        try
        {
            var product = await _db.Products.FindAsync(new object[] { productId }, ct);
            if (product == null)
            {
                return new PricingOptimizationResponse { Success = false, Error = "Product not found", ProductId = productId };
            }

            // Calculate sales in last 30 days
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var salesCount = await _db.OrderLines
                .Where(ol => ol.Order.CreatedAt >= thirtyDaysAgo)
                .Where(ol => ol.PlatformProductId == product.PlatformProductId)
                .SumAsync(ol => ol.Quantity, ct);

            var request = new PricingOptimizationRequest
            {
                ProductId = productId,
                Title = product.Title,
                Category = product.ProductType,
                CurrentPrice = product.Price,
                CostOfGoodsSold = product.CostOfGoodsSold,
                InventoryQuantity = product.InventoryQuantity,
                SalesCount30Days = salesCount
            };

            var prompt = BuildPricingPrompt(request);
            var (providerName, _) = _aiProvider.GetProviderInfo();

            var response = await _aiProvider.GenerateTextAsync(prompt, ct);
            var result = ParsePricingResponse(response, request, providerName);

            if (result.Success)
            {
                // Save suggestion
                var suggestion = new PricingSuggestion
                {
                    ShopDomain = product.ShopDomain,
                    ProductId = productId,
                    CurrentPrice = product.Price,
                    SuggestedPrice = result.SuggestedPrice,
                    MinPrice = result.MinPrice,
                    MaxPrice = result.MaxPrice,
                    PriceChange = result.PriceChange,
                    ChangePercent = result.ChangePercent,
                    Reasoning = result.Reasoning,
                    Confidence = result.Confidence,
                    Provider = providerName
                };

                _db.Set<PricingSuggestion>().Add(suggestion);
                await _db.SaveChangesAsync(ct);

                return result with { SuggestionId = suggestion.Id };
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pricing suggestion for product {ProductId}", productId);
            return new PricingOptimizationResponse
            {
                Success = false,
                Error = ex.Message,
                ProductId = productId
            };
        }
    }

    public async Task<IEnumerable<PricingOptimizationResponse>> GetBulkSuggestionsAsync(IEnumerable<int> productIds, CancellationToken ct = default)
    {
        var results = new List<PricingOptimizationResponse>();

        foreach (var productId in productIds)
        {
            if (ct.IsCancellationRequested) break;

            var result = await GetSuggestionAsync(productId, ct);
            results.Add(result);

            await Task.Delay(200, ct); // Rate limiting
        }

        return results;
    }

    public async Task ApplySuggestionAsync(int suggestionId, CancellationToken ct = default)
    {
        var suggestion = await _db.Set<PricingSuggestion>()
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == suggestionId, ct);

        if (suggestion != null && !suggestion.WasApplied)
        {
            suggestion.Product.Price = suggestion.SuggestedPrice;
            suggestion.Product.UpdatedAt = DateTime.UtcNow;
            suggestion.WasApplied = true;
            suggestion.AppliedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<IEnumerable<PricingSuggestionDto>> GetHistoryAsync(int productId, int count = 10, CancellationToken ct = default)
    {
        var suggestions = await _db.Set<PricingSuggestion>()
            .Where(s => s.ProductId == productId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

        return suggestions.Select(s => new PricingSuggestionDto
        {
            Id = s.Id,
            ProductId = s.ProductId,
            CurrentPrice = s.CurrentPrice,
            SuggestedPrice = s.SuggestedPrice,
            ChangePercent = s.ChangePercent,
            Reasoning = s.Reasoning,
            Confidence = s.Confidence,
            WasApplied = s.WasApplied,
            CreatedAt = s.CreatedAt
        });
    }

    private static string BuildPricingPrompt(PricingOptimizationRequest request)
    {
        var margin = request.CostOfGoodsSold.HasValue && request.CostOfGoodsSold > 0
            ? ((request.CurrentPrice - request.CostOfGoodsSold.Value) / request.CurrentPrice * 100)
            : (decimal?)null;

        return $@"Analyze pricing for this product and suggest an optimal price.

Product: {request.Title}
Category: {request.Category ?? "General"}
Current Price: ${request.CurrentPrice:F2}
Cost: {(request.CostOfGoodsSold.HasValue ? $"${request.CostOfGoodsSold:F2}" : "Unknown")}
Current Margin: {(margin.HasValue ? $"{margin:F1}%" : "Unknown")}
Inventory: {request.InventoryQuantity} units
Sales (30 days): {request.SalesCount30Days} units

Consider:
1. Maximize profit while maintaining competitiveness
2. Account for inventory levels (high inventory may need lower prices)
3. Consider sales velocity (low sales may need price adjustment)
4. Maintain reasonable margins (typically 30-60% for retail)
5. Suggest a price range for flexibility

IMPORTANT: Respond ONLY with valid JSON in this exact format:
{{
  ""suggestedPrice"": 29.99,
  ""minPrice"": 24.99,
  ""maxPrice"": 34.99,
  ""reasoning"": ""Brief explanation of the suggested price (1-2 sentences)"",
  ""confidence"": 75
}}";
    }

    private static PricingOptimizationResponse ParsePricingResponse(string response, PricingOptimizationRequest request, string provider)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<PricingJsonResponse>(jsonStr, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null && parsed.SuggestedPrice > 0)
                {
                    var priceChange = parsed.SuggestedPrice - request.CurrentPrice;
                    var changePercent = request.CurrentPrice > 0
                        ? (priceChange / request.CurrentPrice * 100)
                        : 0;

                    var currentMargin = request.CostOfGoodsSold.HasValue && request.CostOfGoodsSold > 0
                        ? ((request.CurrentPrice - request.CostOfGoodsSold.Value) / request.CurrentPrice * 100)
                        : 0;

                    var suggestedMargin = request.CostOfGoodsSold.HasValue && request.CostOfGoodsSold > 0
                        ? ((parsed.SuggestedPrice - request.CostOfGoodsSold.Value) / parsed.SuggestedPrice * 100)
                        : (decimal?)null;

                    return new PricingOptimizationResponse
                    {
                        Success = true,
                        ProductId = request.ProductId,
                        CurrentPrice = request.CurrentPrice,
                        SuggestedPrice = Math.Round(parsed.SuggestedPrice, 2),
                        MinPrice = parsed.MinPrice.HasValue ? Math.Round(parsed.MinPrice.Value, 2) : null,
                        MaxPrice = parsed.MaxPrice.HasValue ? Math.Round(parsed.MaxPrice.Value, 2) : null,
                        PriceChange = Math.Round(priceChange, 2),
                        ChangePercent = Math.Round(changePercent, 1),
                        CurrentMargin = Math.Round(currentMargin, 1),
                        SuggestedMargin = suggestedMargin.HasValue ? Math.Round(suggestedMargin.Value, 1) : null,
                        Reasoning = parsed.Reasoning,
                        Confidence = Math.Clamp(parsed.Confidence, 0, 100),
                        Provider = provider
                    };
                }
            }

            return new PricingOptimizationResponse
            {
                Success = false,
                Error = "Failed to parse AI response",
                ProductId = request.ProductId,
                CurrentPrice = request.CurrentPrice
            };
        }
        catch (Exception ex)
        {
            return new PricingOptimizationResponse
            {
                Success = false,
                Error = $"Parse error: {ex.Message}",
                ProductId = request.ProductId,
                CurrentPrice = request.CurrentPrice
            };
        }
    }

    private class PricingJsonResponse
    {
        public decimal SuggestedPrice { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Reasoning { get; set; }
        public decimal Confidence { get; set; }
    }
}

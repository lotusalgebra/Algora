using System.Text.Json;
using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces.AI;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.AI.Services;

public class SeoOptimizerService : ISeoOptimizerService
{
    private readonly AppDbContext _db;
    private readonly IAiTextProvider _aiProvider;
    private readonly ILogger<SeoOptimizerService> _logger;

    public SeoOptimizerService(
        AppDbContext db,
        IAiTextProvider aiProvider,
        ILogger<SeoOptimizerService> logger)
    {
        _db = db;
        _aiProvider = aiProvider;
        _logger = logger;
    }

    public async Task<SeoOptimizationResponse> OptimizeAsync(SeoOptimizationRequest request, CancellationToken ct = default)
    {
        try
        {
            var prompt = BuildSeoPrompt(request);
            var (providerName, modelName) = _aiProvider.GetProviderInfo();

            var response = await _aiProvider.GenerateTextAsync(prompt, ct);
            var result = ParseSeoResponse(response, request.ProductId, providerName);

            // Save to database
            var existing = await _db.Set<ProductSeoData>()
                .FirstOrDefaultAsync(s => s.ProductId == request.ProductId, ct);

            if (existing != null)
            {
                existing.MetaTitle = result.MetaTitle;
                existing.MetaDescription = result.MetaDescription;
                existing.FocusKeyword = result.FocusKeyword;
                existing.Keywords = string.Join(",", result.Keywords);
                existing.SeoScore = result.SeoScore;
                existing.Provider = providerName;
                existing.GeneratedAt = DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.Set<ProductSeoData>().Add(new ProductSeoData
                {
                    ProductId = request.ProductId,
                    MetaTitle = result.MetaTitle,
                    MetaDescription = result.MetaDescription,
                    FocusKeyword = result.FocusKeyword,
                    Keywords = string.Join(",", result.Keywords),
                    SeoScore = result.SeoScore,
                    Provider = providerName,
                    GeneratedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing SEO for product {ProductId}", request.ProductId);
            return new SeoOptimizationResponse
            {
                Success = false,
                Error = ex.Message,
                ProductId = request.ProductId
            };
        }
    }

    public async Task<IEnumerable<SeoOptimizationResponse>> BulkOptimizeAsync(IEnumerable<int> productIds, CancellationToken ct = default)
    {
        var results = new List<SeoOptimizationResponse>();

        foreach (var productId in productIds)
        {
            if (ct.IsCancellationRequested) break;

            var product = await _db.Products.FindAsync(new object[] { productId }, ct);
            if (product == null) continue;

            var request = new SeoOptimizationRequest
            {
                ProductId = productId,
                Title = product.Title,
                Description = product.Description,
                Category = product.ProductType,
                Vendor = product.Vendor,
                Tags = product.Tags
            };

            var result = await OptimizeAsync(request, ct);
            results.Add(result);

            // Rate limiting delay
            await Task.Delay(200, ct);
        }

        return results;
    }

    public async Task<int> GetSeoScoreAsync(int productId, CancellationToken ct = default)
    {
        var seoData = await _db.Set<ProductSeoData>()
            .FirstOrDefaultAsync(s => s.ProductId == productId, ct);

        return seoData?.SeoScore ?? 0;
    }

    public async Task ApplySeoDataAsync(int productId, SeoOptimizationResponse data, CancellationToken ct = default)
    {
        var existing = await _db.Set<ProductSeoData>()
            .FirstOrDefaultAsync(s => s.ProductId == productId, ct);

        if (existing != null)
        {
            existing.ApprovedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<SeoOptimizationResponse?> GetSeoDataAsync(int productId, CancellationToken ct = default)
    {
        var seoData = await _db.Set<ProductSeoData>()
            .FirstOrDefaultAsync(s => s.ProductId == productId, ct);

        if (seoData == null) return null;

        return new SeoOptimizationResponse
        {
            Success = true,
            ProductId = productId,
            MetaTitle = seoData.MetaTitle,
            MetaDescription = seoData.MetaDescription,
            FocusKeyword = seoData.FocusKeyword,
            Keywords = seoData.Keywords?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new(),
            SeoScore = seoData.SeoScore ?? 0,
            Provider = seoData.Provider
        };
    }

    private static string BuildSeoPrompt(SeoOptimizationRequest request)
    {
        return $@"You are an SEO expert. Optimize the following product for search engines.

Product: {request.Title}
Description: {request.Description ?? "Not provided"}
Category: {request.Category ?? "Not specified"}
Vendor: {request.Vendor ?? "Not specified"}
Tags: {request.Tags ?? "None"}
Existing Keywords: {request.ExistingKeywords ?? "None"}

Generate SEO-optimized content:
1. Meta Title (max 60 characters, include primary keyword, make it compelling)
2. Meta Description (max 155 characters, include call-to-action, highlight benefits)
3. Focus Keyword (single most important keyword phrase)
4. Related Keywords (5-7 long-tail keywords, comma-separated)
5. SEO Score (0-100) with brief explanation

IMPORTANT: Respond ONLY with valid JSON in this exact format:
{{
  ""metaTitle"": ""...(max 60 chars)"",
  ""metaDescription"": ""...(max 155 chars)"",
  ""focusKeyword"": ""..."",
  ""keywords"": [""keyword1"", ""keyword2"", ""keyword3"", ""keyword4"", ""keyword5""],
  ""seoScore"": 85,
  ""scoreExplanation"": ""Brief explanation of the score""
}}";
    }

    private static SeoOptimizationResponse ParseSeoResponse(string response, int productId, string provider)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<SeoJsonResponse>(jsonStr, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null)
                {
                    return new SeoOptimizationResponse
                    {
                        Success = true,
                        ProductId = productId,
                        MetaTitle = parsed.MetaTitle?.Length > 60 ? parsed.MetaTitle[..60] : parsed.MetaTitle,
                        MetaDescription = parsed.MetaDescription?.Length > 155 ? parsed.MetaDescription[..155] : parsed.MetaDescription,
                        FocusKeyword = parsed.FocusKeyword,
                        Keywords = parsed.Keywords ?? new(),
                        SeoScore = Math.Clamp(parsed.SeoScore, 0, 100),
                        SeoScoreExplanation = parsed.ScoreExplanation,
                        Provider = provider
                    };
                }
            }

            // Fallback: return error
            return new SeoOptimizationResponse
            {
                Success = false,
                Error = "Failed to parse AI response",
                ProductId = productId
            };
        }
        catch (Exception ex)
        {
            return new SeoOptimizationResponse
            {
                Success = false,
                Error = $"Parse error: {ex.Message}",
                ProductId = productId
            };
        }
    }

    private class SeoJsonResponse
    {
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? FocusKeyword { get; set; }
        public List<string>? Keywords { get; set; }
        public int SeoScore { get; set; }
        public string? ScoreExplanation { get; set; }
    }
}

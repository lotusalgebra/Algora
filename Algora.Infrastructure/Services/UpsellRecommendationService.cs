using System.Text.Json;
using Algora.Application.DTOs.Inventory;
using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for managing upsell offers and generating recommendations.
/// </summary>
public class UpsellRecommendationService : IUpsellRecommendationService
{
    private readonly AppDbContext _db;
    private readonly IProductAffinityService _affinityService;
    private readonly ILogger<UpsellRecommendationService> _logger;

    public UpsellRecommendationService(
        AppDbContext db,
        IProductAffinityService affinityService,
        ILogger<UpsellRecommendationService> logger)
    {
        _db = db;
        _affinityService = affinityService;
        _logger = logger;
    }

    public async Task<List<UpsellOfferDto>> GetOffersForOrderAsync(
        string shopDomain,
        long platformOrderId,
        string sessionId)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);
        if (!settings.IsEnabled) return new List<UpsellOfferDto>();

        // Get the order to find purchased products
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.ShopDomain == shopDomain && o.PlatformOrderId == platformOrderId);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for shop {Shop}", platformOrderId, shopDomain);
            return new List<UpsellOfferDto>();
        }

        var purchasedProductIds = order.Lines
            .Where(l => l.PlatformProductId.HasValue)
            .Select(l => l.PlatformProductId!.Value)
            .Distinct()
            .ToList();

        return await GetOffersForProductsAsync(shopDomain, purchasedProductIds, settings.MaxOffersToShow);
    }

    public async Task<List<UpsellOfferDto>> GetOffersForProductsAsync(
        string shopDomain,
        List<long> productIds,
        int maxOffers = 3)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);

        // Get active offers that match the trigger products
        var offers = await _db.UpsellOffers
            .Where(o => o.ShopDomain == shopDomain)
            .Where(o => o.IsActive)
            .Where(o => !productIds.Contains(o.RecommendedProductId)) // Don't recommend already purchased products
            .OrderBy(o => o.DisplayOrder)
            .ToListAsync();

        // Filter by trigger products
        var matchingOffers = offers.Where(o =>
        {
            if (string.IsNullOrEmpty(o.TriggerProductIds))
                return true; // No trigger = matches all

            var triggerIds = ParseTriggerProductIds(o.TriggerProductIds);
            return triggerIds.Any(t => productIds.Contains(t));
        }).ToList();

        // If not enough manual offers, supplement with affinity-based recommendations
        if (matchingOffers.Count < maxOffers)
        {
            var neededCount = maxOffers - matchingOffers.Count;
            var existingRecommendedIds = matchingOffers.Select(o => o.RecommendedProductId).ToHashSet();

            foreach (var productId in productIds.Take(3)) // Check top 3 purchased products
            {
                var affinities = await _affinityService.GetAffinitiesForProductAsync(shopDomain, productId, neededCount);

                foreach (var affinity in affinities)
                {
                    var relatedProductId = affinity.RelatedProductId;
                    if (productIds.Contains(relatedProductId) || existingRecommendedIds.Contains(relatedProductId))
                        continue;

                    // Create a temporary offer from affinity
                    matchingOffers.Add(new UpsellOffer
                    {
                        Id = 0, // Temporary
                        ShopDomain = shopDomain,
                        Name = $"Affinity: {affinity.RelatedProductTitle}",
                        RecommendedProductId = relatedProductId,
                        RecommendedProductTitle = affinity.RelatedProductTitle,
                        RecommendationSource = "affinity",
                        ProductAffinityId = affinity.Id,
                        Headline = "Frequently Bought Together",
                        BodyText = "Customers who bought this item also bought",
                        ButtonText = "Add to Cart",
                        IsActive = true
                    });

                    existingRecommendedIds.Add(relatedProductId);

                    if (matchingOffers.Count >= maxOffers)
                        break;
                }

                if (matchingOffers.Count >= maxOffers)
                    break;
            }
        }

        // Generate cart URLs for each offer
        var result = new List<UpsellOfferDto>();
        foreach (var offer in matchingOffers.Take(maxOffers))
        {
            var cartUrl = await GenerateCartUrlAsync(
                shopDomain,
                new List<CartItemRequest> { new(offer.RecommendedVariantId ?? offer.RecommendedProductId, 1) },
                offer.DiscountCode);

            result.Add(MapToDto(offer, cartUrl));
        }

        return result;
    }

    public Task<string> GenerateCartUrlAsync(
        string shopDomain,
        List<CartItemRequest> items,
        string? discountCode = null)
    {
        // Shopify cart URL format: https://{shop}/cart/{variant_id}:{quantity},{variant_id}:{quantity}
        var cartItems = string.Join(",", items.Select(i => $"{i.VariantId}:{i.Quantity}"));
        var baseUrl = $"https://{shopDomain}/cart/{cartItems}";

        if (!string.IsNullOrEmpty(discountCode))
            baseUrl += $"?discount={Uri.EscapeDataString(discountCode)}";

        return Task.FromResult(baseUrl);
    }

    public async Task<List<ProductAffinityDto>> GetAffinityRecommendationsAsync(
        string shopDomain,
        long productId,
        int limit = 5)
    {
        return await _affinityService.GetAffinitiesForProductAsync(shopDomain, productId, limit);
    }

    public async Task<PaginatedResult<UpsellOfferDto>> GetOffersAsync(
        string shopDomain,
        bool? isActive = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _db.UpsellOffers
            .Where(o => o.ShopDomain == shopDomain)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(o => o.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(o => o.DisplayOrder)
            .ThenByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<UpsellOfferDto>
        {
            Items = items.Select(o => MapToDto(o)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UpsellOfferDto?> GetOfferByIdAsync(int offerId)
    {
        var offer = await _db.UpsellOffers.FindAsync(offerId);
        return offer != null ? MapToDto(offer) : null;
    }

    public async Task<UpsellOfferDto> CreateOfferAsync(string shopDomain, CreateUpsellOfferDto dto)
    {
        var offer = new UpsellOffer
        {
            ShopDomain = shopDomain,
            Name = dto.Name,
            Description = dto.Description,
            TriggerProductIds = dto.TriggerProductIds != null ? JsonSerializer.Serialize(dto.TriggerProductIds) : null,
            RecommendedProductId = dto.RecommendedProductId,
            RecommendedVariantId = dto.RecommendedVariantId,
            RecommendedProductTitle = dto.Name, // Will be updated when product info is fetched
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            DiscountCode = dto.DiscountCode,
            Headline = dto.Headline,
            BodyText = dto.BodyText,
            ButtonText = dto.ButtonText,
            DisplayOrder = dto.DisplayOrder,
            RecommendationSource = "manual",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.UpsellOffers.Add(offer);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created upsell offer {OfferId} for shop {Shop}", offer.Id, shopDomain);
        return MapToDto(offer);
    }

    public async Task<UpsellOfferDto> UpdateOfferAsync(int offerId, CreateUpsellOfferDto dto)
    {
        var offer = await _db.UpsellOffers.FindAsync(offerId);
        if (offer == null)
            throw new InvalidOperationException($"Offer {offerId} not found");

        offer.Name = dto.Name;
        offer.Description = dto.Description;
        offer.TriggerProductIds = dto.TriggerProductIds != null ? JsonSerializer.Serialize(dto.TriggerProductIds) : null;
        offer.RecommendedProductId = dto.RecommendedProductId;
        offer.RecommendedVariantId = dto.RecommendedVariantId;
        offer.DiscountType = dto.DiscountType;
        offer.DiscountValue = dto.DiscountValue;
        offer.DiscountCode = dto.DiscountCode;
        offer.Headline = dto.Headline;
        offer.BodyText = dto.BodyText;
        offer.ButtonText = dto.ButtonText;
        offer.DisplayOrder = dto.DisplayOrder;
        offer.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated upsell offer {OfferId}", offerId);
        return MapToDto(offer);
    }

    public async Task DeleteOfferAsync(int offerId)
    {
        var offer = await _db.UpsellOffers.FindAsync(offerId);
        if (offer != null)
        {
            _db.UpsellOffers.Remove(offer);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted upsell offer {OfferId}", offerId);
        }
    }

    public async Task<UpsellSettingsDto> GetSettingsAsync(string shopDomain)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);
        return MapSettingsToDto(settings);
    }

    public async Task<UpsellSettingsDto> UpdateSettingsAsync(string shopDomain, UpdateUpsellSettingsDto dto)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);

        if (dto.IsEnabled.HasValue) settings.IsEnabled = dto.IsEnabled.Value;
        if (dto.ShowOnConfirmationPage.HasValue) settings.ShowOnConfirmationPage = dto.ShowOnConfirmationPage.Value;
        if (dto.SendUpsellEmail.HasValue) settings.SendUpsellEmail = dto.SendUpsellEmail.Value;
        if (dto.MaxOffersToShow.HasValue) settings.MaxOffersToShow = dto.MaxOffersToShow.Value;
        if (dto.DisplayLayout != null) settings.DisplayLayout = dto.DisplayLayout;
        if (dto.AffinityLookbackDays.HasValue) settings.AffinityLookbackDays = dto.AffinityLookbackDays.Value;
        if (dto.MinimumConfidenceScore.HasValue) settings.MinimumConfidenceScore = dto.MinimumConfidenceScore.Value;
        if (dto.MinimumCoOccurrences.HasValue) settings.MinimumCoOccurrences = dto.MinimumCoOccurrences.Value;
        if (dto.PageTitle != null) settings.PageTitle = dto.PageTitle;
        if (dto.ThankYouMessage != null) settings.ThankYouMessage = dto.ThankYouMessage;
        if (dto.UpsellSectionTitle != null) settings.UpsellSectionTitle = dto.UpsellSectionTitle;
        if (dto.CustomCss != null) settings.CustomCss = dto.CustomCss;
        if (dto.LogoUrl != null) settings.LogoUrl = dto.LogoUrl;
        if (dto.PrimaryColor != null) settings.PrimaryColor = dto.PrimaryColor;
        if (dto.SecondaryColor != null) settings.SecondaryColor = dto.SecondaryColor;

        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated upsell settings for shop {Shop}", shopDomain);
        return MapSettingsToDto(settings);
    }

    private async Task<UpsellSettings> GetOrCreateSettingsAsync(string shopDomain)
    {
        var settings = await _db.UpsellSettings
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings == null)
        {
            settings = new UpsellSettings
            {
                ShopDomain = shopDomain,
                PageTitle = "Order Confirmed",
                ThankYouMessage = "Thank you for your order!",
                UpsellSectionTitle = "You might also like",
                CreatedAt = DateTime.UtcNow
            };
            _db.UpsellSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        return settings;
    }

    private static List<long> ParseTriggerProductIds(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new List<long>();
        try
        {
            return JsonSerializer.Deserialize<List<long>>(json) ?? new List<long>();
        }
        catch
        {
            return new List<long>();
        }
    }

    private static UpsellOfferDto MapToDto(UpsellOffer o, string? cartUrl = null)
    {
        decimal? discountedPrice = null;
        if (o.DiscountType == "percentage" && o.DiscountValue.HasValue)
            discountedPrice = o.RecommendedProductPrice * (1 - o.DiscountValue.Value / 100);
        else if (o.DiscountType == "fixed_amount" && o.DiscountValue.HasValue)
            discountedPrice = Math.Max(0, o.RecommendedProductPrice - o.DiscountValue.Value);

        return new UpsellOfferDto
        {
            Id = o.Id,
            Name = o.Name,
            Description = o.Description,
            IsActive = o.IsActive,
            RecommendedProductId = o.RecommendedProductId,
            RecommendedVariantId = o.RecommendedVariantId,
            RecommendedProductTitle = o.RecommendedProductTitle,
            RecommendedProductImageUrl = o.RecommendedProductImageUrl,
            RecommendedProductPrice = o.RecommendedProductPrice,
            DiscountedPrice = discountedPrice,
            DiscountType = o.DiscountType,
            DiscountValue = o.DiscountValue,
            Headline = o.Headline,
            BodyText = o.BodyText,
            ButtonText = o.ButtonText ?? "Add to Cart",
            RecommendationSource = o.RecommendationSource,
            CartUrl = cartUrl
        };
    }

    private static UpsellSettingsDto MapSettingsToDto(UpsellSettings s) => new()
    {
        Id = s.Id,
        IsEnabled = s.IsEnabled,
        ShowOnConfirmationPage = s.ShowOnConfirmationPage,
        SendUpsellEmail = s.SendUpsellEmail,
        MaxOffersToShow = s.MaxOffersToShow,
        DisplayLayout = s.DisplayLayout,
        AffinityLookbackDays = s.AffinityLookbackDays,
        MinimumConfidenceScore = s.MinimumConfidenceScore,
        MinimumCoOccurrences = s.MinimumCoOccurrences,
        PageTitle = s.PageTitle,
        ThankYouMessage = s.ThankYouMessage,
        UpsellSectionTitle = s.UpsellSectionTitle,
        CustomCss = s.CustomCss,
        LogoUrl = s.LogoUrl,
        PrimaryColor = s.PrimaryColor,
        SecondaryColor = s.SecondaryColor
    };
}

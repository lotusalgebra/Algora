using System.Text.Json;
using Algora.Application.DTOs.Bundles;
using Algora.Application.DTOs.Inventory;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for managing product bundles.
/// </summary>
public class BundleService : IBundleService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BundleService> _logger;

    public BundleService(AppDbContext db, ILogger<BundleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region Bundle CRUD

    public async Task<PaginatedResult<BundleListDto>> GetBundlesAsync(
        string shopDomain,
        string? bundleType = null,
        string? status = null,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _db.Bundles
            .Where(b => b.ShopDomain == shopDomain);

        if (!string.IsNullOrEmpty(bundleType))
            query = query.Where(b => b.BundleType == bundleType);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(b => b.Status == status);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(b => b.Name.Contains(search) || (b.Description != null && b.Description.Contains(search)));

        var totalCount = await query.CountAsync();

        var bundles = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(b => b.Items)
            .Select(b => new BundleListDto
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                BundleType = b.BundleType,
                Status = b.Status,
                ImageUrl = b.ImageUrl,
                OriginalPrice = b.OriginalPrice,
                BundlePrice = b.BundlePrice,
                SavingsPercent = b.OriginalPrice > 0 ? ((b.OriginalPrice - b.BundlePrice) / b.OriginalPrice) * 100 : 0,
                ItemCount = b.Items.Count,
                AvailableQuantity = 0, // Will be calculated separately
                IsActive = b.IsActive,
                ShopifySyncStatus = b.ShopifySyncStatus,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResult<BundleListDto>
        {
            Items = bundles,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<BundleDto>> GetActiveBundlesAsync(string shopDomain)
    {
        var bundles = await _db.Bundles
            .Where(b => b.ShopDomain == shopDomain && b.IsActive && b.Status == "active")
            .OrderBy(b => b.DisplayOrder)
            .Include(b => b.Items)
            .Include(b => b.Rules)
                .ThenInclude(r => r.Tiers)
            .ToListAsync();

        return bundles.Select(MapToDto).ToList();
    }

    public async Task<BundleDto?> GetBundleByIdAsync(int bundleId)
    {
        var bundle = await _db.Bundles
            .Include(b => b.Items.OrderBy(i => i.DisplayOrder))
            .Include(b => b.Rules.OrderBy(r => r.DisplayOrder))
                .ThenInclude(r => r.Tiers.OrderBy(t => t.MinQuantity))
            .FirstOrDefaultAsync(b => b.Id == bundleId);

        return bundle != null ? MapToDto(bundle) : null;
    }

    public async Task<BundleDto?> GetBundleBySlugAsync(string shopDomain, string slug)
    {
        var bundle = await _db.Bundles
            .Include(b => b.Items.OrderBy(i => i.DisplayOrder))
            .Include(b => b.Rules.OrderBy(r => r.DisplayOrder))
                .ThenInclude(r => r.Tiers.OrderBy(t => t.MinQuantity))
            .FirstOrDefaultAsync(b => b.ShopDomain == shopDomain && b.Slug == slug);

        return bundle != null ? MapToDto(bundle) : null;
    }

    public async Task<BundleDto> CreateBundleAsync(string shopDomain, CreateBundleDto dto)
    {
        var slug = dto.Slug ?? GenerateSlug(dto.Name);

        // Ensure unique slug
        var baseSlug = slug;
        var counter = 1;
        while (await _db.Bundles.AnyAsync(b => b.ShopDomain == shopDomain && b.Slug == slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        var bundle = new Bundle
        {
            ShopDomain = shopDomain,
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            BundleType = dto.BundleType,
            Status = "draft",
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            DiscountCode = dto.DiscountCode,
            ImageUrl = dto.ImageUrl,
            MinItems = dto.MinItems,
            MaxItems = dto.MaxItems,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Bundles.Add(bundle);
        await _db.SaveChangesAsync();

        // Add items for fixed bundles
        if (dto.BundleType == "fixed" && dto.Items.Any())
        {
            foreach (var itemDto in dto.Items)
            {
                var item = new BundleItem
                {
                    BundleId = bundle.Id,
                    PlatformProductId = itemDto.PlatformProductId,
                    PlatformVariantId = itemDto.PlatformVariantId,
                    ProductTitle = itemDto.ProductTitle,
                    VariantTitle = itemDto.VariantTitle,
                    Sku = itemDto.Sku,
                    ImageUrl = itemDto.ImageUrl,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    DisplayOrder = itemDto.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };
                _db.BundleItems.Add(item);
            }
        }

        // Add rules for mix-and-match bundles
        if (dto.BundleType == "mix_and_match" && dto.Rules.Any())
        {
            foreach (var ruleDto in dto.Rules)
            {
                var rule = new BundleRule
                {
                    BundleId = bundle.Id,
                    Name = ruleDto.Name,
                    EligibleProductIds = ruleDto.EligibleProductIds.Any() ? JsonSerializer.Serialize(ruleDto.EligibleProductIds) : null,
                    EligibleCollectionIds = ruleDto.EligibleCollectionIds.Any() ? JsonSerializer.Serialize(ruleDto.EligibleCollectionIds) : null,
                    EligibleTags = ruleDto.EligibleTags.Any() ? JsonSerializer.Serialize(ruleDto.EligibleTags) : null,
                    MinQuantity = ruleDto.MinQuantity,
                    MaxQuantity = ruleDto.MaxQuantity,
                    AllowDuplicates = ruleDto.AllowDuplicates,
                    DisplayOrder = ruleDto.DisplayOrder,
                    DisplayLabel = ruleDto.DisplayLabel,
                    CreatedAt = DateTime.UtcNow
                };
                _db.BundleRules.Add(rule);
                await _db.SaveChangesAsync();

                foreach (var tierDto in ruleDto.Tiers)
                {
                    var tier = new BundleRuleTier
                    {
                        BundleRuleId = rule.Id,
                        MinQuantity = tierDto.MinQuantity,
                        MaxQuantity = tierDto.MaxQuantity,
                        DiscountType = tierDto.DiscountType,
                        DiscountValue = tierDto.DiscountValue,
                        DisplayLabel = tierDto.DisplayLabel,
                        DisplayOrder = tierDto.DisplayOrder,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.BundleRuleTiers.Add(tier);
                }
            }
        }

        await _db.SaveChangesAsync();

        // Calculate and update prices
        await UpdateBundlePricesAsync(bundle.Id);

        return (await GetBundleByIdAsync(bundle.Id))!;
    }

    public async Task<BundleDto?> UpdateBundleAsync(int bundleId, UpdateBundleDto dto)
    {
        var bundle = await _db.Bundles.FindAsync(bundleId);
        if (bundle == null) return null;

        if (dto.Name != null) bundle.Name = dto.Name;
        if (dto.Slug != null) bundle.Slug = dto.Slug;
        if (dto.Description != null) bundle.Description = dto.Description;
        if (dto.DiscountType != null) bundle.DiscountType = dto.DiscountType;
        if (dto.DiscountValue.HasValue) bundle.DiscountValue = dto.DiscountValue.Value;
        if (dto.DiscountCode != null) bundle.DiscountCode = dto.DiscountCode;
        if (dto.ImageUrl != null) bundle.ImageUrl = dto.ImageUrl;
        if (dto.ThumbnailUrl != null) bundle.ThumbnailUrl = dto.ThumbnailUrl;
        if (dto.DisplayOrder.HasValue) bundle.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.IsActive.HasValue) bundle.IsActive = dto.IsActive.Value;
        if (dto.Status != null) bundle.Status = dto.Status;
        if (dto.MinItems.HasValue) bundle.MinItems = dto.MinItems;
        if (dto.MaxItems.HasValue) bundle.MaxItems = dto.MaxItems;

        bundle.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await UpdateBundlePricesAsync(bundleId);

        return await GetBundleByIdAsync(bundleId);
    }

    public async Task<bool> DeleteBundleAsync(int bundleId)
    {
        var bundle = await _db.Bundles
            .Include(b => b.Items)
            .Include(b => b.Rules)
                .ThenInclude(r => r.Tiers)
            .FirstOrDefaultAsync(b => b.Id == bundleId);

        if (bundle == null) return false;

        _db.Bundles.Remove(bundle);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ArchiveBundleAsync(int bundleId)
    {
        var bundle = await _db.Bundles.FindAsync(bundleId);
        if (bundle == null) return false;

        bundle.Status = "archived";
        bundle.IsActive = false;
        bundle.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateBundleAsync(int bundleId)
    {
        var bundle = await _db.Bundles.FindAsync(bundleId);
        if (bundle == null) return false;

        bundle.Status = "active";
        bundle.IsActive = true;
        bundle.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Bundle Items

    public async Task<BundleItemDto?> AddBundleItemAsync(int bundleId, CreateBundleItemDto dto)
    {
        var bundle = await _db.Bundles.FindAsync(bundleId);
        if (bundle == null || bundle.BundleType != "fixed") return null;

        var item = new BundleItem
        {
            BundleId = bundleId,
            PlatformProductId = dto.PlatformProductId,
            PlatformVariantId = dto.PlatformVariantId,
            ProductTitle = dto.ProductTitle,
            VariantTitle = dto.VariantTitle,
            Sku = dto.Sku,
            ImageUrl = dto.ImageUrl,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.BundleItems.Add(item);
        await _db.SaveChangesAsync();
        await UpdateBundlePricesAsync(bundleId);

        return MapItemToDto(item);
    }

    public async Task<bool> RemoveBundleItemAsync(int itemId)
    {
        var item = await _db.BundleItems.FindAsync(itemId);
        if (item == null) return false;

        var bundleId = item.BundleId;
        _db.BundleItems.Remove(item);
        await _db.SaveChangesAsync();
        await UpdateBundlePricesAsync(bundleId);

        return true;
    }

    public async Task<BundleItemDto?> UpdateBundleItemAsync(int itemId, int? quantity, int? displayOrder)
    {
        var item = await _db.BundleItems.FindAsync(itemId);
        if (item == null) return null;

        if (quantity.HasValue) item.Quantity = quantity.Value;
        if (displayOrder.HasValue) item.DisplayOrder = displayOrder.Value;

        await _db.SaveChangesAsync();
        await UpdateBundlePricesAsync(item.BundleId);

        return MapItemToDto(item);
    }

    #endregion

    #region Bundle Rules

    public async Task<BundleRuleDto?> AddBundleRuleAsync(int bundleId, CreateBundleRuleDto dto)
    {
        var bundle = await _db.Bundles.FindAsync(bundleId);
        if (bundle == null || bundle.BundleType != "mix_and_match") return null;

        var rule = new BundleRule
        {
            BundleId = bundleId,
            Name = dto.Name,
            EligibleProductIds = dto.EligibleProductIds.Any() ? JsonSerializer.Serialize(dto.EligibleProductIds) : null,
            EligibleCollectionIds = dto.EligibleCollectionIds.Any() ? JsonSerializer.Serialize(dto.EligibleCollectionIds) : null,
            EligibleTags = dto.EligibleTags.Any() ? JsonSerializer.Serialize(dto.EligibleTags) : null,
            MinQuantity = dto.MinQuantity,
            MaxQuantity = dto.MaxQuantity,
            AllowDuplicates = dto.AllowDuplicates,
            DisplayOrder = dto.DisplayOrder,
            DisplayLabel = dto.DisplayLabel,
            CreatedAt = DateTime.UtcNow
        };

        _db.BundleRules.Add(rule);
        await _db.SaveChangesAsync();

        foreach (var tierDto in dto.Tiers)
        {
            var tier = new BundleRuleTier
            {
                BundleRuleId = rule.Id,
                MinQuantity = tierDto.MinQuantity,
                MaxQuantity = tierDto.MaxQuantity,
                DiscountType = tierDto.DiscountType,
                DiscountValue = tierDto.DiscountValue,
                DisplayLabel = tierDto.DisplayLabel,
                DisplayOrder = tierDto.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };
            _db.BundleRuleTiers.Add(tier);
        }

        await _db.SaveChangesAsync();

        return await GetBundleRuleAsync(rule.Id);
    }

    public async Task<bool> RemoveBundleRuleAsync(int ruleId)
    {
        var rule = await _db.BundleRules
            .Include(r => r.Tiers)
            .FirstOrDefaultAsync(r => r.Id == ruleId);

        if (rule == null) return false;

        _db.BundleRules.Remove(rule);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<EligibleProductDto>> GetEligibleProductsAsync(int bundleId, int? ruleId = null)
    {
        var bundle = await _db.Bundles
            .Include(b => b.Rules)
            .FirstOrDefaultAsync(b => b.Id == bundleId);

        if (bundle == null) return new List<EligibleProductDto>();

        var productIds = new HashSet<long>();

        var rules = ruleId.HasValue
            ? bundle.Rules.Where(r => r.Id == ruleId.Value)
            : bundle.Rules;

        foreach (var rule in rules)
        {
            if (!string.IsNullOrEmpty(rule.EligibleProductIds))
            {
                var ids = JsonSerializer.Deserialize<List<long>>(rule.EligibleProductIds);
                if (ids != null)
                {
                    foreach (var id in ids)
                        productIds.Add(id);
                }
            }
        }

        var settings = await GetOrCreateSettingsAsync(bundle.ShopDomain);

        var products = await _db.Products
            .Where(p => p.ShopDomain == bundle.ShopDomain && productIds.Contains(p.PlatformProductId))
            .Select(p => new EligibleProductDto
            {
                PlatformProductId = p.PlatformProductId,
                ProductTitle = p.Title,
                ImageUrl = null, // Would need to join with images
                Price = p.Price,
                InventoryQuantity = p.InventoryQuantity,
                IsLowStock = p.InventoryQuantity <= settings.LowInventoryThreshold && p.InventoryQuantity > 0,
                IsOutOfStock = p.InventoryQuantity <= 0
            })
            .ToListAsync();

        return products;
    }

    #endregion

    #region Price Calculation

    public async Task<BundlePriceCalculationDto> CalculateBundlePriceAsync(CustomerBundleSelectionDto selection)
    {
        var bundle = await _db.Bundles
            .Include(b => b.Rules)
                .ThenInclude(r => r.Tiers)
            .FirstOrDefaultAsync(b => b.Id == selection.BundleId);

        if (bundle == null)
        {
            return new BundlePriceCalculationDto
            {
                BundleId = selection.BundleId,
                IsValid = false,
                ValidationError = "Bundle not found"
            };
        }

        var items = new List<SelectedItemDetailDto>();
        decimal originalPrice = 0;

        foreach (var selected in selection.SelectedItems)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.PlatformProductId == selected.PlatformProductId);

            if (product != null)
            {
                var lineTotal = product.Price * selected.Quantity;
                items.Add(new SelectedItemDetailDto
                {
                    PlatformProductId = selected.PlatformProductId,
                    PlatformVariantId = selected.PlatformVariantId,
                    ProductTitle = product.Title,
                    Quantity = selected.Quantity,
                    UnitPrice = product.Price,
                    LineTotal = lineTotal
                });
                originalPrice += lineTotal;
            }
        }

        var totalQuantity = selection.SelectedItems.Sum(i => i.Quantity);

        // Validate quantity constraints
        if (bundle.MinItems.HasValue && totalQuantity < bundle.MinItems.Value)
        {
            return new BundlePriceCalculationDto
            {
                BundleId = bundle.Id,
                OriginalPrice = originalPrice,
                Items = items,
                IsValid = false,
                ValidationError = $"Minimum {bundle.MinItems} items required"
            };
        }

        if (bundle.MaxItems.HasValue && totalQuantity > bundle.MaxItems.Value)
        {
            return new BundlePriceCalculationDto
            {
                BundleId = bundle.Id,
                OriginalPrice = originalPrice,
                Items = items,
                IsValid = false,
                ValidationError = $"Maximum {bundle.MaxItems} items allowed"
            };
        }

        // Calculate discount
        decimal discountAmount = 0;
        string? appliedTierLabel = null;

        if (bundle.BundleType == "mix_and_match" && bundle.Rules.Any())
        {
            // Find applicable tier
            var rule = bundle.Rules.FirstOrDefault();
            if (rule != null)
            {
                var tier = rule.Tiers
                    .Where(t => totalQuantity >= t.MinQuantity && (!t.MaxQuantity.HasValue || totalQuantity <= t.MaxQuantity.Value))
                    .OrderByDescending(t => t.MinQuantity)
                    .FirstOrDefault();

                if (tier != null)
                {
                    discountAmount = tier.DiscountType == "percentage"
                        ? originalPrice * (tier.DiscountValue / 100)
                        : tier.DiscountValue;
                    appliedTierLabel = tier.DisplayLabel;
                }
            }
        }
        else
        {
            // Fixed bundle discount
            discountAmount = bundle.DiscountType == "percentage"
                ? originalPrice * (bundle.DiscountValue / 100)
                : bundle.DiscountValue;
        }

        var finalPrice = originalPrice - discountAmount;

        return new BundlePriceCalculationDto
        {
            BundleId = bundle.Id,
            OriginalPrice = originalPrice,
            DiscountAmount = discountAmount,
            FinalPrice = finalPrice,
            SavingsPercent = originalPrice > 0 ? (discountAmount / originalPrice) * 100 : 0,
            DiscountCode = bundle.DiscountCode,
            AppliedTierLabel = appliedTierLabel,
            IsValid = true,
            Items = items
        };
    }

    public async Task<BundleCartUrlDto> GenerateCartUrlAsync(string shopDomain, CustomerBundleSelectionDto selection)
    {
        var bundle = await _db.Bundles.FindAsync(selection.BundleId);
        if (bundle == null)
        {
            return new BundleCartUrlDto { CartUrl = string.Empty };
        }

        var cartItems = new List<string>();
        decimal totalPrice = 0;
        int totalItems = 0;

        foreach (var item in selection.SelectedItems)
        {
            var variantId = item.PlatformVariantId ?? item.PlatformProductId;
            cartItems.Add($"{variantId}:{item.Quantity}");
            totalItems += item.Quantity;

            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.PlatformProductId == item.PlatformProductId);
            if (product != null)
            {
                totalPrice += product.Price * item.Quantity;
            }
        }

        var cartUrl = $"https://{shopDomain}/cart/{string.Join(",", cartItems)}";

        if (!string.IsNullOrEmpty(bundle.DiscountCode))
        {
            cartUrl += $"?discount={bundle.DiscountCode}";
        }

        return new BundleCartUrlDto
        {
            CartUrl = cartUrl,
            DiscountCode = bundle.DiscountCode,
            TotalPrice = totalPrice,
            TotalItems = totalItems
        };
    }

    public async Task<int> CalculateAvailableQuantityAsync(int bundleId)
    {
        var items = await _db.BundleItems
            .Where(i => i.BundleId == bundleId)
            .ToListAsync();

        if (!items.Any()) return 0;

        return items.Min(i => i.CurrentInventory / Math.Max(i.Quantity, 1));
    }

    #endregion

    #region Inventory

    public async Task UpdateBundleInventoryAsync(int bundleId)
    {
        var bundle = await _db.Bundles
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == bundleId);

        if (bundle == null || bundle.BundleType != "fixed") return;

        foreach (var item in bundle.Items)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.PlatformProductId == item.PlatformProductId);

            if (product != null)
            {
                item.CurrentInventory = product.InventoryQuantity;
                item.InventoryCheckedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpdateAllBundleInventoryAsync(string shopDomain)
    {
        var bundles = await _db.Bundles
            .Where(b => b.ShopDomain == shopDomain && b.BundleType == "fixed")
            .Select(b => b.Id)
            .ToListAsync();

        foreach (var bundleId in bundles)
        {
            await UpdateBundleInventoryAsync(bundleId);
        }
    }

    #endregion

    #region Settings

    public async Task<BundleSettingsDto> GetSettingsAsync(string shopDomain)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);
        return MapSettingsToDto(settings);
    }

    public async Task<BundleSettingsDto> UpdateSettingsAsync(string shopDomain, UpdateBundleSettingsDto dto)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);

        if (dto.IsEnabled.HasValue) settings.IsEnabled = dto.IsEnabled.Value;
        if (dto.DefaultDiscountType != null) settings.DefaultDiscountType = dto.DefaultDiscountType;
        if (dto.DefaultDiscountValue.HasValue) settings.DefaultDiscountValue = dto.DefaultDiscountValue.Value;
        if (dto.ShowInventoryWarnings.HasValue) settings.ShowInventoryWarnings = dto.ShowInventoryWarnings.Value;
        if (dto.LowInventoryThreshold.HasValue) settings.LowInventoryThreshold = dto.LowInventoryThreshold.Value;
        if (dto.HideOutOfStock.HasValue) settings.HideOutOfStock = dto.HideOutOfStock.Value;
        if (dto.BundlePageTitle != null) settings.BundlePageTitle = dto.BundlePageTitle;
        if (dto.BundlePageDescription != null) settings.BundlePageDescription = dto.BundlePageDescription;
        if (dto.DisplayLayout != null) settings.DisplayLayout = dto.DisplayLayout;
        if (dto.BundlesPerPage.HasValue) settings.BundlesPerPage = dto.BundlesPerPage.Value;
        if (dto.ShowOnStorefront.HasValue) settings.ShowOnStorefront = dto.ShowOnStorefront.Value;
        if (dto.PrimaryColor != null) settings.PrimaryColor = dto.PrimaryColor;
        if (dto.SecondaryColor != null) settings.SecondaryColor = dto.SecondaryColor;
        if (dto.CustomCss != null) settings.CustomCss = dto.CustomCss;
        if (dto.AutoSyncToShopify.HasValue) settings.AutoSyncToShopify = dto.AutoSyncToShopify.Value;
        if (dto.ShopifyProductType != null) settings.ShopifyProductType = dto.ShopifyProductType;
        if (dto.ShopifyProductTags != null) settings.ShopifyProductTags = dto.ShopifyProductTags;

        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapSettingsToDto(settings);
    }

    private async Task<BundleSettings> GetOrCreateSettingsAsync(string shopDomain)
    {
        var settings = await _db.BundleSettings
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings == null)
        {
            settings = new BundleSettings
            {
                ShopDomain = shopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _db.BundleSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        return settings;
    }

    #endregion

    #region Analytics

    public async Task<BundleAnalyticsSummaryDto> GetAnalyticsSummaryAsync(string shopDomain)
    {
        var bundles = await _db.Bundles
            .Where(b => b.ShopDomain == shopDomain)
            .ToListAsync();

        return new BundleAnalyticsSummaryDto
        {
            TotalBundles = bundles.Count,
            ActiveBundles = bundles.Count(b => b.IsActive && b.Status == "active"),
            FixedBundles = bundles.Count(b => b.BundleType == "fixed"),
            MixAndMatchBundles = bundles.Count(b => b.BundleType == "mix_and_match"),
            SyncedToShopify = bundles.Count(b => b.ShopifySyncStatus == "synced"),
            // Revenue tracking would require order integration
            TotalRevenue = 0,
            TotalOrders = 0,
            AverageOrderValue = 0,
            TopBundles = new List<BundlePerformanceDto>()
        };
    }

    public async Task<BundlePerformanceDto?> GetBundlePerformanceAsync(int bundleId)
    {
        var bundle = await _db.Bundles.FindAsync(bundleId);
        if (bundle == null) return null;

        return new BundlePerformanceDto
        {
            BundleId = bundle.Id,
            BundleName = bundle.Name,
            Views = 0,
            AddToCarts = 0,
            Orders = 0,
            Revenue = 0,
            ConversionRate = 0
        };
    }

    #endregion

    #region Private Helpers

    private async Task UpdateBundlePricesAsync(int bundleId)
    {
        var bundle = await _db.Bundles
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == bundleId);

        if (bundle == null) return;

        if (bundle.BundleType == "fixed")
        {
            bundle.OriginalPrice = bundle.Items.Sum(i => i.UnitPrice * i.Quantity);

            var discountAmount = bundle.DiscountType == "percentage"
                ? bundle.OriginalPrice * (bundle.DiscountValue / 100)
                : bundle.DiscountValue;

            bundle.BundlePrice = bundle.OriginalPrice - discountAmount;
        }

        bundle.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("&", "and");
    }

    private async Task<BundleRuleDto?> GetBundleRuleAsync(int ruleId)
    {
        var rule = await _db.BundleRules
            .Include(r => r.Tiers)
            .FirstOrDefaultAsync(r => r.Id == ruleId);

        return rule != null ? MapRuleToDto(rule) : null;
    }

    private BundleDto MapToDto(Bundle bundle)
    {
        var savingsAmount = bundle.OriginalPrice - bundle.BundlePrice;
        var savingsPercent = bundle.OriginalPrice > 0 ? (savingsAmount / bundle.OriginalPrice) * 100 : 0;

        return new BundleDto
        {
            Id = bundle.Id,
            ShopDomain = bundle.ShopDomain,
            Name = bundle.Name,
            Slug = bundle.Slug,
            Description = bundle.Description,
            BundleType = bundle.BundleType,
            Status = bundle.Status,
            DiscountType = bundle.DiscountType,
            DiscountValue = bundle.DiscountValue,
            DiscountCode = bundle.DiscountCode,
            ImageUrl = bundle.ImageUrl,
            ThumbnailUrl = bundle.ThumbnailUrl,
            DisplayOrder = bundle.DisplayOrder,
            IsActive = bundle.IsActive,
            ShopifyProductId = bundle.ShopifyProductId,
            ShopifyVariantId = bundle.ShopifyVariantId,
            ShopifySyncStatus = bundle.ShopifySyncStatus,
            ShopifySyncError = bundle.ShopifySyncError,
            ShopifySyncedAt = bundle.ShopifySyncedAt,
            MinItems = bundle.MinItems,
            MaxItems = bundle.MaxItems,
            OriginalPrice = bundle.OriginalPrice,
            BundlePrice = bundle.BundlePrice,
            SavingsAmount = savingsAmount,
            SavingsPercent = savingsPercent,
            Currency = bundle.Currency,
            CreatedAt = bundle.CreatedAt,
            UpdatedAt = bundle.UpdatedAt,
            Items = bundle.Items.Select(MapItemToDto).ToList(),
            Rules = bundle.Rules.Select(MapRuleToDto).ToList()
        };
    }

    private static BundleItemDto MapItemToDto(BundleItem item)
    {
        return new BundleItemDto
        {
            Id = item.Id,
            BundleId = item.BundleId,
            PlatformProductId = item.PlatformProductId,
            PlatformVariantId = item.PlatformVariantId,
            ProductTitle = item.ProductTitle,
            VariantTitle = item.VariantTitle,
            Sku = item.Sku,
            ImageUrl = item.ImageUrl,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineTotal = item.UnitPrice * item.Quantity,
            DisplayOrder = item.DisplayOrder,
            CurrentInventory = item.CurrentInventory,
            IsLowStock = item.CurrentInventory <= 5 && item.CurrentInventory > 0,
            IsOutOfStock = item.CurrentInventory <= 0
        };
    }

    private static BundleRuleDto MapRuleToDto(BundleRule rule)
    {
        return new BundleRuleDto
        {
            Id = rule.Id,
            BundleId = rule.BundleId,
            Name = rule.Name,
            EligibleProductIds = !string.IsNullOrEmpty(rule.EligibleProductIds)
                ? JsonSerializer.Deserialize<List<long>>(rule.EligibleProductIds) ?? new List<long>()
                : new List<long>(),
            EligibleCollectionIds = !string.IsNullOrEmpty(rule.EligibleCollectionIds)
                ? JsonSerializer.Deserialize<List<long>>(rule.EligibleCollectionIds) ?? new List<long>()
                : new List<long>(),
            EligibleTags = !string.IsNullOrEmpty(rule.EligibleTags)
                ? JsonSerializer.Deserialize<List<string>>(rule.EligibleTags) ?? new List<string>()
                : new List<string>(),
            MinQuantity = rule.MinQuantity,
            MaxQuantity = rule.MaxQuantity,
            AllowDuplicates = rule.AllowDuplicates,
            DisplayOrder = rule.DisplayOrder,
            DisplayLabel = rule.DisplayLabel,
            Tiers = rule.Tiers.Select(MapTierToDto).ToList()
        };
    }

    private static BundleRuleTierDto MapTierToDto(BundleRuleTier tier)
    {
        return new BundleRuleTierDto
        {
            Id = tier.Id,
            BundleRuleId = tier.BundleRuleId,
            MinQuantity = tier.MinQuantity,
            MaxQuantity = tier.MaxQuantity,
            DiscountType = tier.DiscountType,
            DiscountValue = tier.DiscountValue,
            DisplayLabel = tier.DisplayLabel,
            DisplayOrder = tier.DisplayOrder
        };
    }

    private static BundleSettingsDto MapSettingsToDto(BundleSettings settings)
    {
        return new BundleSettingsDto
        {
            Id = settings.Id,
            ShopDomain = settings.ShopDomain,
            IsEnabled = settings.IsEnabled,
            DefaultDiscountType = settings.DefaultDiscountType,
            DefaultDiscountValue = settings.DefaultDiscountValue,
            ShowInventoryWarnings = settings.ShowInventoryWarnings,
            LowInventoryThreshold = settings.LowInventoryThreshold,
            HideOutOfStock = settings.HideOutOfStock,
            BundlePageTitle = settings.BundlePageTitle,
            BundlePageDescription = settings.BundlePageDescription,
            DisplayLayout = settings.DisplayLayout,
            BundlesPerPage = settings.BundlesPerPage,
            ShowOnStorefront = settings.ShowOnStorefront,
            PrimaryColor = settings.PrimaryColor,
            SecondaryColor = settings.SecondaryColor,
            CustomCss = settings.CustomCss,
            AutoSyncToShopify = settings.AutoSyncToShopify,
            ShopifyProductType = settings.ShopifyProductType,
            ShopifyProductTags = settings.ShopifyProductTags
        };
    }

    #endregion
}

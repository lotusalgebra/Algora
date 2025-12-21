using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for managing shop records and credentials.
/// Supports per-shop custom Shopify credentials with fallback to app defaults.
/// </summary>
public class ShopService : IShopService
{
    private readonly AppDbContext _db;
    private readonly ShopifyOptions _defaultOptions;
    private readonly ILogger<ShopService> _logger;

    public ShopService(
        AppDbContext db,
        IOptions<ShopifyOptions> defaultOptions,
        ILogger<ShopService> logger)
    {
        _db = db;
        _defaultOptions = defaultOptions.Value;
        _logger = logger;
    }

    public async Task<ShopDto?> GetShopAsync(string shopDomain)
    {
        var shop = await _db.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Domain == shopDomain);
        return shop is null ? null : MapToDto(shop);
    }

    public async Task<ShopDto?> GetShopByIdAsync(Guid shopId)
    {
        var shop = await _db.Shops.FindAsync(shopId);
        return shop is null ? null : MapToDto(shop);
    }

    public async Task<IEnumerable<ShopDto>> GetAllShopsAsync(bool activeOnly = true)
    {
        var query = _db.Shops.AsNoTracking();
        if (activeOnly) query = query.Where(s => s.IsActive);
        return await query.OrderBy(s => s.Domain).Select(s => MapToDto(s)).ToListAsync();
    }

    public async Task<ShopCredentialsDto> GetShopCredentialsAsync(string shopDomain)
    {
        var shop = await _db.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Domain == shopDomain);

        // If shop exists and uses custom credentials, return them
        if (shop is not null && shop.UseCustomCredentials &&
            !string.IsNullOrEmpty(shop.CustomApiKey) &&
            !string.IsNullOrEmpty(shop.CustomApiSecret))
        {
            return new ShopCredentialsDto
            {
                ApiKey = shop.CustomApiKey,
                ApiSecret = shop.CustomApiSecret,
                Scopes = shop.CustomScopes ?? _defaultOptions.Scopes,
                AppUrl = shop.CustomAppUrl ?? _defaultOptions.AppUrl,
                IsCustom = true
            };
        }

        // Return app defaults
        return new ShopCredentialsDto
        {
            ApiKey = _defaultOptions.ApiKey,
            ApiSecret = _defaultOptions.ApiSecret,
            Scopes = _defaultOptions.Scopes,
            AppUrl = _defaultOptions.AppUrl,
            IsCustom = false
        };
    }

    public async Task<ShopDto> UpdateShopCredentialsAsync(string shopDomain, UpdateShopCredentialsDto dto)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain)
            ?? throw new InvalidOperationException($"Shop {shopDomain} not found");

        if (dto.ApiKey is not null) shop.CustomApiKey = dto.ApiKey;
        if (dto.ApiSecret is not null) shop.CustomApiSecret = dto.ApiSecret;
        if (dto.Scopes is not null) shop.CustomScopes = dto.Scopes;
        if (dto.AppUrl is not null) shop.CustomAppUrl = dto.AppUrl;
        shop.UseCustomCredentials = dto.UseCustomCredentials;
        shop.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated Shopify credentials for shop {ShopDomain}, UseCustom: {UseCustom}",
            shopDomain, dto.UseCustomCredentials);

        return MapToDto(shop);
    }

    public async Task<bool> ClearCustomCredentialsAsync(string shopDomain)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);
        if (shop is null) return false;

        shop.CustomApiKey = null;
        shop.CustomApiSecret = null;
        shop.CustomScopes = null;
        shop.CustomAppUrl = null;
        shop.UseCustomCredentials = false;
        shop.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Cleared custom Shopify credentials for shop {ShopDomain}", shopDomain);
        return true;
    }

    public async Task<ShopDto> UpdateShopInfoAsync(string shopDomain, UpdateShopInfoDto dto)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain)
            ?? throw new InvalidOperationException($"Shop {shopDomain} not found");

        if (dto.ShopName is not null) shop.ShopName = dto.ShopName;
        if (dto.Email is not null) shop.Email = dto.Email;
        if (dto.PrimaryLocale is not null) shop.PrimaryLocale = dto.PrimaryLocale;
        if (dto.Timezone is not null) shop.Timezone = dto.Timezone;
        if (dto.Currency is not null) shop.Currency = dto.Currency;
        if (dto.Country is not null) shop.Country = dto.Country;
        if (dto.PlanName is not null) shop.PlanName = dto.PlanName;
        shop.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(shop);
    }

    public async Task<bool> DeactivateShopAsync(string shopDomain)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);
        if (shop is null) return false;

        shop.IsActive = false;
        shop.OfflineAccessToken = null; // Clear token on uninstall
        shop.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Deactivated shop {ShopDomain}", shopDomain);
        return true;
    }

    public async Task<ShopDto> SyncShopInfoAsync(string shopDomain)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain)
            ?? throw new InvalidOperationException($"Shop {shopDomain} not found");

        // TODO: Call Shopify API to get shop info
        // For now, just update the sync timestamp
        shop.LastSyncedAt = DateTime.UtcNow;
        shop.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Synced shop info for {ShopDomain}", shopDomain);
        return MapToDto(shop);
    }

    private static ShopDto MapToDto(Shop s) => new()
    {
        Id = s.Id,
        Domain = s.Domain,
        ShopName = s.ShopName,
        Email = s.Email,
        PrimaryLocale = s.PrimaryLocale,
        Timezone = s.Timezone,
        Currency = s.Currency,
        Country = s.Country,
        PlanName = s.PlanName,
        IsActive = s.IsActive,
        UseCustomCredentials = s.UseCustomCredentials,
        HasCustomCredentials = !string.IsNullOrEmpty(s.CustomApiKey),
        InstalledAt = s.InstalledAt,
        LastSyncedAt = s.LastSyncedAt
    };
}
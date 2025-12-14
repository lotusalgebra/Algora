using Algora.Application.DTOs;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing shop records and credentials.
/// </summary>
public interface IShopService
{
    Task<ShopDto?> GetShopAsync(string shopDomain);
    Task<ShopDto?> GetShopByIdAsync(Guid shopId);
    Task<IEnumerable<ShopDto>> GetAllShopsAsync(bool activeOnly = true);

    /// <summary>
    /// Gets effective Shopify credentials for a shop (custom or app default).
    /// </summary>
    Task<ShopCredentialsDto> GetShopCredentialsAsync(string shopDomain);

    /// <summary>
    /// Updates custom Shopify credentials for a shop.
    /// </summary>
    Task<ShopDto> UpdateShopCredentialsAsync(string shopDomain, UpdateShopCredentialsDto dto);

    /// <summary>
    /// Clears custom credentials and reverts to app defaults.
    /// </summary>
    Task<bool> ClearCustomCredentialsAsync(string shopDomain);

    /// <summary>
    /// Updates shop info (from Shopify sync or manual update).
    /// </summary>
    Task<ShopDto> UpdateShopInfoAsync(string shopDomain, UpdateShopInfoDto dto);

    /// <summary>
    /// Marks a shop as uninstalled.
    /// </summary>
    Task<bool> DeactivateShopAsync(string shopDomain);

    /// <summary>
    /// Syncs shop info from Shopify API.
    /// </summary>
    Task<ShopDto> SyncShopInfoAsync(string shopDomain);
}
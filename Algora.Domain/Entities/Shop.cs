using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Domain.Entities;

/// <summary>
/// Represents a persisted shop record for an installed merchant.
/// Stores identifying information, access tokens, and optional custom app credentials.
/// </summary>
public class Shop
{
    /// <summary>
    /// Primary identifier for the shop record in the local database.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The shop's myshopify domain (e.g., "example-shop.myshopify.com").
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// The offline access token returned by Shopify after OAuth.
    /// </summary>
    public string? OfflineAccessToken { get; set; }

    /// <summary>
    /// UTC timestamp when the app was installed for this shop.
    /// </summary>
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    // ===== Custom Shopify App Credentials (optional per-shop override) =====

    /// <summary>
    /// Custom Shopify API Key for this shop. If null, uses app default.
    /// </summary>
    public string? CustomApiKey { get; set; }

    /// <summary>
    /// Custom Shopify API Secret for this shop. If null, uses app default.
    /// </summary>
    public string? CustomApiSecret { get; set; }

    /// <summary>
    /// Custom OAuth scopes for this shop. If null, uses app default.
    /// </summary>
    public string? CustomScopes { get; set; }

    /// <summary>
    /// Custom App URL for this shop (for OAuth redirects). If null, uses app default.
    /// </summary>
    public string? CustomAppUrl { get; set; }

    /// <summary>
    /// Whether this shop uses custom Shopify app credentials.
    /// </summary>
    public bool UseCustomCredentials { get; set; }

    // ===== Shop Info (cached from Shopify) =====

    /// <summary>
    /// Shop name from Shopify.
    /// </summary>
    public string? ShopName { get; set; }

    /// <summary>
    /// Shop owner email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Shop's primary locale.
    /// </summary>
    public string? PrimaryLocale { get; set; }

    /// <summary>
    /// Shop's timezone.
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// Shop's currency code.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Shop's country code.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Shop's Shopify plan name.
    /// </summary>
    public string? PlanName { get; set; }

    /// <summary>
    /// Whether the shop is active (not uninstalled).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the shop was last synced with Shopify.
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

namespace Algora.Auth.Models;

/// <summary>
/// Configuration settings for authentication behavior.
/// </summary>
public class AuthSettings
{
    /// <summary>
    /// Whether to automatically create a shop record during registration if it doesn't exist.
    /// Should be false in production - shops should be created via Shopify OAuth install flow.
    /// </summary>
    public bool AllowAutoCreateShop { get; set; } = false;
}
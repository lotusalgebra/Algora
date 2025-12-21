namespace Algora.Infrastructure.Services.Scrapers;

/// <summary>
/// Configuration options for scraping API services
/// </summary>
public class ScraperApiOptions
{
    public const string SectionName = "ScraperApi";

    /// <summary>
    /// The scraping API provider to use (ScraperAPI, Zyte, BrightData)
    /// </summary>
    public string Provider { get; set; } = "ScraperAPI";

    /// <summary>
    /// API key for the scraping service
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether to enable JavaScript rendering (slower but more accurate)
    /// </summary>
    public bool RenderJs { get; set; } = true;

    /// <summary>
    /// Country code for geo-targeting (e.g., "us", "uk", "de")
    /// </summary>
    public string CountryCode { get; set; } = "us";

    /// <summary>
    /// Whether to use premium proxies (better success rate, higher cost)
    /// </summary>
    public bool PremiumProxy { get; set; } = false;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Whether scraping API is enabled (falls back to direct scraping if false)
    /// </summary>
    public bool Enabled { get; set; } = true;
}

namespace Algora.Infrastructure.Services;

/// <summary>
/// Configuration options for Shippo integration.
/// </summary>
public class ShippoOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Shippo";

    /// <summary>
    /// Shippo API base URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.goshippo.com";

    /// <summary>
    /// Default API key (fallback if shop doesn't have one configured).
    /// </summary>
    public string? DefaultApiKey { get; set; }

    /// <summary>
    /// Default label format (PDF, PNG, ZPL).
    /// </summary>
    public string DefaultLabelFormat { get; set; } = "PDF";

    /// <summary>
    /// Default parcel weight in pounds (for returns without specified weight).
    /// </summary>
    public decimal DefaultParcelWeightLbs { get; set; } = 1.0m;

    /// <summary>
    /// Default parcel dimensions in inches.
    /// </summary>
    public decimal DefaultParcelLengthIn { get; set; } = 10.0m;
    public decimal DefaultParcelWidthIn { get; set; } = 8.0m;
    public decimal DefaultParcelHeightIn { get; set; } = 4.0m;
}

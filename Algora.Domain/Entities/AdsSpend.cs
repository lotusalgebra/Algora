namespace Algora.Domain.Entities
{
    /// <summary>
    /// Tracks advertising spend by platform, campaign, and date.
    /// </summary>
    public class AdsSpend
    {
        public int Id { get; set; }

        /// <summary>
        /// The shop domain this ad spend belongs to.
        /// </summary>
        public string ShopDomain { get; set; } = string.Empty;

        /// <summary>
        /// Advertising platform (facebook, google, tiktok, instagram, manual).
        /// </summary>
        public string Platform { get; set; } = string.Empty;

        /// <summary>
        /// Campaign name or identifier.
        /// </summary>
        public string? CampaignName { get; set; }

        /// <summary>
        /// Campaign ID from the ad platform.
        /// </summary>
        public string? CampaignId { get; set; }

        /// <summary>
        /// Date of the ad spend.
        /// </summary>
        public DateTime SpendDate { get; set; }

        /// <summary>
        /// Amount spent on this date.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (USD, EUR, etc.).
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Number of impressions.
        /// </summary>
        public int? Impressions { get; set; }

        /// <summary>
        /// Number of clicks.
        /// </summary>
        public int? Clicks { get; set; }

        /// <summary>
        /// Number of conversions (purchases).
        /// </summary>
        public int? Conversions { get; set; }

        /// <summary>
        /// Revenue attributed to this campaign.
        /// </summary>
        public decimal? Revenue { get; set; }

        /// <summary>
        /// Optional notes about the spend.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// When the record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the record was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}

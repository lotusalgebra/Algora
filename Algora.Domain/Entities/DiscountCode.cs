namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a discount or promo code.
    /// </summary>
    public class DiscountCode
    {
        public int Id { get; set; }
        public long? PlatformDiscountId { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DiscountType { get; set; } = "percentage"; // percentage, fixed_amount, free_shipping
        public decimal Value { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsageCount { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
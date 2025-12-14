namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents shop-specific settings and configuration.
    /// </summary>
    public class ShopSettings
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string? ShopName { get; set; }
        public string? Email { get; set; }
        public string? Currency { get; set; }
        public string? Timezone { get; set; }
        public string? WeightUnit { get; set; }
        public string? PlanName { get; set; }
        public string? CountryCode { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Address1 { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
        public bool TaxesIncluded { get; set; }
        public string? InvoicePrefix { get; set; }
        public int InvoiceNextNumber { get; set; } = 1;
        public string? LogoUrl { get; set; }
        public string? InvoiceTerms { get; set; }
        public string? InvoiceFooter { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
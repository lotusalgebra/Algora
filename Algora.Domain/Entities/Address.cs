namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a reusable address entity.
    /// </summary>
    public class Address
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public string AddressType { get; set; } = "shipping"; // billing, shipping
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Company { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? ProvinceCode { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
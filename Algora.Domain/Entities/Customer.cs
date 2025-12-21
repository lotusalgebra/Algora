namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a customer record synced from the e-commerce platform.
    /// </summary>
    public class Customer
    {
        /// <summary>
        /// Primary key for the customer record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Platform customer identifier (Shopify customer id).
        /// </summary>
        public long PlatformCustomerId { get; set; }

        /// <summary>
        /// The shop domain this customer belongs to.
        /// </summary>
        public string ShopDomain { get; set; } = string.Empty;

        /// <summary>
        /// Customer's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Customer's last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Customer's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Customer's phone number.
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Default billing address.
        /// </summary>
        public string? BillingAddress { get; set; }

        /// <summary>
        /// Default shipping address.
        /// </summary>
        public string? ShippingAddress { get; set; }

        /// <summary>
        /// City.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// State or province.
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Postal or ZIP code.
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// Country.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// UTC timestamp when the customer was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when the customer was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Navigation property for orders placed by this customer.
        /// </summary>
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
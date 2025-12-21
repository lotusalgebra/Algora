namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an email template for notifications.
    /// </summary>
    public class EmailTemplate
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty; // order_confirmation, invoice, shipping_notification, etc.
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty; // HTML template with placeholders
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
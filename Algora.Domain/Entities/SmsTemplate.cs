namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an SMS message template.
    /// </summary>
    public class SmsTemplate
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty; // order_confirmation, shipping_update, promotion, etc.
        public string Body { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
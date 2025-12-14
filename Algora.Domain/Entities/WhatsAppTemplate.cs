namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a WhatsApp message template (approved by Meta).
    /// </summary>
    public class WhatsAppTemplate
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ExternalTemplateId { get; set; } // Meta template ID
        public string Language { get; set; } = "en";
        public string Category { get; set; } = "MARKETING"; // MARKETING, UTILITY, AUTHENTICATION
        public string? HeaderType { get; set; } // none, text, image, video, document
        public string? HeaderContent { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? Footer { get; set; }
        public string? Buttons { get; set; } // JSON array of buttons
        public string Status { get; set; } = "pending"; // pending, approved, rejected
        public string? RejectionReason { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
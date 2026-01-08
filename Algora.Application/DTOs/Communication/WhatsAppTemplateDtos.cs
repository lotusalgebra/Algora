namespace Algora.Application.DTOs.Communication;

/// <summary>
/// WhatsApp template data transfer object
/// </summary>
public class WhatsAppTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "UTILITY"; // MARKETING, UTILITY, AUTHENTICATION
    public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
    public string Language { get; set; } = "en";
    public string Body { get; set; } = string.Empty;
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }
    public List<WhatsAppTemplateButtonDto> Buttons { get; set; } = new();
    public List<string> Variables { get; set; } = new(); // {{1}}, {{2}}, etc.
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// WhatsApp template button
/// </summary>
public class WhatsAppTemplateButtonDto
{
    public string Type { get; set; } = "QUICK_REPLY"; // QUICK_REPLY, URL, PHONE_NUMBER
    public string Text { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// DTO for creating a new WhatsApp template
/// </summary>
public class CreateWhatsAppTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "UTILITY";
    public string Language { get; set; } = "en";
    public string Body { get; set; } = string.Empty;
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }
    public List<WhatsAppTemplateButtonDto>? Buttons { get; set; }
}

/// <summary>
/// DTO for updating a WhatsApp template
/// </summary>
public class UpdateWhatsAppTemplateDto
{
    public string? Body { get; set; }
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }
    public List<WhatsAppTemplateButtonDto>? Buttons { get; set; }
}

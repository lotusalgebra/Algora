namespace Algora.Application.DTOs.Communication;

/// <summary>
/// Email template data transfer object
/// </summary>
public class EmailTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? PreviewText { get; set; }
    public string Body { get; set; } = string.Empty;
    public string TemplateType { get; set; } = "custom"; // custom, transactional, marketing
    public string Category { get; set; } = "general"; // general, welcome, order, shipping, review, abandoned_cart
    public bool IsActive { get; set; } = true;
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new email template
/// </summary>
public class CreateEmailTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? PreviewText { get; set; }
    public string Body { get; set; } = string.Empty;
    public string TemplateType { get; set; } = "custom";
    public string Category { get; set; } = "general";
}

/// <summary>
/// DTO for updating an existing email template
/// </summary>
public class UpdateEmailTemplateDto
{
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? PreviewText { get; set; }
    public string? Body { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
}

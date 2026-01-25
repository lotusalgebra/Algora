namespace Algora.Application.DTOs.CustomerPortal;

/// <summary>
/// DTO for reading field configuration
/// </summary>
public class FieldConfigurationDto
{
    public int Id { get; set; }
    public string PageType { get; set; } = "Registration";
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = "text";
    public string Label { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsSystemField { get; set; }
    public int DisplayOrder { get; set; }
    public string? ValidationRegex { get; set; }
    public string? ValidationMessage { get; set; }
    public List<string>? SelectOptions { get; set; }
    public string? DefaultValue { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? CssClass { get; set; }
    public int ColumnWidth { get; set; } = 12;
}

/// <summary>
/// DTO for creating a new field
/// </summary>
public record CreateFieldDto(
    string PageType,
    string FieldName,
    string FieldType,
    string Label,
    string? Placeholder = null,
    string? HelpText = null,
    bool IsRequired = false,
    int DisplayOrder = 0,
    string? ValidationRegex = null,
    string? ValidationMessage = null,
    string? SelectOptions = null,
    string? DefaultValue = null,
    int? MinLength = null,
    int? MaxLength = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    string? CssClass = null,
    int ColumnWidth = 12
);

/// <summary>
/// DTO for updating an existing field
/// </summary>
public record UpdateFieldDto(
    string? Label = null,
    string? Placeholder = null,
    string? HelpText = null,
    bool? IsRequired = null,
    bool? IsEnabled = null,
    int? DisplayOrder = null,
    string? ValidationRegex = null,
    string? ValidationMessage = null,
    string? SelectOptions = null,
    string? DefaultValue = null,
    int? MinLength = null,
    int? MaxLength = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    string? CssClass = null,
    int? ColumnWidth = null
);

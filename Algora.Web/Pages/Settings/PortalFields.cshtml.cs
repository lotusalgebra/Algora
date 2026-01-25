using Algora.Application.DTOs.CustomerPortal;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Settings;

public class PortalFieldsModel : PageModel
{
    private readonly IPortalFieldService _fieldService;
    private readonly AppDbContext _dbContext;

    public PortalFieldsModel(IPortalFieldService fieldService, AppDbContext dbContext)
    {
        _fieldService = fieldService;
        _dbContext = dbContext;
    }

    public string ActiveTab { get; set; } = "Registration";
    public List<FieldConfigurationDto> Fields { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public FieldInputModel Input { get; set; } = new();

    private string ShopDomain => HttpContext.Items["ShopDomain"]?.ToString() ?? "";

    public async Task<IActionResult> OnGetAsync(string? activeTab)
    {
        ActiveTab = activeTab ?? "Registration";
        await LoadFieldsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddFieldAsync()
    {
        ActiveTab = Input.PageType ?? "Registration";

        if (string.IsNullOrEmpty(Input.FieldName) || string.IsNullOrEmpty(Input.Label))
        {
            ErrorMessage = "Field name and label are required.";
            await LoadFieldsAsync();
            return Page();
        }

        try
        {
            var createDto = new CreateFieldDto(
                PageType: Input.PageType ?? "Registration",
                FieldName: Input.FieldName.ToLowerInvariant().Replace(" ", "_"),
                FieldType: Input.FieldType ?? "text",
                Label: Input.Label,
                Placeholder: Input.Placeholder,
                HelpText: Input.HelpText,
                IsRequired: Input.IsRequired,
                DisplayOrder: Input.DisplayOrder,
                ValidationRegex: Input.ValidationRegex,
                ValidationMessage: Input.ValidationMessage,
                SelectOptions: Input.SelectOptions,
                DefaultValue: Input.DefaultValue,
                MinLength: Input.MinLength,
                MaxLength: Input.MaxLength,
                MinValue: Input.MinValue,
                MaxValue: Input.MaxValue,
                CssClass: Input.CssClass,
                ColumnWidth: Input.ColumnWidth
            );

            await _fieldService.CreateFieldAsync(ShopDomain, createDto);
            SuccessMessage = $"Field '{Input.Label}' added successfully.";
            Input = new FieldInputModel { PageType = ActiveTab };
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error adding field: {ex.Message}";
        }

        await LoadFieldsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateFieldAsync(int fieldId)
    {
        ActiveTab = Input.PageType ?? "Registration";

        try
        {
            var updateDto = new UpdateFieldDto(
                Label: Input.Label,
                Placeholder: Input.Placeholder,
                HelpText: Input.HelpText,
                IsRequired: Input.IsRequired,
                IsEnabled: Input.IsEnabled,
                DisplayOrder: Input.DisplayOrder,
                ValidationRegex: Input.ValidationRegex,
                ValidationMessage: Input.ValidationMessage,
                SelectOptions: Input.SelectOptions,
                DefaultValue: Input.DefaultValue,
                MinLength: Input.MinLength,
                MaxLength: Input.MaxLength,
                MinValue: Input.MinValue,
                MaxValue: Input.MaxValue,
                CssClass: Input.CssClass,
                ColumnWidth: Input.ColumnWidth
            );

            await _fieldService.UpdateFieldAsync(fieldId, updateDto);
            SuccessMessage = "Field updated successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating field: {ex.Message}";
        }

        await LoadFieldsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteFieldAsync(int fieldId)
    {
        ActiveTab = Request.Query["activeTab"].FirstOrDefault() ?? "Registration";

        try
        {
            await _fieldService.DeleteFieldAsync(fieldId);
            SuccessMessage = "Field deleted successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting field: {ex.Message}";
        }

        await LoadFieldsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleFieldAsync(int fieldId, bool enable)
    {
        ActiveTab = Request.Query["activeTab"].FirstOrDefault() ?? "Registration";

        try
        {
            var updateDto = new UpdateFieldDto(IsEnabled: enable);
            await _fieldService.UpdateFieldAsync(fieldId, updateDto);
            SuccessMessage = enable ? "Field enabled." : "Field disabled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error toggling field: {ex.Message}";
        }

        await LoadFieldsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostReorderFieldsAsync([FromBody] ReorderRequest request)
    {
        try
        {
            await _fieldService.ReorderFieldsAsync(ShopDomain, request.PageType, request.FieldIds);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    private async Task LoadFieldsAsync()
    {
        Fields = await _fieldService.GetFieldsAsync(ShopDomain, ActiveTab);
    }

    public class FieldInputModel
    {
        public string? PageType { get; set; } = "Registration";
        public string? FieldName { get; set; }
        public string? FieldType { get; set; } = "text";
        public string? Label { get; set; }
        public string? Placeholder { get; set; }
        public string? HelpText { get; set; }
        public bool IsRequired { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int DisplayOrder { get; set; }
        public string? ValidationRegex { get; set; }
        public string? ValidationMessage { get; set; }
        public string? SelectOptions { get; set; }
        public string? DefaultValue { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? CssClass { get; set; }
        public int ColumnWidth { get; set; } = 12;
    }

    public class ReorderRequest
    {
        public string PageType { get; set; } = "";
        public List<int> FieldIds { get; set; } = new();
    }
}

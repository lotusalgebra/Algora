using System.Text.Json;
using Algora.Application.DTOs.CustomerPortal;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.CustomerPortal;

/// <summary>
/// Service for managing Customer Portal field configurations
/// </summary>
public class PortalFieldService : IPortalFieldService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PortalFieldService> _logger;

    public PortalFieldService(AppDbContext dbContext, ILogger<PortalFieldService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<FieldConfigurationDto>> GetFieldsAsync(string shopDomain, string pageType)
    {
        // Ensure default fields exist for this shop
        await SeedDefaultFieldsAsync(shopDomain);

        var fields = await _dbContext.Set<PortalFieldConfiguration>()
            .Where(f => f.ShopDomain == shopDomain && f.PageType == pageType)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();

        return fields.Select(MapToDto).ToList();
    }

    public async Task<FieldConfigurationDto?> GetFieldByIdAsync(int fieldId)
    {
        var field = await _dbContext.Set<PortalFieldConfiguration>()
            .FirstOrDefaultAsync(f => f.Id == fieldId);

        return field == null ? null : MapToDto(field);
    }

    public async Task<FieldConfigurationDto> CreateFieldAsync(string shopDomain, CreateFieldDto dto)
    {
        // Validate field name doesn't already exist for this shop/page
        var exists = await _dbContext.Set<PortalFieldConfiguration>()
            .AnyAsync(f => f.ShopDomain == shopDomain && f.PageType == dto.PageType && f.FieldName == dto.FieldName);

        if (exists)
        {
            throw new InvalidOperationException($"Field '{dto.FieldName}' already exists for {dto.PageType} page.");
        }

        var field = new PortalFieldConfiguration
        {
            ShopDomain = shopDomain,
            PageType = dto.PageType,
            FieldName = dto.FieldName.ToLowerInvariant().Replace(" ", "_"),
            FieldType = dto.FieldType,
            Label = dto.Label,
            Placeholder = dto.Placeholder,
            HelpText = dto.HelpText,
            IsRequired = dto.IsRequired,
            IsEnabled = true,
            IsSystemField = false,
            DisplayOrder = dto.DisplayOrder,
            ValidationRegex = dto.ValidationRegex,
            ValidationMessage = dto.ValidationMessage,
            SelectOptions = dto.SelectOptions,
            DefaultValue = dto.DefaultValue,
            MinLength = dto.MinLength,
            MaxLength = dto.MaxLength,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            CssClass = dto.CssClass,
            ColumnWidth = dto.ColumnWidth,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Set<PortalFieldConfiguration>().Add(field);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created field '{FieldName}' for {PageType} on {ShopDomain}",
            field.FieldName, field.PageType, shopDomain);

        return MapToDto(field);
    }

    public async Task<FieldConfigurationDto> UpdateFieldAsync(int fieldId, UpdateFieldDto dto)
    {
        var field = await _dbContext.Set<PortalFieldConfiguration>()
            .FirstOrDefaultAsync(f => f.Id == fieldId);

        if (field == null)
        {
            throw new InvalidOperationException($"Field with ID {fieldId} not found.");
        }

        // Update only provided properties
        if (dto.Label != null) field.Label = dto.Label;
        if (dto.Placeholder != null) field.Placeholder = dto.Placeholder;
        if (dto.HelpText != null) field.HelpText = dto.HelpText;
        if (dto.IsRequired.HasValue) field.IsRequired = dto.IsRequired.Value;
        if (dto.IsEnabled.HasValue) field.IsEnabled = dto.IsEnabled.Value;
        if (dto.DisplayOrder.HasValue) field.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.ValidationRegex != null) field.ValidationRegex = dto.ValidationRegex;
        if (dto.ValidationMessage != null) field.ValidationMessage = dto.ValidationMessage;
        if (dto.SelectOptions != null) field.SelectOptions = dto.SelectOptions;
        if (dto.DefaultValue != null) field.DefaultValue = dto.DefaultValue;
        if (dto.MinLength.HasValue) field.MinLength = dto.MinLength;
        if (dto.MaxLength.HasValue) field.MaxLength = dto.MaxLength;
        if (dto.MinValue.HasValue) field.MinValue = dto.MinValue;
        if (dto.MaxValue.HasValue) field.MaxValue = dto.MaxValue;
        if (dto.CssClass != null) field.CssClass = dto.CssClass;
        if (dto.ColumnWidth.HasValue) field.ColumnWidth = dto.ColumnWidth.Value;

        field.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated field '{FieldName}' (ID: {FieldId})", field.FieldName, fieldId);

        return MapToDto(field);
    }

    public async Task DeleteFieldAsync(int fieldId)
    {
        var field = await _dbContext.Set<PortalFieldConfiguration>()
            .FirstOrDefaultAsync(f => f.Id == fieldId);

        if (field == null)
        {
            throw new InvalidOperationException($"Field with ID {fieldId} not found.");
        }

        if (field.IsSystemField)
        {
            throw new InvalidOperationException("Cannot delete system fields. You can disable them instead.");
        }

        _dbContext.Set<PortalFieldConfiguration>().Remove(field);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted field '{FieldName}' (ID: {FieldId}) from {ShopDomain}",
            field.FieldName, fieldId, field.ShopDomain);
    }

    public async Task ReorderFieldsAsync(string shopDomain, string pageType, List<int> fieldIds)
    {
        var fields = await _dbContext.Set<PortalFieldConfiguration>()
            .Where(f => f.ShopDomain == shopDomain && f.PageType == pageType)
            .ToListAsync();

        for (int i = 0; i < fieldIds.Count; i++)
        {
            var field = fields.FirstOrDefault(f => f.Id == fieldIds[i]);
            if (field != null)
            {
                field.DisplayOrder = i;
                field.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Reordered {Count} fields for {PageType} on {ShopDomain}",
            fieldIds.Count, pageType, shopDomain);
    }

    public Task<List<FieldConfigurationDto>> GetDefaultFieldsAsync(string pageType)
    {
        var defaults = GetDefaultFieldDefinitions(pageType);
        return Task.FromResult(defaults.Select(MapToDto).ToList());
    }

    public async Task SeedDefaultFieldsAsync(string shopDomain)
    {
        // Check if fields already exist for this shop
        var existingCount = await _dbContext.Set<PortalFieldConfiguration>()
            .CountAsync(f => f.ShopDomain == shopDomain);

        if (existingCount > 0)
        {
            return; // Already seeded
        }

        var pageTypes = new[] { "Registration", "Profile", "Checkout" };
        var fieldsToAdd = new List<PortalFieldConfiguration>();

        foreach (var pageType in pageTypes)
        {
            var defaults = GetDefaultFieldDefinitions(pageType);
            foreach (var def in defaults)
            {
                def.ShopDomain = shopDomain;
                fieldsToAdd.Add(def);
            }
        }

        _dbContext.Set<PortalFieldConfiguration>().AddRange(fieldsToAdd);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} default fields for {ShopDomain}", fieldsToAdd.Count, shopDomain);
    }

    private static List<PortalFieldConfiguration> GetDefaultFieldDefinitions(string pageType)
    {
        return pageType switch
        {
            "Registration" => new List<PortalFieldConfiguration>
            {
                new() { PageType = "Registration", FieldName = "email", FieldType = "email", Label = "Email Address", Placeholder = "you@example.com", IsRequired = true, IsSystemField = true, DisplayOrder = 1, ColumnWidth = 12 },
                new() { PageType = "Registration", FieldName = "password", FieldType = "password", Label = "Password", Placeholder = "Create a password", IsRequired = true, IsSystemField = true, DisplayOrder = 2, MinLength = 8, ColumnWidth = 6 },
                new() { PageType = "Registration", FieldName = "confirm_password", FieldType = "password", Label = "Confirm Password", Placeholder = "Confirm your password", IsRequired = true, IsSystemField = true, DisplayOrder = 3, ColumnWidth = 6 },
                new() { PageType = "Registration", FieldName = "first_name", FieldType = "text", Label = "First Name", Placeholder = "John", IsRequired = true, IsSystemField = true, DisplayOrder = 4, ColumnWidth = 6 },
                new() { PageType = "Registration", FieldName = "last_name", FieldType = "text", Label = "Last Name", Placeholder = "Doe", IsRequired = true, IsSystemField = true, DisplayOrder = 5, ColumnWidth = 6 },
                new() { PageType = "Registration", FieldName = "phone", FieldType = "phone", Label = "Phone Number", Placeholder = "+1 (555) 000-0000", IsRequired = false, IsSystemField = false, DisplayOrder = 6, ColumnWidth = 12 }
            },
            "Profile" => new List<PortalFieldConfiguration>
            {
                new() { PageType = "Profile", FieldName = "first_name", FieldType = "text", Label = "First Name", IsRequired = true, IsSystemField = true, DisplayOrder = 1, ColumnWidth = 6 },
                new() { PageType = "Profile", FieldName = "last_name", FieldType = "text", Label = "Last Name", IsRequired = true, IsSystemField = true, DisplayOrder = 2, ColumnWidth = 6 },
                new() { PageType = "Profile", FieldName = "email", FieldType = "email", Label = "Email Address", IsRequired = true, IsSystemField = true, DisplayOrder = 3, HelpText = "Contact support to change your email", ColumnWidth = 12 },
                new() { PageType = "Profile", FieldName = "phone", FieldType = "phone", Label = "Phone Number", Placeholder = "+1 (555) 000-0000", IsRequired = false, IsSystemField = false, DisplayOrder = 4, ColumnWidth = 6 },
                new() { PageType = "Profile", FieldName = "date_of_birth", FieldType = "date", Label = "Date of Birth", IsRequired = false, IsSystemField = false, DisplayOrder = 5, ColumnWidth = 6 },
                new() { PageType = "Profile", FieldName = "address", FieldType = "textarea", Label = "Address", Placeholder = "Street address, city, state, zip", IsRequired = false, IsSystemField = false, DisplayOrder = 6, ColumnWidth = 12 }
            },
            "Checkout" => new List<PortalFieldConfiguration>
            {
                new() { PageType = "Checkout", FieldName = "email", FieldType = "email", Label = "Email Address", Placeholder = "For order confirmation", IsRequired = true, IsSystemField = true, DisplayOrder = 1, ColumnWidth = 12 },
                new() { PageType = "Checkout", FieldName = "first_name", FieldType = "text", Label = "First Name", IsRequired = true, IsSystemField = true, DisplayOrder = 2, ColumnWidth = 6 },
                new() { PageType = "Checkout", FieldName = "last_name", FieldType = "text", Label = "Last Name", IsRequired = true, IsSystemField = true, DisplayOrder = 3, ColumnWidth = 6 },
                new() { PageType = "Checkout", FieldName = "phone", FieldType = "phone", Label = "Phone", Placeholder = "For delivery updates", IsRequired = true, IsSystemField = true, DisplayOrder = 4, ColumnWidth = 12 },
                new() { PageType = "Checkout", FieldName = "address_line1", FieldType = "text", Label = "Address Line 1", Placeholder = "Street address", IsRequired = true, IsSystemField = true, DisplayOrder = 5, ColumnWidth = 12 },
                new() { PageType = "Checkout", FieldName = "address_line2", FieldType = "text", Label = "Address Line 2", Placeholder = "Apt, suite, unit, etc. (optional)", IsRequired = false, IsSystemField = false, DisplayOrder = 6, ColumnWidth = 12 },
                new() { PageType = "Checkout", FieldName = "city", FieldType = "text", Label = "City", IsRequired = true, IsSystemField = true, DisplayOrder = 7, ColumnWidth = 6 },
                new() { PageType = "Checkout", FieldName = "state", FieldType = "text", Label = "State/Province", IsRequired = true, IsSystemField = true, DisplayOrder = 8, ColumnWidth = 6 },
                new() { PageType = "Checkout", FieldName = "postal_code", FieldType = "text", Label = "Postal Code", IsRequired = true, IsSystemField = true, DisplayOrder = 9, ColumnWidth = 6 },
                new() { PageType = "Checkout", FieldName = "country", FieldType = "select", Label = "Country", IsRequired = true, IsSystemField = true, DisplayOrder = 10, SelectOptions = "[\"United States\",\"Canada\",\"United Kingdom\",\"Australia\",\"Germany\",\"France\",\"Other\"]", ColumnWidth = 6 },
                new() { PageType = "Checkout", FieldName = "order_notes", FieldType = "textarea", Label = "Order Notes", Placeholder = "Special instructions for delivery", IsRequired = false, IsSystemField = false, DisplayOrder = 11, ColumnWidth = 12 }
            },
            _ => new List<PortalFieldConfiguration>()
        };
    }

    private static FieldConfigurationDto MapToDto(PortalFieldConfiguration field)
    {
        List<string>? selectOptions = null;
        if (!string.IsNullOrEmpty(field.SelectOptions))
        {
            try
            {
                selectOptions = JsonSerializer.Deserialize<List<string>>(field.SelectOptions);
            }
            catch
            {
                // If JSON parsing fails, treat as newline-separated
                selectOptions = field.SelectOptions.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }

        return new FieldConfigurationDto
        {
            Id = field.Id,
            PageType = field.PageType,
            FieldName = field.FieldName,
            FieldType = field.FieldType,
            Label = field.Label,
            Placeholder = field.Placeholder,
            HelpText = field.HelpText,
            IsRequired = field.IsRequired,
            IsEnabled = field.IsEnabled,
            IsSystemField = field.IsSystemField,
            DisplayOrder = field.DisplayOrder,
            ValidationRegex = field.ValidationRegex,
            ValidationMessage = field.ValidationMessage,
            SelectOptions = selectOptions,
            DefaultValue = field.DefaultValue,
            MinLength = field.MinLength,
            MaxLength = field.MaxLength,
            MinValue = field.MinValue,
            MaxValue = field.MaxValue,
            CssClass = field.CssClass,
            ColumnWidth = field.ColumnWidth
        };
    }
}

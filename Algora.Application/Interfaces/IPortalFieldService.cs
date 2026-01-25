using Algora.Application.DTOs.CustomerPortal;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing Customer Portal field configurations
/// </summary>
public interface IPortalFieldService
{
    /// <summary>
    /// Gets all fields for a specific page type
    /// </summary>
    Task<List<FieldConfigurationDto>> GetFieldsAsync(string shopDomain, string pageType);

    /// <summary>
    /// Gets a field by ID
    /// </summary>
    Task<FieldConfigurationDto?> GetFieldByIdAsync(int fieldId);

    /// <summary>
    /// Creates a new field
    /// </summary>
    Task<FieldConfigurationDto> CreateFieldAsync(string shopDomain, CreateFieldDto dto);

    /// <summary>
    /// Updates an existing field
    /// </summary>
    Task<FieldConfigurationDto> UpdateFieldAsync(int fieldId, UpdateFieldDto dto);

    /// <summary>
    /// Deletes a field (cannot delete system fields)
    /// </summary>
    Task DeleteFieldAsync(int fieldId);

    /// <summary>
    /// Reorders fields for a page type
    /// </summary>
    Task ReorderFieldsAsync(string shopDomain, string pageType, List<int> fieldIds);

    /// <summary>
    /// Gets the default system fields for a page type
    /// </summary>
    Task<List<FieldConfigurationDto>> GetDefaultFieldsAsync(string pageType);

    /// <summary>
    /// Seeds default fields for a shop if they don't exist
    /// </summary>
    Task SeedDefaultFieldsAsync(string shopDomain);
}

using Algora.Application.DTOs.Operations;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing label templates and generating product labels.
/// </summary>
public interface ILabelDesignerService
{
    // Template CRUD operations
    Task<LabelTemplateDto> CreateTemplateAsync(string shopDomain, CreateLabelTemplateRequest request, CancellationToken ct = default);
    Task<LabelTemplateDto?> GetTemplateByIdAsync(string shopDomain, int templateId, CancellationToken ct = default);
    Task<IEnumerable<LabelTemplateDto>> GetTemplatesAsync(string shopDomain, CancellationToken ct = default);
    Task<LabelTemplateDto?> GetDefaultTemplateAsync(string shopDomain, CancellationToken ct = default);
    Task<LabelTemplateDto> UpdateTemplateAsync(string shopDomain, UpdateLabelTemplateRequest request, CancellationToken ct = default);
    Task<bool> DeleteTemplateAsync(string shopDomain, int templateId, CancellationToken ct = default);
    Task<bool> SetDefaultTemplateAsync(string shopDomain, int templateId, CancellationToken ct = default);

    // Label preview data
    Task<LabelPreviewData?> GetPreviewDataAsync(string shopDomain, int productId, int? variantId = null, CancellationToken ct = default);
    Task<List<LabelPreviewData>> GetPreviewDataForProductsAsync(string shopDomain, List<LabelProductSelection> products, CancellationToken ct = default);

    // Label generation
    Task<LabelGenerationResult> GenerateLabelsPdfAsync(string shopDomain, GenerateLabelsRequest request, CancellationToken ct = default);
    Task<byte[]?> GenerateSingleLabelPreviewAsync(string shopDomain, int templateId, int productId, int? variantId = null, CancellationToken ct = default);

    // Helper methods
    IEnumerable<AvailableLabelField> GetAvailableFields();
}

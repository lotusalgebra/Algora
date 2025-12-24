using Algora.Application.DTOs.Operations;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing suppliers and supplier-product relationships.
/// </summary>
public interface ISupplierService
{
    // Supplier CRUD
    Task<IEnumerable<SupplierDto>> GetSuppliersAsync(string shopDomain, bool activeOnly = true);
    Task<SupplierDto?> GetSupplierAsync(int id);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto);
    Task<SupplierDto> UpdateSupplierAsync(int id, UpdateSupplierDto dto);
    Task DeleteSupplierAsync(int id);

    // Supplier-Product relationships
    Task<IEnumerable<SupplierProductDto>> GetSupplierProductsAsync(int supplierId);
    Task<SupplierProductDto> AddProductToSupplierAsync(int supplierId, AddSupplierProductDto dto);
    Task<SupplierProductDto> UpdateSupplierProductAsync(int supplierProductId, UpdateSupplierProductDto dto);
    Task RemoveProductFromSupplierAsync(int supplierProductId);
    Task<IEnumerable<SupplierDto>> GetSuppliersForProductAsync(int productId, int? productVariantId = null);
    Task<SupplierProductDto?> GetPreferredSupplierForProductAsync(int productId, int? productVariantId = null);

    // Analytics
    Task<SupplierAnalyticsDto> GetSupplierAnalyticsAsync(int supplierId);
    Task UpdateSupplierMetricsAsync(int supplierId);
}

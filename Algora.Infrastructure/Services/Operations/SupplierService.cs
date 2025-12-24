using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Operations;

/// <summary>
/// Service for managing suppliers and supplier-product relationships.
/// </summary>
public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(AppDbContext db, ILogger<SupplierService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<SupplierDto>> GetSuppliersAsync(string shopDomain, bool activeOnly = true)
    {
        var query = _db.Suppliers.Where(s => s.ShopDomain == shopDomain);

        if (activeOnly)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.Name)
            .Select(s => MapToDto(s))
            .ToListAsync();
    }

    public async Task<SupplierDto?> GetSupplierAsync(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        return supplier == null ? null : MapToDto(supplier);
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto)
    {
        var supplier = new Supplier
        {
            ShopDomain = dto.ShopDomain,
            Name = dto.Name,
            Code = dto.Code,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            ContactPerson = dto.ContactPerson,
            Website = dto.Website,
            DefaultLeadTimeDays = dto.DefaultLeadTimeDays,
            MinimumOrderAmount = dto.MinimumOrderAmount,
            PaymentTerms = dto.PaymentTerms,
            Notes = dto.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created supplier {SupplierId} '{Name}' for shop {ShopDomain}",
            supplier.Id, supplier.Name, supplier.ShopDomain);

        return MapToDto(supplier);
    }

    public async Task<SupplierDto> UpdateSupplierAsync(int id, UpdateSupplierDto dto)
    {
        var supplier = await _db.Suppliers.FindAsync(id)
            ?? throw new InvalidOperationException($"Supplier {id} not found");

        supplier.Name = dto.Name;
        supplier.Code = dto.Code;
        supplier.Email = dto.Email;
        supplier.Phone = dto.Phone;
        supplier.Address = dto.Address;
        supplier.ContactPerson = dto.ContactPerson;
        supplier.Website = dto.Website;
        supplier.DefaultLeadTimeDays = dto.DefaultLeadTimeDays;
        supplier.MinimumOrderAmount = dto.MinimumOrderAmount;
        supplier.PaymentTerms = dto.PaymentTerms;
        supplier.Notes = dto.Notes;
        supplier.IsActive = dto.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated supplier {SupplierId} '{Name}'", supplier.Id, supplier.Name);

        return MapToDto(supplier);
    }

    public async Task DeleteSupplierAsync(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id)
            ?? throw new InvalidOperationException($"Supplier {id} not found");

        // Check if there are any purchase orders for this supplier
        var hasOrders = await _db.PurchaseOrders.AnyAsync(po => po.SupplierId == id);
        if (hasOrders)
        {
            // Soft delete by deactivating
            supplier.IsActive = false;
            supplier.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Deactivated supplier {SupplierId} (has existing orders)", id);
        }
        else
        {
            _db.Suppliers.Remove(supplier);
            _logger.LogInformation("Deleted supplier {SupplierId}", id);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<SupplierProductDto>> GetSupplierProductsAsync(int supplierId)
    {
        return await _db.SupplierProducts
            .Include(sp => sp.Supplier)
            .Include(sp => sp.Product)
            .Include(sp => sp.ProductVariant)
            .Where(sp => sp.SupplierId == supplierId)
            .OrderBy(sp => sp.Product.Title)
            .Select(sp => MapToDto(sp))
            .ToListAsync();
    }

    public async Task<SupplierProductDto> AddProductToSupplierAsync(int supplierId, AddSupplierProductDto dto)
    {
        var supplier = await _db.Suppliers.FindAsync(supplierId)
            ?? throw new InvalidOperationException($"Supplier {supplierId} not found");

        var product = await _db.Products.FindAsync(dto.ProductId)
            ?? throw new InvalidOperationException($"Product {dto.ProductId} not found");

        // Check if already exists
        var existing = await _db.SupplierProducts
            .FirstOrDefaultAsync(sp => sp.SupplierId == supplierId
                && sp.ProductId == dto.ProductId
                && sp.ProductVariantId == dto.ProductVariantId);

        if (existing != null)
            throw new InvalidOperationException("This product is already linked to this supplier");

        // If this is being set as preferred, unset any existing preferred
        if (dto.IsPreferred)
        {
            var currentPreferred = await _db.SupplierProducts
                .Where(sp => sp.ProductId == dto.ProductId
                    && sp.ProductVariantId == dto.ProductVariantId
                    && sp.IsPreferred)
                .ToListAsync();

            foreach (var sp in currentPreferred)
                sp.IsPreferred = false;
        }

        var supplierProduct = new SupplierProduct
        {
            SupplierId = supplierId,
            ProductId = dto.ProductId,
            ProductVariantId = dto.ProductVariantId,
            SupplierSku = dto.SupplierSku,
            SupplierProductName = dto.SupplierProductName,
            UnitCost = dto.UnitCost,
            MinimumOrderQuantity = dto.MinimumOrderQuantity,
            LeadTimeDays = dto.LeadTimeDays,
            IsPreferred = dto.IsPreferred,
            CreatedAt = DateTime.UtcNow
        };

        _db.SupplierProducts.Add(supplierProduct);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(supplierProduct).Reference(sp => sp.Supplier).LoadAsync();
        await _db.Entry(supplierProduct).Reference(sp => sp.Product).LoadAsync();
        if (dto.ProductVariantId.HasValue)
            await _db.Entry(supplierProduct).Reference(sp => sp.ProductVariant).LoadAsync();

        _logger.LogInformation("Added product {ProductId} to supplier {SupplierId}", dto.ProductId, supplierId);

        return MapToDto(supplierProduct);
    }

    public async Task<SupplierProductDto> UpdateSupplierProductAsync(int supplierProductId, UpdateSupplierProductDto dto)
    {
        var supplierProduct = await _db.SupplierProducts
            .Include(sp => sp.Supplier)
            .Include(sp => sp.Product)
            .Include(sp => sp.ProductVariant)
            .FirstOrDefaultAsync(sp => sp.Id == supplierProductId)
            ?? throw new InvalidOperationException($"SupplierProduct {supplierProductId} not found");

        if (dto.SupplierSku != null)
            supplierProduct.SupplierSku = dto.SupplierSku;
        if (dto.SupplierProductName != null)
            supplierProduct.SupplierProductName = dto.SupplierProductName;
        if (dto.UnitCost.HasValue)
            supplierProduct.UnitCost = dto.UnitCost.Value;
        if (dto.MinimumOrderQuantity.HasValue)
            supplierProduct.MinimumOrderQuantity = dto.MinimumOrderQuantity.Value;
        if (dto.LeadTimeDays.HasValue)
            supplierProduct.LeadTimeDays = dto.LeadTimeDays.Value;

        if (dto.IsPreferred.HasValue)
        {
            if (dto.IsPreferred.Value)
            {
                // Unset any existing preferred for this product
                var currentPreferred = await _db.SupplierProducts
                    .Where(sp => sp.ProductId == supplierProduct.ProductId
                        && sp.ProductVariantId == supplierProduct.ProductVariantId
                        && sp.IsPreferred
                        && sp.Id != supplierProductId)
                    .ToListAsync();

                foreach (var sp in currentPreferred)
                    sp.IsPreferred = false;
            }
            supplierProduct.IsPreferred = dto.IsPreferred.Value;
        }

        await _db.SaveChangesAsync();

        return MapToDto(supplierProduct);
    }

    public async Task RemoveProductFromSupplierAsync(int supplierProductId)
    {
        var supplierProduct = await _db.SupplierProducts.FindAsync(supplierProductId)
            ?? throw new InvalidOperationException($"SupplierProduct {supplierProductId} not found");

        _db.SupplierProducts.Remove(supplierProduct);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Removed supplier product link {SupplierProductId}", supplierProductId);
    }

    public async Task<IEnumerable<SupplierDto>> GetSuppliersForProductAsync(int productId, int? productVariantId = null)
    {
        var query = _db.SupplierProducts
            .Include(sp => sp.Supplier)
            .Where(sp => sp.ProductId == productId && sp.Supplier.IsActive);

        if (productVariantId.HasValue)
            query = query.Where(sp => sp.ProductVariantId == productVariantId || sp.ProductVariantId == null);

        var supplierProducts = await query
            .OrderByDescending(sp => sp.IsPreferred)
            .ThenBy(sp => sp.Supplier.Name)
            .ToListAsync();

        return supplierProducts.Select(sp => MapToDto(sp.Supplier)).DistinctBy(s => s.Id);
    }

    public async Task<SupplierProductDto?> GetPreferredSupplierForProductAsync(int productId, int? productVariantId = null)
    {
        var query = _db.SupplierProducts
            .Include(sp => sp.Supplier)
            .Include(sp => sp.Product)
            .Include(sp => sp.ProductVariant)
            .Where(sp => sp.ProductId == productId && sp.Supplier.IsActive);

        if (productVariantId.HasValue)
            query = query.Where(sp => sp.ProductVariantId == productVariantId || sp.ProductVariantId == null);

        // Prefer: exact variant match > product-level > lowest cost
        var preferred = await query
            .OrderByDescending(sp => sp.IsPreferred)
            .ThenByDescending(sp => sp.ProductVariantId == productVariantId)
            .ThenBy(sp => sp.UnitCost)
            .FirstOrDefaultAsync();

        return preferred == null ? null : MapToDto(preferred);
    }

    public async Task<SupplierAnalyticsDto> GetSupplierAnalyticsAsync(int supplierId)
    {
        var supplier = await _db.Suppliers.FindAsync(supplierId)
            ?? throw new InvalidOperationException($"Supplier {supplierId} not found");

        var purchaseOrders = await _db.PurchaseOrders
            .Where(po => po.SupplierId == supplierId)
            .OrderByDescending(po => po.CreatedAt)
            .ToListAsync();

        var pendingOrders = purchaseOrders.Where(po => po.Status != "received" && po.Status != "cancelled");
        var productsSupplied = await _db.SupplierProducts.CountAsync(sp => sp.SupplierId == supplierId);

        // Get recent orders
        var recentOrders = purchaseOrders
            .Take(10)
            .Select(po => new RecentOrderDto(
                po.Id,
                po.OrderNumber,
                po.Status,
                po.Total,
                po.CreatedAt,
                po.ReceivedAt
            ))
            .ToList();

        // Get top products
        var topProducts = await _db.PurchaseOrderLines
            .Include(pol => pol.PurchaseOrder)
            .Where(pol => pol.PurchaseOrder.SupplierId == supplierId)
            .GroupBy(pol => new { pol.ProductId, pol.ProductTitle })
            .Select(g => new TopProductDto(
                g.Key.ProductId,
                g.Key.ProductTitle,
                g.Sum(pol => pol.QuantityOrdered),
                g.Sum(pol => pol.TotalCost),
                g.Select(pol => pol.PurchaseOrderId).Distinct().Count()
            ))
            .OrderByDescending(tp => tp.TotalSpent)
            .Take(10)
            .ToListAsync();

        return new SupplierAnalyticsDto(
            SupplierId: supplier.Id,
            SupplierName: supplier.Name,
            TotalOrders: supplier.TotalOrders,
            TotalSpent: supplier.TotalSpent,
            PendingOrders: pendingOrders.Count(),
            PendingOrdersValue: pendingOrders.Sum(po => po.Total),
            AverageDeliveryDays: supplier.AverageDeliveryDays,
            OnTimeDeliveryRate: supplier.OnTimeDeliveryRate,
            ProductsSupplied: productsSupplied,
            RecentOrders: recentOrders,
            TopProducts: topProducts
        );
    }

    public async Task UpdateSupplierMetricsAsync(int supplierId)
    {
        var supplier = await _db.Suppliers.FindAsync(supplierId);
        if (supplier == null) return;

        var completedOrders = await _db.PurchaseOrders
            .Where(po => po.SupplierId == supplierId && po.Status == "received")
            .ToListAsync();

        supplier.TotalOrders = completedOrders.Count;
        supplier.TotalSpent = completedOrders.Sum(po => po.Total);

        // Calculate average delivery days
        var ordersWithDelivery = completedOrders
            .Where(po => po.OrderedAt.HasValue && po.ReceivedAt.HasValue)
            .ToList();

        if (ordersWithDelivery.Any())
        {
            supplier.AverageDeliveryDays = (decimal)ordersWithDelivery
                .Average(po => (po.ReceivedAt!.Value - po.OrderedAt!.Value).TotalDays);
        }

        // Calculate on-time delivery rate
        var ordersWithExpected = completedOrders
            .Where(po => po.ExpectedDeliveryDate.HasValue && po.ReceivedAt.HasValue)
            .ToList();

        if (ordersWithExpected.Any())
        {
            var onTime = ordersWithExpected.Count(po => po.ReceivedAt <= po.ExpectedDeliveryDate);
            supplier.OnTimeDeliveryRate = (decimal)onTime / ordersWithExpected.Count * 100;
        }

        supplier.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated metrics for supplier {SupplierId}", supplierId);
    }

    private static SupplierDto MapToDto(Supplier s) => new(
        s.Id,
        s.ShopDomain,
        s.Name,
        s.Code,
        s.Email,
        s.Phone,
        s.Address,
        s.ContactPerson,
        s.Website,
        s.DefaultLeadTimeDays,
        s.MinimumOrderAmount,
        s.PaymentTerms,
        s.Notes,
        s.IsActive,
        s.TotalOrders,
        s.TotalSpent,
        s.AverageDeliveryDays,
        s.OnTimeDeliveryRate,
        s.CreatedAt,
        s.UpdatedAt
    );

    private static SupplierProductDto MapToDto(SupplierProduct sp) => new(
        sp.Id,
        sp.SupplierId,
        sp.Supplier.Name,
        sp.ProductId,
        sp.Product.Title,
        sp.ProductVariantId,
        sp.ProductVariant?.Title,
        sp.SupplierSku,
        sp.SupplierProductName,
        sp.UnitCost,
        sp.MinimumOrderQuantity,
        sp.LeadTimeDays,
        sp.IsPreferred,
        sp.LastOrderedAt,
        sp.CreatedAt
    );
}

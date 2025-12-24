using Algora.Application.DTOs.Operations;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing purchase orders and automated reordering.
/// </summary>
public interface IPurchaseOrderService
{
    // Purchase Order CRUD
    Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersAsync(string shopDomain, PurchaseOrderFilterDto? filter = null);
    Task<PurchaseOrderDto?> GetPurchaseOrderAsync(int id);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto);
    Task<PurchaseOrderDto> UpdatePurchaseOrderAsync(int id, UpdatePurchaseOrderDto dto);
    Task DeletePurchaseOrderAsync(int id);

    // Line items
    Task<PurchaseOrderLineDto> AddLineItemAsync(int purchaseOrderId, AddPurchaseOrderLineDto dto);
    Task<PurchaseOrderLineDto> UpdateLineItemAsync(int lineId, UpdatePurchaseOrderLineDto dto);
    Task RemoveLineItemAsync(int lineId);

    // Status workflow
    Task<PurchaseOrderDto> SendToSupplierAsync(int id, string? message = null);
    Task<PurchaseOrderDto> MarkAsConfirmedAsync(int id, DateTime? expectedDeliveryDate = null, string? supplierReference = null);
    Task<PurchaseOrderDto> MarkAsShippedAsync(int id, string? trackingNumber = null);
    Task<PurchaseOrderDto> ReceiveItemsAsync(int id, ReceiveItemsDto dto);
    Task<PurchaseOrderDto> MarkAsReceivedAsync(int id);
    Task<PurchaseOrderDto> CancelOrderAsync(int id, string reason);
    Task<PurchaseOrderDto> CancelPurchaseOrderAsync(int id, string reason); // Alias for CancelOrderAsync

    // Automated reordering
    Task<IEnumerable<SuggestedPurchaseOrderDto>> GenerateSuggestedOrdersAsync(string shopDomain);
    Task<PurchaseOrderDto> CreateFromSuggestionAsync(SuggestedPurchaseOrderDto suggestion);
    Task<int> ProcessAutoPurchaseOrdersAsync(string shopDomain, CancellationToken ct);

    // Utilities
    Task<string> GenerateOrderNumberAsync(string shopDomain);
    Task RecalculateTotalsAsync(int purchaseOrderId);
}

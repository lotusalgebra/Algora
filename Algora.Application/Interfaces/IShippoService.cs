using Algora.Application.DTOs.Returns;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for Shippo shipping label integration.
/// </summary>
public interface IShippoService
{
    /// <summary>
    /// Create a return shipping label.
    /// </summary>
    /// <param name="shopDomain">The shop domain.</param>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <param name="customerAddress">Customer's address (from).</param>
    /// <param name="carrier">Optional carrier override.</param>
    /// <param name="serviceLevel">Optional service level override.</param>
    /// <returns>The created label details.</returns>
    Task<ReturnLabelDto> CreateReturnLabelAsync(
        string shopDomain,
        int returnRequestId,
        ReturnAddressDto customerAddress,
        string? carrier = null,
        string? serviceLevel = null);

    /// <summary>
    /// Get label details by ID.
    /// </summary>
    Task<ReturnLabelDto?> GetLabelAsync(int labelId);

    /// <summary>
    /// Get tracking updates for a shipment.
    /// </summary>
    Task<ShippoTrackingDto?> GetTrackingStatusAsync(string trackingNumber, string carrier);

    /// <summary>
    /// Validate an address.
    /// </summary>
    Task<AddressValidationResultDto> ValidateAddressAsync(ReturnAddressDto address);

    /// <summary>
    /// Void/refund an unused label.
    /// </summary>
    Task<bool> VoidLabelAsync(int labelId);

    /// <summary>
    /// Get available shipping rates for a return.
    /// </summary>
    Task<List<ShippingRateDto>> GetRatesAsync(
        string shopDomain,
        ReturnAddressDto fromAddress,
        ReturnAddressDto toAddress);

    /// <summary>
    /// Check if Shippo is configured for the shop.
    /// </summary>
    Task<bool> IsConfiguredAsync(string shopDomain);
}

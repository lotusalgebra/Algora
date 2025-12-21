using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Algora.Application.DTOs.Returns;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for Shippo shipping label integration.
/// </summary>
public class ShippoService : IShippoService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ShippoOptions _options;
    private readonly ILogger<ShippoService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public ShippoService(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<ShippoOptions> options,
        ILogger<ShippoService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ReturnLabelDto> CreateReturnLabelAsync(
        string shopDomain,
        int returnRequestId,
        ReturnAddressDto customerAddress,
        string? carrier = null,
        string? serviceLevel = null)
    {
        var settings = await GetSettingsAsync(shopDomain);
        var apiKey = settings?.ShippoApiKey ?? _options.DefaultApiKey;

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Shippo API key is not configured");

        var returnRequest = await _db.ReturnRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId);

        if (returnRequest == null)
            throw new InvalidOperationException($"Return request {returnRequestId} not found");

        // Build return address (destination)
        var toAddress = new
        {
            name = settings?.ReturnAddressName ?? "Returns Department",
            company = settings?.ReturnAddressCompany,
            street1 = settings?.ReturnAddressStreet1,
            street2 = settings?.ReturnAddressStreet2,
            city = settings?.ReturnAddressCity,
            state = settings?.ReturnAddressState,
            zip = settings?.ReturnAddressZip,
            country = settings?.ReturnAddressCountry ?? "US",
            phone = settings?.ReturnAddressPhone,
            email = settings?.ReturnAddressEmail
        };

        // Build customer address (origin)
        var fromAddress = new
        {
            name = customerAddress.Name,
            company = customerAddress.Company,
            street1 = customerAddress.Street1,
            street2 = customerAddress.Street2,
            city = customerAddress.City,
            state = customerAddress.State,
            zip = customerAddress.Zip,
            country = customerAddress.Country ?? "US",
            phone = customerAddress.Phone,
            email = customerAddress.Email
        };

        // Build parcel
        var parcel = new
        {
            length = _options.DefaultParcelLengthIn.ToString("F1"),
            width = _options.DefaultParcelWidthIn.ToString("F1"),
            height = _options.DefaultParcelHeightIn.ToString("F1"),
            distance_unit = "in",
            weight = _options.DefaultParcelWeightLbs.ToString("F1"),
            mass_unit = "lb"
        };

        // Create shipment
        var shipmentRequest = new
        {
            address_from = fromAddress,
            address_to = toAddress,
            parcels = new[] { parcel },
            async = false
        };

        var client = CreateHttpClient(apiKey);
        var shipmentResponse = await PostAsync<ShippoShipmentResponse>(client, "shipments", shipmentRequest);

        if (shipmentResponse == null || string.IsNullOrEmpty(shipmentResponse.ObjectId))
            throw new InvalidOperationException("Failed to create Shippo shipment");

        // Find the rate
        var selectedCarrier = carrier ?? settings?.DefaultCarrier ?? "usps";
        var selectedService = serviceLevel ?? settings?.DefaultServiceLevel ?? "usps_priority";

        var rate = shipmentResponse.Rates?
            .FirstOrDefault(r => r.Provider?.ToLower() == selectedCarrier.ToLower() &&
                                 r.ServiceLevel?.Token?.ToLower() == selectedService.ToLower());

        if (rate == null)
        {
            // Fallback to cheapest rate
            rate = shipmentResponse.Rates?
                .OrderBy(r => decimal.TryParse(r.Amount, out var amt) ? amt : decimal.MaxValue)
                .FirstOrDefault();
        }

        if (rate == null || string.IsNullOrEmpty(rate.ObjectId))
            throw new InvalidOperationException("No shipping rates available");

        // Purchase label
        var transactionRequest = new
        {
            rate = rate.ObjectId,
            label_file_type = _options.DefaultLabelFormat,
            async = false
        };

        var transactionResponse = await PostAsync<ShippoTransactionResponse>(client, "transactions", transactionRequest);

        if (transactionResponse == null || transactionResponse.Status != "SUCCESS")
            throw new InvalidOperationException($"Failed to create shipping label: {transactionResponse?.Messages?.FirstOrDefault()?.Text}");

        // Save label to database
        var label = new ReturnLabel
        {
            ShopDomain = shopDomain,
            ShippoTransactionId = transactionResponse.ObjectId ?? "",
            ShippoRateId = rate.ObjectId ?? "",
            ShippoShipmentId = shipmentResponse.ObjectId,
            TrackingNumber = transactionResponse.TrackingNumber ?? "",
            TrackingUrl = transactionResponse.TrackingUrlProvider,
            Carrier = rate.Provider ?? selectedCarrier,
            ServiceLevel = rate.ServiceLevel?.Token ?? selectedService,
            LabelUrl = transactionResponse.LabelUrl ?? "",
            LabelFormat = _options.DefaultLabelFormat,
            Cost = decimal.TryParse(rate.Amount, out var amount) ? amount : 0,
            Currency = rate.Currency ?? "USD",
            FromAddressJson = JsonSerializer.Serialize(customerAddress),
            ToAddressJson = JsonSerializer.Serialize(new ReturnAddressDto
            {
                Name = settings?.ReturnAddressName,
                Company = settings?.ReturnAddressCompany,
                Street1 = settings?.ReturnAddressStreet1,
                Street2 = settings?.ReturnAddressStreet2,
                City = settings?.ReturnAddressCity,
                State = settings?.ReturnAddressState,
                Zip = settings?.ReturnAddressZip,
                Country = settings?.ReturnAddressCountry,
                Phone = settings?.ReturnAddressPhone,
                Email = settings?.ReturnAddressEmail
            }),
            Status = "created",
            ExpiresAt = DateTime.UtcNow.AddDays(settings?.LabelExpirationDays ?? 14),
            CreatedAt = DateTime.UtcNow
        };

        _db.ReturnLabels.Add(label);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created return label {LabelId} for return request {ReturnRequestId}", label.Id, returnRequestId);

        return MapToDto(label);
    }

    public async Task<ReturnLabelDto?> GetLabelAsync(int labelId)
    {
        var label = await _db.ReturnLabels.FindAsync(labelId);
        return label != null ? MapToDto(label) : null;
    }

    public async Task<ShippoTrackingDto?> GetTrackingStatusAsync(string trackingNumber, string carrier)
    {
        // For now, return null - implement when webhook is set up
        await Task.CompletedTask;
        return null;
    }

    public async Task<AddressValidationResultDto> ValidateAddressAsync(ReturnAddressDto address)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(address.Street1) ||
            string.IsNullOrWhiteSpace(address.City) ||
            string.IsNullOrWhiteSpace(address.State) ||
            string.IsNullOrWhiteSpace(address.Zip))
        {
            return new AddressValidationResultDto
            {
                IsValid = false,
                Message = "Address is incomplete. Please provide street, city, state, and zip code."
            };
        }

        await Task.CompletedTask;
        return new AddressValidationResultDto { IsValid = true };
    }

    public async Task<bool> VoidLabelAsync(int labelId)
    {
        var label = await _db.ReturnLabels.FindAsync(labelId);
        if (label == null) return false;

        var settings = await GetSettingsAsync(label.ShopDomain);
        var apiKey = settings?.ShippoApiKey ?? _options.DefaultApiKey;

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Cannot void label {LabelId} - no API key configured", labelId);
            return false;
        }

        try
        {
            var client = CreateHttpClient(apiKey);
            var refundRequest = new { transaction = label.ShippoTransactionId };
            await PostAsync<object>(client, "refunds", refundRequest);

            label.Status = "refunded";
            await _db.SaveChangesAsync();

            _logger.LogInformation("Voided label {LabelId}", labelId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to void label {LabelId}", labelId);
            return false;
        }
    }

    public async Task<List<ShippingRateDto>> GetRatesAsync(
        string shopDomain,
        ReturnAddressDto fromAddress,
        ReturnAddressDto toAddress)
    {
        var settings = await GetSettingsAsync(shopDomain);
        var apiKey = settings?.ShippoApiKey ?? _options.DefaultApiKey;

        if (string.IsNullOrEmpty(apiKey))
            return new List<ShippingRateDto>();

        var shipmentRequest = new
        {
            address_from = new
            {
                name = fromAddress.Name,
                street1 = fromAddress.Street1,
                street2 = fromAddress.Street2,
                city = fromAddress.City,
                state = fromAddress.State,
                zip = fromAddress.Zip,
                country = fromAddress.Country ?? "US"
            },
            address_to = new
            {
                name = toAddress.Name,
                street1 = toAddress.Street1,
                street2 = toAddress.Street2,
                city = toAddress.City,
                state = toAddress.State,
                zip = toAddress.Zip,
                country = toAddress.Country ?? "US"
            },
            parcels = new[]
            {
                new
                {
                    length = _options.DefaultParcelLengthIn.ToString("F1"),
                    width = _options.DefaultParcelWidthIn.ToString("F1"),
                    height = _options.DefaultParcelHeightIn.ToString("F1"),
                    distance_unit = "in",
                    weight = _options.DefaultParcelWeightLbs.ToString("F1"),
                    mass_unit = "lb"
                }
            },
            async = false
        };

        try
        {
            var client = CreateHttpClient(apiKey);
            var response = await PostAsync<ShippoShipmentResponse>(client, "shipments", shipmentRequest);

            return response?.Rates?.Select(r => new ShippingRateDto
            {
                RateId = r.ObjectId ?? "",
                Carrier = r.Provider ?? "",
                ServiceLevel = r.ServiceLevel?.Token ?? "",
                ServiceName = r.ServiceLevel?.Name ?? "",
                Amount = decimal.TryParse(r.Amount, out var amt) ? amt : 0,
                Currency = r.Currency ?? "USD",
                EstimatedDays = r.EstimatedDays
            }).ToList() ?? new List<ShippingRateDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shipping rates for shop {Shop}", shopDomain);
            return new List<ShippingRateDto>();
        }
    }

    public async Task<bool> IsConfiguredAsync(string shopDomain)
    {
        var settings = await GetSettingsAsync(shopDomain);
        return !string.IsNullOrEmpty(settings?.ShippoApiKey) || !string.IsNullOrEmpty(_options.DefaultApiKey);
    }

    private async Task<ReturnSettings?> GetSettingsAsync(string shopDomain)
    {
        return await _db.ReturnSettings.FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);
    }

    private HttpClient CreateHttpClient(string apiKey)
    {
        var client = _httpClientFactory.CreateClient("Shippo");
        client.BaseAddress = new Uri(_options.BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ShippoToken", apiKey);
        return client;
    }

    private async Task<T?> PostAsync<T>(HttpClient client, string endpoint, object data) where T : class
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(endpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Shippo API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
            throw new InvalidOperationException($"Shippo API error: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<T>(responseContent, JsonOptions);
    }

    private static ReturnLabelDto MapToDto(ReturnLabel label) => new()
    {
        Id = label.Id,
        ShippoTransactionId = label.ShippoTransactionId,
        TrackingNumber = label.TrackingNumber,
        TrackingUrl = label.TrackingUrl,
        Carrier = label.Carrier,
        ServiceLevel = label.ServiceLevel,
        LabelUrl = label.LabelUrl,
        LabelFormat = label.LabelFormat,
        Cost = label.Cost,
        Currency = label.Currency,
        Status = label.Status,
        ExpiresAt = label.ExpiresAt,
        CreatedAt = label.CreatedAt
    };

    // Shippo API response models
    private class ShippoShipmentResponse
    {
        public string? ObjectId { get; set; }
        public List<ShippoRate>? Rates { get; set; }
    }

    private class ShippoRate
    {
        public string? ObjectId { get; set; }
        public string? Provider { get; set; }
        public string? Amount { get; set; }
        public string? Currency { get; set; }
        public int? EstimatedDays { get; set; }
        public ShippoServiceLevel? ServiceLevel { get; set; }
    }

    private class ShippoServiceLevel
    {
        public string? Token { get; set; }
        public string? Name { get; set; }
    }

    private class ShippoTransactionResponse
    {
        public string? ObjectId { get; set; }
        public string? Status { get; set; }
        public string? TrackingNumber { get; set; }
        public string? TrackingUrlProvider { get; set; }
        public string? LabelUrl { get; set; }
        public List<ShippoMessage>? Messages { get; set; }
    }

    private class ShippoMessage
    {
        public string? Text { get; set; }
    }
}

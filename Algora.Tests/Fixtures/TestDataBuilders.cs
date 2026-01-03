using Algora.Application.DTOs.Inventory;
using Algora.Application.DTOs.Operations;
using Algora.Application.DTOs.Upsell;

namespace Algora.Tests.Fixtures;

/// <summary>
/// Builders for creating test DTOs for pagination tests.
/// </summary>
public static class TestDataBuilders
{
    public const string TestShopDomain = "pagination-test.myshopify.com";

    public static List<SupplierDto> CreateSuppliers(int count = 20)
    {
        return Enumerable.Range(1, count).Select(i => new SupplierDto(
            Id: i,
            ShopDomain: TestShopDomain,
            Name: $"Supplier {i:D2}",
            Code: $"SUP-{i:D4}",
            Email: $"supplier{i}@test.com",
            Phone: $"+1555001{i:D4}",
            Address: $"{i * 100} Supplier Street, New York, NY",
            ContactPerson: $"Contact Person {i}",
            Website: $"https://supplier{i}.com",
            DefaultLeadTimeDays: 5 + (i % 10),
            MinimumOrderAmount: 100m * i,
            PaymentTerms: "Net 30",
            Notes: $"Notes for supplier {i}",
            IsActive: i % 7 != 0,
            TotalOrders: i * 5,
            TotalSpent: i * 1000m,
            AverageDeliveryDays: 5m + (i * 0.5m),
            OnTimeDeliveryRate: 0.85m + (i * 0.005m),
            CreatedAt: DateTime.UtcNow.AddDays(-i * 5),
            UpdatedAt: DateTime.UtcNow.AddDays(-i)
        )).ToList();
    }

    public static List<LocationDto> CreateLocations(int count = 10)
    {
        return Enumerable.Range(1, count).Select(i => new LocationDto(
            Id: i,
            ShopDomain: TestShopDomain,
            ShopifyLocationId: 4000 + i,
            Name: $"Warehouse {i}",
            Address1: $"{i * 50} Warehouse Blvd",
            Address2: i % 3 == 0 ? "Suite 100" : null,
            City: i % 3 == 0 ? "Chicago" : (i % 3 == 1 ? "Houston" : "Phoenix"),
            Province: i % 3 == 0 ? "Illinois" : (i % 3 == 1 ? "Texas" : "Arizona"),
            ProvinceCode: i % 3 == 0 ? "IL" : (i % 3 == 1 ? "TX" : "AZ"),
            Country: "United States",
            CountryCode: "US",
            Zip: $"{60000 + i}",
            Phone: $"+1555002{i:D4}",
            IsActive: i % 5 != 0 || i == 1,
            IsPrimary: i == 1,
            FulfillsOnlineOrders: true,
            LastSyncedAt: DateTime.UtcNow.AddHours(-i),
            CreatedAt: DateTime.UtcNow.AddDays(-i * 10),
            TotalProducts: 50 * i,
            TotalInventory: 1000 * i
        )).ToList();
    }

    public static List<PurchaseOrderDto> CreatePurchaseOrders(int count = 40)
    {
        var statuses = new[] { "draft", "pending", "confirmed", "shipped", "received", "cancelled" };

        return Enumerable.Range(1, count).Select(i => new PurchaseOrderDto(
            Id: i,
            ShopDomain: TestShopDomain,
            SupplierId: (i % 10) + 1,
            SupplierName: $"Supplier {((i % 10) + 1):D2}",
            OrderNumber: $"PO-{i:D4}",
            Status: statuses[i % 6],
            LocationId: (i % 5) + 1,
            LocationName: $"Warehouse {((i % 5) + 1)}",
            Subtotal: 500m + (i * 50),
            Tax: 50m + (i * 5),
            Shipping: 25m,
            Total: 575m + (i * 55),
            Currency: "USD",
            Notes: $"Notes for PO {i}",
            SupplierReference: $"REF-{i}",
            TrackingNumber: i % 3 == 0 ? $"TRACK{i:D8}" : null,
            ExpectedDeliveryDate: DateTime.UtcNow.AddDays(7 + i),
            OrderedAt: i % 6 >= 1 ? DateTime.UtcNow.AddDays(-i * 2) : null,
            ConfirmedAt: i % 6 >= 2 ? DateTime.UtcNow.AddDays(-i * 2 + 1) : null,
            ShippedAt: i % 6 >= 3 ? DateTime.UtcNow.AddDays(-i * 2 + 2) : null,
            ReceivedAt: i % 6 == 4 ? DateTime.UtcNow.AddDays(-i * 2 + 3) : null,
            CancelledAt: i % 6 == 5 ? DateTime.UtcNow.AddDays(-i * 2 + 1) : null,
            CancellationReason: i % 6 == 5 ? "Test cancellation" : null,
            CreatedAt: DateTime.UtcNow.AddDays(-i * 2),
            UpdatedAt: DateTime.UtcNow.AddDays(-i),
            Lines: Enumerable.Range(1, (i % 5) + 1).Select(j => new PurchaseOrderLineDto(
                Id: i * 10 + j,
                PurchaseOrderId: i,
                ProductId: j,
                ProductVariantId: j * 10,
                Sku: $"SKU-{i}-{j}",
                ProductTitle: $"Product {j}",
                VariantTitle: $"Variant {j}",
                QuantityOrdered: 10 + j * 5,
                QuantityReceived: i % 6 == 4 ? 10 + j * 5 : 0,
                UnitCost: 10m + j * 2,
                TotalCost: (10m + j * 2) * (10 + j * 5),
                ReceivedAt: i % 6 == 4 ? DateTime.UtcNow.AddDays(-i) : null,
                ReceivingNotes: null
            )).ToList()
        )).ToList();
    }

    public static List<UpsellOfferDto> CreateUpsellOffers(int count = 25)
    {
        return Enumerable.Range(1, count).Select(i => new UpsellOfferDto
        {
            Id = i,
            Name = $"Upsell Offer {i:D2}",
            TriggerProductIds = new List<long> { 1000 + i, 1000 + i + 1 },
            RecommendedProductId = 2000 + i,
            RecommendedProductTitle = $"Recommended Product {i}",
            RecommendedProductImageUrl = $"https://example.com/product{i}.jpg",
            RecommendedProductPrice = 29.99m + i,
            DiscountType = i % 3 == 0 ? "percentage" : (i % 3 == 1 ? "fixed" : "none"),
            DiscountValue = i % 3 == 0 ? 10m : (i % 3 == 1 ? 5m : 0m),
            DiscountedPrice = i % 3 != 2 ? 24.99m + i : null,
            IsActive = i % 4 != 0,
            Priority = i,
            Impressions = i * 100,
            Clicks = i * 20,
            Conversions = i * 10,
            Revenue = i * 50m
        }).ToList();
    }

    public static List<UpsellExperimentDto> CreateUpsellExperiments(int count = 15)
    {
        var statuses = new[] { "draft", "running", "paused", "completed" };

        return Enumerable.Range(1, count).Select(i => new UpsellExperimentDto
        {
            Id = i,
            Name = $"Experiment {i:D2}",
            Description = $"Testing upsell strategy {i}",
            Status = statuses[i % 4],
            StartedAt = i % 4 >= 1 ? DateTime.UtcNow.AddDays(-i * 7) : null,
            EndedAt = i % 4 == 3 ? DateTime.UtcNow.AddDays(-1) : null,
            VariantCount = (i % 3) + 2,
            TotalImpressions = i * 100,
            TotalConversions = i * 10,
            TotalRevenue = i * 50m,
            IsStatisticallySignificant = i % 4 == 3,
            CreatedAt = DateTime.UtcNow.AddDays(-i * 7),
            VariantResults = Enumerable.Range(0, (i % 3) + 2).Select(v => new ExperimentVariantResultDto
            {
                VariantName = v == 0 ? "Control" : $"Variant {v}",
                OfferId = i * 10 + v,
                OfferTitle = $"Offer for Variant {v}",
                Impressions = i * 50 + v * 20,
                Clicks = i * 10 + v * 5,
                Conversions = i * 5 + v * 2,
                Revenue = i * 25m + v * 10m,
                ClickRate = 0.2m,
                ConversionRate = 0.1m + (v * 0.02m),
                RevenuePerView = 0.5m + v * 0.1m,
                IsWinner = i % 4 == 3 && v == 1
            }).ToList()
        }).ToList();
    }

    public static List<ProductAffinityDto> CreateProductAffinities(int count = 60)
    {
        return Enumerable.Range(1, count).Select(i => new ProductAffinityDto
        {
            Id = i,
            SourceProductId = 1000 + i,
            SourceProductTitle = $"Source Product {i:D3}",
            RelatedProductId = 2000 + i,
            RelatedProductTitle = $"Related Product {i:D3}",
            Support = 0.01m + (i * 0.001m),
            Confidence = 0.1m + (i * 0.01m),
            Lift = 0.5m + (i * 0.05m),
            CoOccurrences = i * 5,
            CalculatedAt = DateTime.UtcNow.AddHours(-i)
        }).ToList();
    }

    public static PaginatedResult<InventoryPredictionDto> CreateInventoryPredictions(int count = 35)
    {
        var statuses = new[] { "healthy", "low_stock", "critical", "out_of_stock" };
        var confidenceLevels = new[] { "low", "medium", "high" };

        var items = Enumerable.Range(1, count).Select(i => new InventoryPredictionDto
        {
            Id = i,
            ShopDomain = TestShopDomain,
            PlatformProductId = 1000 + i,
            ProductTitle = $"Product {i:D3}",
            PlatformVariantId = 2000 + i,
            VariantTitle = "Default",
            Sku = $"SKU-{i:D4}",
            CurrentQuantity = 100 + i * 10,
            AverageDailySales = 5m + (i * 0.5m),
            DaysUntilStockout = 20 - (i % 15),
            SuggestedReorderQuantity = 50 + i * 5,
            Status = statuses[i % 4],
            ConfidenceLevel = confidenceLevels[i % 3],
            CalculatedAt = DateTime.UtcNow.AddDays(-i)
        }).ToList();

        return new PaginatedResult<InventoryPredictionDto>
        {
            Items = items,
            TotalCount = items.Count,
            Page = 1,
            PageSize = count
        };
    }

    public static PaginatedResult<UpsellOfferDto> CreatePaginatedOffers(int count = 25)
    {
        var items = CreateUpsellOffers(count);
        return new PaginatedResult<UpsellOfferDto>
        {
            Items = items,
            TotalCount = items.Count,
            Page = 1,
            PageSize = count
        };
    }

    public static PaginatedResult<UpsellExperimentDto> CreatePaginatedExperiments(int count = 15)
    {
        var items = CreateUpsellExperiments(count);
        return new PaginatedResult<UpsellExperimentDto>
        {
            Items = items,
            TotalCount = items.Count,
            Page = 1,
            PageSize = count
        };
    }

    public static PaginatedResult<ProductAffinityDto> CreatePaginatedAffinities(int count = 60)
    {
        var items = CreateProductAffinities(count);
        return new PaginatedResult<ProductAffinityDto>
        {
            Items = items,
            TotalCount = items.Count,
            Page = 1,
            PageSize = count
        };
    }
}

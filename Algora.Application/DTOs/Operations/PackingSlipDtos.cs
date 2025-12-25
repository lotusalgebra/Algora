namespace Algora.Application.DTOs.Operations;

// ==================== Packing Slip DTOs ====================

public class PackingSlipDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public AddressInfo ShippingAddress { get; set; } = new();
    public AddressInfo BillingAddress { get; set; } = new();
    public List<PackingSlipItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public string? OrderBarcode { get; set; }
    public string? Notes { get; set; }
    public string? ShippingMethod { get; set; }
    public string? TrackingNumber { get; set; }
}

public class PackingSlipItemDto
{
    public string ProductTitle { get; set; } = string.Empty;
    public string? VariantTitle { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? ImageUrl { get; set; }
    public string? Barcode { get; set; }
}

public class AddressInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

// ==================== Packing Slip Settings ====================

public class PackingSlipSettings
{
    public bool ShowPrices { get; set; } = true;
    public bool ShowBarcode { get; set; } = true;
    public bool ShowImages { get; set; } = false;
    public bool ShowNotes { get; set; } = true;
    public bool ShowShippingInfo { get; set; } = true;
    public string? CompanyLogo { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyAddress { get; set; }
    public string? FooterMessage { get; set; }
}

// ==================== Generation Request DTOs ====================

public record GeneratePackingSlipRequest(
    int OrderId,
    PackingSlipSettings? Settings = null
);

public record GenerateBulkPackingSlipsRequest(
    int[] OrderIds,
    PackingSlipSettings? Settings = null,
    bool CombineIntoPdf = true
);

public record PackingSlipResult(
    int OrderId,
    string OrderNumber,
    byte[] PdfData,
    bool Success,
    string? Error = null
);

public record BulkPackingSlipResult(
    int TotalOrders,
    int SuccessCount,
    int FailedCount,
    byte[]? CombinedPdfData,
    List<PackingSlipResult> Results
);

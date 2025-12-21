using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Algora.Web.Pages.Orders;

[Authorize]
public class InvoiceDownloadModel : PageModel
{
    private readonly IShopifyOrderService _orderService;
    private readonly QuestPdfInvoiceGeneratorService _pdfGenerator;
    private readonly ILogger<InvoiceDownloadModel> _logger;

    public InvoiceDownloadModel(
        IShopifyOrderService orderService,
        QuestPdfInvoiceGeneratorService pdfGenerator,
        ILogger<InvoiceDownloadModel> logger)
    {
        _orderService = orderService;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        try
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for invoice download", id);
                return NotFound($"Order #{id} not found.");
            }

            // Build customer name
            var customerName = order.Customer != null
                ? $"{order.Customer.FirstName} {order.Customer.LastName}".Trim()
                : (order.Email ?? "Guest Customer");

            // Build billing address
            var billingAddress = BuildAddress(order.BillingAddress);

            // Build shipping address
            var shippingAddress = BuildAddress(order.ShippingAddress, includeNameFromAddress: true);

            // Build line items
            var lines = order.LineItems.Select(item => new InvoiceLineDto
            {
                ProductName = item.Title ?? "Unknown Product",
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList();

            var subtotal = lines.Sum(l => l.Total);

            // Create invoice DTO
            var invoiceDto = new InvoicePdfDto
            {
                InvoiceNumber = order.Name ?? $"INV-{order.Id}",
                InvoiceDate = order.CreatedAt,
                CustomerName = customerName,
                CustomerEmail = order.Email ?? order.Customer?.Email ?? string.Empty,
                BillingAddress = billingAddress,
                ShippingAddress = shippingAddress,
                Lines = lines,
                Subtotal = subtotal,
                Tax = 0,
                Total = order.TotalPrice
            };

            // Generate PDF directly using QuestPDF
            var pdf = await _pdfGenerator.GenerateInvoicePdfAsync(invoiceDto);

            _logger.LogInformation("Generated PDF invoice for order {OrderId}", id);

            // Return PDF file
            var fileName = $"Invoice-{order.Name?.Replace("#", "") ?? order.Id.ToString()}-{order.CreatedAt:yyyyMMdd}.pdf";
            return File(pdf, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invoice PDF for order {OrderId}", id);
            return StatusCode(500, "Failed to generate invoice. Please try again later.");
        }
    }

    private static string BuildAddress(Application.DTOs.Order.AddressDto? address, bool includeNameFromAddress = false)
    {
        if (address == null) return string.Empty;

        var parts = new List<string>();

        if (includeNameFromAddress && !string.IsNullOrEmpty(address.Name))
            parts.Add(address.Name);

        if (!string.IsNullOrEmpty(address.Address1))
            parts.Add(address.Address1);

        if (!string.IsNullOrEmpty(address.Address2))
            parts.Add(address.Address2);

        var cityLine = new List<string>();
        if (!string.IsNullOrEmpty(address.City))
            cityLine.Add(address.City);
        if (!string.IsNullOrEmpty(address.Province))
            cityLine.Add(address.Province);
        if (!string.IsNullOrEmpty(address.Zip))
            cityLine.Add(address.Zip);
        if (cityLine.Count > 0)
            parts.Add(string.Join(", ", cityLine));

        if (!string.IsNullOrEmpty(address.Country))
            parts.Add(address.Country);

        return string.Join("\n", parts);
    }
}

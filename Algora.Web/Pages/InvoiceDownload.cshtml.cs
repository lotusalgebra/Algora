using System.Threading.Tasks;
using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages
{
    public class InvoiceDownloadModel : PageModel
    {
        private readonly QuestPdfInvoiceGeneratorService _pdfGenerator;

        public InvoiceDownloadModel(QuestPdfInvoiceGeneratorService pdfGenerator)
        {
            _pdfGenerator = pdfGenerator;
        }

        // GET /InvoiceDownload?invoiceNumber=INV-001
        public async Task<IActionResult> OnGetAsync(string invoiceNumber)
        {
            // Create a sample invoice for testing
            var invoice = new InvoicePdfDto
            {
                InvoiceNumber = invoiceNumber ?? "INV-000",
                CustomerName = "Sample Customer",
                CustomerEmail = "customer@example.com",
                BillingAddress = "123 Main St\nCity, State 12345\nCountry",
                InvoiceDate = System.DateTime.UtcNow,
                Lines = new[]
                {
                    new InvoiceLineDto { ProductName = "Sample Product", Quantity = 1, Price = 99.99m }
                },
                Subtotal = 99.99m,
                Tax = 0m,
                Total = 99.99m
            };

            var pdf = await _pdfGenerator.GenerateInvoicePdfAsync(invoice);

            return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }
    }
}

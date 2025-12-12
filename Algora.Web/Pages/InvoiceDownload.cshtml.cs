using System.Threading.Tasks;
using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages
{
    public class InvoiceDownloadModel : PageModel
    {
        private readonly IInvoiceTemplateService _templateService;
        private readonly IPdfGeneratorService _pdfGenerator;

        public InvoiceDownloadModel(IInvoiceTemplateService templateService, IPdfGeneratorService pdfGenerator)
        {
            _templateService = templateService;
            _pdfGenerator = pdfGenerator;
        }

        // GET /InvoiceDownload?invoiceNumber=INV-001
        public async Task<IActionResult> OnGetAsync(string invoiceNumber)
        {
            // TODO: Replace with real lookup for invoiceNumber (DB/service)
            var invoice = new InvoicePdfDto
            {
                InvoiceNumber = invoiceNumber ?? "INV-000",
                CustomerName = "Acme Co.",
                CustomerEmail = "billing@acme.example",
                BillingAddress = "123 Main St, City",
                InvoiceDate = System.DateTime.UtcNow,
                Lines = System.Array.Empty<Algora.Application.DTOs.InvoiceLineDto>(),
                Subtotal = 0m,
                Tax = 0m,
                Total = 0m
            };

            var html = await _templateService.RenderInvoiceHtmlAsync(invoice);
            var pdf = await _pdfGenerator.GeneratePdfAsync(html);

            return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }
    }
}
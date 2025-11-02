using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IInvoiceTemplateService
    {
        Task<string> RenderInvoiceHtmlAsync(InvoicePdfDto model, string templateName = "Default");
    }
}

using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IShopifyInvoiceService
    {
        Task<IEnumerable<InvoiceDto>> GetAllAsync(int limit = 25);
        Task<InvoiceDto?> GetByIdAsync(long id);
        Task<InvoiceDto?> CreateInvoiceAsync(string email, string title, decimal price);
        Task SendInvoiceAsync(long draftOrderId);
        Task CompleteInvoiceAsync(long draftOrderId);
        Task CancelInvoiceAsync(long draftOrderId);
    }
}

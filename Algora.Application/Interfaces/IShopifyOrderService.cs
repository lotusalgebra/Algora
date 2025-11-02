using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IShopifyOrderService
    {
        Task<IEnumerable<OrderDto>> GetAllAsync(int limit = 25);
        Task<OrderDto?> GetByIdAsync(long id);
        Task<OrderDto?> CreateAsync(OrderDto order);
        Task CancelAsync(long id);
        Task SendInvoiceAsync(long orderId);
    }
}

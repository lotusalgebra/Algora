using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IShopifyCustomerService
    {
        Task<IEnumerable<CustomerDto>> GetAllAsync(int limit = 25);
        Task<CustomerDto?> GetByIdAsync(long id);
        Task<CustomerDto?> CreateAsync(CustomerDto customer);
        Task<CustomerDto?> UpdateAsync(long id, CustomerDto customer);
        Task DeleteAsync(long id);
    }
}

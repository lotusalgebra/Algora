using Algora.Application.DTOs.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IAbandonedCartService
    {
        Task<IEnumerable<AbandonedCartDto>> GetAllAsync(DateTime? since = null);
        Task<bool> SendReminderAsync(long checkoutId);
    }
}

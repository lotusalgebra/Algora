using Algora.Application.DTOs;

namespace Algora.Application.Interfaces
{
    public interface IAbandonedCartService
    {
        Task<IEnumerable<AbandonedCartDto>> GetAllAsync(DateTime? since = null);
        Task<bool> SendReminderAsync(long checkoutId);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IWhatsAppService
    {
        Task SendOrderUpdateAsync(string toPhone, string message);
    }
}

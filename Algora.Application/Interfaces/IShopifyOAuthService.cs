using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IShopifyOAuthService
    {
        Task<string> ExchangeCodeForTokenAsync(string shopDomain, string code);
        Task<string?> GetAccessTokenAsync(string shopDomain);
    }

}

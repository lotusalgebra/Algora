using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IShopifyGraphService
    {
        Task<string> PostAsync(string shopDomain, string accessToken, string query, object? variables = null);
    }

}

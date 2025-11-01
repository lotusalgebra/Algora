using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IShopifyGraphClient
    {
        Task<T?> QueryAsync<T>(string gql, object? variables = null);
    }
}

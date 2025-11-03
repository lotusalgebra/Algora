using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IShopContext
    {
        string ShopDomain { get; }
        string AccessToken { get; }
    }
}

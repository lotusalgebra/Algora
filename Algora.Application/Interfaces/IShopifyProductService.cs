using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IShopifyProductService
    {
        Task<IReadOnlyList<ProductDto>> GetProductsAsync(ProductSearchFilter filter, int first = 25);
        Task<VariantDto> CreateVariantAsync(string productGid, string title, decimal price, string? sku, string? option1 = null, string? option2 = null, string? option3 = null);
        Task<VariantDto> UpdateVariantAsync(string variantGid, string? title = null, decimal? price = null, string? sku = null, string? option1 = null, string? option2 = null, string? option3 = null);
    }
}

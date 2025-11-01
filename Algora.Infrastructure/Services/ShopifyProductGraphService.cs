using ShopifySharp;
using ShopifySharp.GraphQL;
using Algora.Core.Models;
using System.Text.RegularExpressions;
using Algora.Infrastructure.Shopify.Models;
using System.Text.Json;
using System.Linq;
using System.Reflection;

namespace Algora.Infrastructure.Services
{
    public class ShopifyProductGraphService
    {
        private readonly GraphService _graphService;

        public ShopifyProductGraphService(string shopUrl, string accessToken)
        {
            _graphService = new GraphService(shopUrl, accessToken);
        }

        public async Task<List<ProductViewModel>> GetAllProductsAsync()
        {
            var all = new List<ProductViewModel>();
            string? cursor = null;
            bool hasNext = true;

            while (hasNext)
            {
                var request = new GraphRequest()
                {
                    Query = @"
                  query ($first: Int!, $after: String) {
                    products(first: $first, after: $after) {
                      edges {
                        node {
                          id
                          title
                          descriptionHtml
                          vendor
                          tags
                          variants(first: 1) { edges { node { price } } }
                        }
                        cursor
                      }
                      pageInfo { hasNextPage }
                    }
                  }",
                    Variables = new Dictionary<string, object?>
                    {
                        { "first", 250 },
                        { "after", cursor }
                    }
                };

                // GraphService.PostAsync may return a typed object (GraphResponse) not a string.
                object? respObj = await _graphService.PostAsync(request);
                if (respObj == null)
                    break;

                // Robust normalization of the response into a JSON string:
                string raw = string.Empty;

                // 1) If the response object itself is a string
                if (respObj is string sResp)
                {
                    raw = sResp;
                }
                // 2) If the response object is a JsonElement (boxed struct)
                else if (respObj is JsonElement jeResp)
                {
                    raw = jeResp.GetRawText();
                }
                else
                {
                    // 3) Try to find a "Json" property (GraphResponse.Json) and handle string/JsonElement inside it
                    var jsonProp = respObj.GetType().GetProperty("Json", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (jsonProp != null)
                    {
                        var jsonVal = jsonProp.GetValue(respObj);
                        if (jsonVal is string js) raw = js;
                        else if (jsonVal is JsonElement je) raw = je.GetRawText();
                        else if (jsonVal != null) raw = JsonSerializer.Serialize(jsonVal);
                    }
                }

                // 4) Final fallback - use ToString() if it looks useful, otherwise serialize the whole object
                if (string.IsNullOrWhiteSpace(raw))
                {
                    var toStr = respObj.ToString();
                    raw = !string.IsNullOrWhiteSpace(toStr) ? toStr : JsonSerializer.Serialize(respObj);
                }

                if (string.IsNullOrWhiteSpace(raw))
                    break;

                var result = JsonSerializer.Deserialize<ProductsQueryResult>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Data?.Products?.Edges == null || result.Data.Products.Edges.Count == 0)
                {
                    // No more results or unexpected shape
                    break;
                }

                foreach (var edge in result.Data.Products.Edges)
                {
                    var p = edge.Node;
                    if (p == null) continue;

                    var view = new ProductViewModel
                    {
                        // Parse numeric id from Shopify global id (e.g. "gid://shopify/Product/12345")
                        Id = ParseShopifyId(p.Id),

                        Title = p.Title ?? string.Empty,
                        Description = p.DescriptionHtml ?? string.Empty,
                        Vendor = p.Vendor ?? string.Empty,
                        Tags = p.Tags != null ? string.Join(",", p.Tags) : string.Empty
                    };

                    // Safe price parsing
                    var priceStr = p.Variants?.Edges?.FirstOrDefault()?.Node?.Price;
                    if (!decimal.TryParse(priceStr, out var price))
                        price = 0m;
                    view.Price = price;

                    all.Add(view);
                }

                hasNext = result.Data.Products.PageInfo?.HasNextPage ?? false;
                cursor = result.Data.Products.Edges.LastOrDefault()?.Cursor;
                if (!hasNext) break;
            }

            return all;
        }

        private static long ParseShopifyId(string? gid)
        {
            if (string.IsNullOrWhiteSpace(gid)) return 0;
            // Extract trailing digits
            var m = Regex.Match(gid, @"(\d+)$");
            if (m.Success && long.TryParse(m.Groups[1].Value, out var id))
                return id;
            return 0;
        }

        // Similar for CreateProduct (mutation) / UpdateProduct (mutation)
    }
}

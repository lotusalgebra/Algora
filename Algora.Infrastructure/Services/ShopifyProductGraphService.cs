using ShopifySharp;
using ShopifySharp.GraphQL;
using Algora.Core.Models;
using System.Text.RegularExpressions;
using Algora.Infrastructure.Shopify.Models;
using System.Text.Json;
using System.Linq;
using System.Reflection;
using Algora.Application.Interfaces;
using Algora.Application.DTOs;
using System;

namespace Algora.Infrastructure.Services
{
    public class ShopifyProductGraphService : IShopifyProductService
    {
        private readonly GraphService _graphService;

        // Constructor updated for DI: accept IShopContext (registered in DI) instead of primitive strings.
        public ShopifyProductGraphService(IShopContext shopContext)
        {
            if (shopContext == null) throw new ArgumentNullException(nameof(shopContext));
            if (string.IsNullOrWhiteSpace(shopContext.ShopDomain))
                throw new InvalidOperationException("Shop domain missing in shop context.");
            if (string.IsNullOrWhiteSpace(shopContext.AccessToken))
                throw new InvalidOperationException("Shop access token missing in shop context.");

            _graphService = new GraphService(shopContext.ShopDomain, shopContext.AccessToken);
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
                    Variables = new Dictionary<string, object>
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

        // Helper: send GraphRequest and return normalized JSON string (or null)
        private static string? NormalizeResponseToJson(object? respObj)
        {
            if (respObj == null) return null;

            // 1) If the response object itself is a string
            if (respObj is string sResp) return sResp;

            // 2) If it's a JsonElement
            if (respObj is JsonElement jeResp) return jeResp.GetRawText();

            // 3) Try reflect "Json" property (GraphResult.Json etc.)
            var jsonProp = respObj.GetType().GetProperty("Json", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (jsonProp != null)
            {
                var jsonVal = jsonProp.GetValue(respObj);
                if (jsonVal is string js) return js;
                if (jsonVal is JsonElement je) return je.GetRawText();
                if (jsonVal != null) return JsonSerializer.Serialize(jsonVal);
            }

            // 4) Fallback: ToString or serialize
            var toStr = respObj.ToString();
            if (!string.IsNullOrWhiteSpace(toStr)) return toStr;
            return JsonSerializer.Serialize(respObj);
        }

        // Private convenience: run GraphQL and return raw JSON string or null
        private async Task<string?> SendGraphQueryRawAsync(string gql, object? variables = null)
        {
            var variableDict = new Dictionary<string, object>();
            if (variables != null)
            {
                foreach (var prop in variables.GetType().GetProperties())
                    variableDict[prop.Name] = prop.GetValue(variables);
            }

            var request = new GraphRequest
            {
                Query = gql,
                Variables = variableDict.Count == 0 ? null : variableDict
            };

            var respObj = await _graphService.PostAsync(request);
            var raw = NormalizeResponseToJson(respObj);
            if (string.IsNullOrWhiteSpace(raw)) return null;

            // Some GraphService implementations return the whole response (including "data"), others may already return the "data".
            // Ensure we return the whole JSON text so callers can parse "data" as needed.
            return raw;
        }

        // Build search query same as other service
        private static string? BuildSearchQuery(ProductSearchFilter filter)
        {
            var clauses = new List<string>();
            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var t = filter.Name!.Replace("\"", "");
                clauses.Add($"title:*{t}*");
            }
            if (!string.IsNullOrWhiteSpace(filter.Tag))
            {
                // Tag must be quoted
                var tag = filter.Tag!.Replace("'", " "); // sanitize simple quotes
                clauses.Add($"tag:'{tag}'");
            }
            if (filter.MinPrice.HasValue)
            {
                clauses.Add($"variants.price:>={filter.MinPrice.Value}");
            }
            if (filter.MaxPrice.HasValue)
            {
                clauses.Add($"variants.price:<={filter.MaxPrice.Value}");
            }
            if (clauses.Count == 0) return null;
            return string.Join(" AND ", clauses);
        }

        public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(ProductSearchFilter filter, int first = 25)
        {
            var query = BuildSearchQuery(filter);
            var gql = @"query GetProducts($first:Int!, $query:String) {
              products(first: $first, query: $query) {
                edges {
                  node {
                    id
                    title
                    handle
                    tags
                    variants(first: 50) {
                      nodes {
                        id
                        title
                        sku
                        price
                        option1
                        option2
                        option3
                      }
                    }
                  }
                }
              }
            }";

            var raw = await SendGraphQueryRawAsync(gql, new { first, query });
            if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<ProductDto>();

            using var doc = JsonDocument.Parse(raw);
            // Response may be either { "data": { ... } } or already the inner data object.
            var root = doc.RootElement;
            JsonElement dataEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) dataEl = d;
            else dataEl = root;

            if (!dataEl.TryGetProperty("products", out var productsEl))
            {
                // No products
                return Array.Empty<ProductDto>();
            }

            var list = new List<ProductDto>();
            if (productsEl.TryGetProperty("edges", out var edges) && edges.ValueKind == JsonValueKind.Array)
            {
                foreach (var edge in edges.EnumerateArray())
                {
                    if (!edge.TryGetProperty("node", out var node)) continue;
                    var gid = node.GetProperty("id").GetString() ?? string.Empty;
                    var title = node.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty;
                    var handle = node.TryGetProperty("handle", out var h) ? h.GetString() : null;

                    var tags = new List<string>();
                    if (node.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var ti in tagsEl.EnumerateArray())
                            if (ti.ValueKind == JsonValueKind.String) tags.Add(ti.GetString()!);
                    }

                    var variants = new List<VariantDto>();
                    if (node.TryGetProperty("variants", out var variantsEl))
                    {
                        // first try "nodes"
                        if (variantsEl.TryGetProperty("nodes", out var nodesEl) && nodesEl.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var v in nodesEl.EnumerateArray())
                            {
                                var vid = v.TryGetProperty("id", out var pid) ? pid.GetString() ?? string.Empty : string.Empty;
                                var vtitle = v.TryGetProperty("title", out var vt) ? vt.GetString() ?? string.Empty : string.Empty;
                                var sku = v.TryGetProperty("sku", out var vs) && vs.ValueKind != JsonValueKind.Null ? vs.GetString() : null;
                                decimal? price = null;
                                if (v.TryGetProperty("price", out var vp) && vp.ValueKind != JsonValueKind.Null)
                                {
                                    if (vp.ValueKind == JsonValueKind.Number && vp.TryGetDecimal(out var pd)) price = pd;
                                    else if (vp.ValueKind == JsonValueKind.String && decimal.TryParse(vp.GetString(), out var pd2)) price = pd2;
                                }
                                var option1 = v.TryGetProperty("option1", out var o1) && o1.ValueKind != JsonValueKind.Null ? o1.GetString() : null;
                                var option2 = v.TryGetProperty("option2", out var o2) && o2.ValueKind != JsonValueKind.Null ? o2.GetString() : null;
                                var option3 = v.TryGetProperty("option3", out var o3) && o3.ValueKind != JsonValueKind.Null ? o3.GetString() : null;
                                variants.Add(new VariantDto(vid, vtitle, sku, price, option1, option2, option3));
                            }
                        }
                        // fallback: maybe "edges" -> "node"
                        else if (variantsEl.TryGetProperty("edges", out var vedges) && vedges.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var ve in vedges.EnumerateArray())
                            {
                                if (!ve.TryGetProperty("node", out var vnode)) continue;
                                var vid = vnode.TryGetProperty("id", out var pid) ? pid.GetString() ?? string.Empty : string.Empty;
                                var vtitle = vnode.TryGetProperty("title", out var vt) ? vt.GetString() ?? string.Empty : string.Empty;
                                var sku = vnode.TryGetProperty("sku", out var vs) && vs.ValueKind != JsonValueKind.Null ? vs.GetString() : null;
                                decimal? price = null;
                                if (vnode.TryGetProperty("price", out var vp) && vp.ValueKind != JsonValueKind.Null)
                                {
                                    if (vp.ValueKind == JsonValueKind.Number && vp.TryGetDecimal(out var pd)) price = pd;
                                    else if (vp.ValueKind == JsonValueKind.String && decimal.TryParse(vp.GetString(), out var pd2)) price = pd2;
                                }
                                var option1 = vnode.TryGetProperty("option1", out var o1) && o1.ValueKind != JsonValueKind.Null ? o1.GetString() : null;
                                var option2 = vnode.TryGetProperty("option2", out var o2) && o2.ValueKind != JsonValueKind.Null ? o2.GetString() : null;
                                var option3 = vnode.TryGetProperty("option3", out var o3) && o3.ValueKind != JsonValueKind.Null ? o3.GetString() : null;
                                variants.Add(new VariantDto(vid, vtitle, sku, price, option1, option2, option3));
                            }
                        }
                    }

                    var dto = new ProductDto(gid, ParseShopifyId(gid), title, handle, tags, variants);
                    list.Add(dto);
                }
            }

            return list;
        }

        public async Task<VariantDto> CreateVariantAsync(string productGid, string title, decimal price, string? sku, string? option1 = null, string? option2 = null, string? option3 = null)
        {
            var gql = @"mutation CreateVariant($input: ProductVariantInput!) {
              productVariantCreate(input: $input) {
                variant { id title sku price option1 option2 option3 }
                userErrors { field message }
              }
            }";

            var variables = new
            {
                input = new
                {
                    productId = productGid,
                    title = title,
                    price = price,
                    sku = sku,
                    option1 = option1,
                    option2 = option2,
                    option3 = option3
                }
            };

            var raw = await SendGraphQueryRawAsync(gql, variables);
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException("Empty response from Shopify.");

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            JsonElement dataEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) dataEl = d;
            else dataEl = root;

            if (!dataEl.TryGetProperty("productVariantCreate", out var pvc))
            {
                // Some responses nest under productVariantCreate inside data -> productVariantCreate
                if (dataEl.TryGetProperty("productVariantCreate", out var _pvc)) pvc = _pvc;
                else if (dataEl.TryGetProperty("productVariantCreate", out _)) pvc = default;
            }

            // The typical shape is data.productVariantCreate
            var createEl = dataEl.GetProperty("productVariantCreate");

            var userErrors = createEl.TryGetProperty("userErrors", out var ue) && ue.ValueKind == JsonValueKind.Array ? ue.EnumerateArray().ToList() : new List<JsonElement>();
            if (userErrors.Count > 0)
            {
                var msgs = userErrors.Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "");
                throw new InvalidOperationException("Variant create failed: " + string.Join("; ", msgs));
            }

            var vEl = createEl.GetProperty("variant");
            var id = vEl.TryGetProperty("id", out var idp) ? idp.GetString() ?? string.Empty : string.Empty;
            var vtitle = vEl.TryGetProperty("title", out var vt) ? vt.GetString() ?? string.Empty : string.Empty;
            var vsku = vEl.TryGetProperty("sku", out var vs) && vs.ValueKind != JsonValueKind.Null ? vs.GetString() : null;
            decimal? vprice = null;
            if (vEl.TryGetProperty("price", out var vp) && vp.ValueKind != JsonValueKind.Null)
            {
                if (vp.ValueKind == JsonValueKind.Number && vp.TryGetDecimal(out var pd)) vprice = pd;
                else if (vp.ValueKind == JsonValueKind.String && decimal.TryParse(vp.GetString(), out var pd2)) vprice = pd2;
            }
            var o1 = vEl.TryGetProperty("option1", out var o1p) && o1p.ValueKind != JsonValueKind.Null ? o1p.GetString() : null;
            var o2 = vEl.TryGetProperty("option2", out var o2p) && o2p.ValueKind != JsonValueKind.Null ? o2p.GetString() : null;
            var o3 = vEl.TryGetProperty("option3", out var o3p) && o3p.ValueKind != JsonValueKind.Null ? o3p.GetString() : null;

            return new VariantDto(id, vtitle, vsku, vprice, o1, o2, o3);
        }

        public async Task<VariantDto> UpdateVariantAsync(string variantGid, string? title = null, decimal? price = null, string? sku = null, string? option1 = null, string? option2 = null, string? option3 = null)
        {
            var gql = @"mutation UpdateVariant($input: ProductVariantInput!) {
              productVariantUpdate(input: $input) {
                variant { id title sku price option1 option2 option3 }
                userErrors { field message }
              }
            }";

            var variables = new
            {
                input = new
                {
                    id = variantGid,
                    title = title,
                    price = price,
                    sku = sku,
                    option1 = option1,
                    option2 = option2,
                    option3 = option3
                }
            };

            var raw = await SendGraphQueryRawAsync(gql, variables);
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException("Empty response from Shopify.");

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            JsonElement dataEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) dataEl = d;
            else dataEl = root;

            var updateEl = dataEl.GetProperty("productVariantUpdate");

            var userErrors = updateEl.TryGetProperty("userErrors", out var ue) && ue.ValueKind == JsonValueKind.Array ? ue.EnumerateArray().ToList() : new List<JsonElement>();
            if (userErrors.Count > 0)
            {
                var msgs = userErrors.Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "");
                throw new InvalidOperationException("Variant update failed: " + string.Join("; ", msgs));
            }

            var vEl = updateEl.GetProperty("variant");
            var id = vEl.TryGetProperty("id", out var idp) ? idp.GetString() ?? string.Empty : string.Empty;
            var vtitle = vEl.TryGetProperty("title", out var vt) ? vt.GetString() ?? string.Empty : string.Empty;
            var vsku = vEl.TryGetProperty("sku", out var vs) && vs.ValueKind != JsonValueKind.Null ? vs.GetString() : null;
            decimal? vprice = null;
            if (vEl.TryGetProperty("price", out var vp) && vp.ValueKind != JsonValueKind.Null)
            {
                if (vp.ValueKind == JsonValueKind.Number && vp.TryGetDecimal(out var pd)) vprice = pd;
                else if (vp.ValueKind == JsonValueKind.String && decimal.TryParse(vp.GetString(), out var pd2)) vprice = pd2;
            }
            var o1 = vEl.TryGetProperty("option1", out var o1p) && o1p.ValueKind != JsonValueKind.Null ? o1p.GetString() : null;
            var o2 = vEl.TryGetProperty("option2", out var o2p) && o2p.ValueKind != JsonValueKind.Null ? o2p.GetString() : null;
            var o3 = vEl.TryGetProperty("option3", out var o3p) && o3p.ValueKind != JsonValueKind.Null ? o3p.GetString() : null;

            return new VariantDto(id, vtitle, vsku, vprice, o1, o2, o3);
        }

        // Similar for CreateProduct (mutation) / UpdateProduct (mutation)
    }
}

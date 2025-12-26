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
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services
{
    public class ShopifyProductGraphService : IShopifyProductService
    {
        private readonly GraphService _graphService;
        private readonly ILogger<ShopifyProductGraphService> _logger;

        // Constructor updated for DI: accept IShopContext (registered in DI) instead of primitive strings.
        public ShopifyProductGraphService(IShopContext shopContext, ILogger<ShopifyProductGraphService> logger)
        {
            _logger = logger;
            if (shopContext == null) throw new ArgumentNullException(nameof(shopContext));
            if (string.IsNullOrWhiteSpace(shopContext.ShopDomain))
                throw new InvalidOperationException("Shop domain missing in shop context.");
            if (string.IsNullOrWhiteSpace(shopContext.AccessToken))
                throw new InvalidOperationException("Shop access token missing in shop context.");

            _logger.LogInformation("Creating GraphService for shop: {ShopDomain}", shopContext.ShopDomain);
            _graphService = new GraphService(shopContext.ShopDomain, shopContext.AccessToken);
        }

        public async Task<List<ProductViewModel>> GetAllProductsAsync()
        {
            _logger.LogInformation("GetAllProductsAsync called");
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
                    Variables = cursor != null
                        ? new Dictionary<string, object> { { "first", 250 }, { "after", cursor } }
                        : new Dictionary<string, object> { { "first", 250 } }
                };

                try
                {
                    // GraphService.PostAsync returns GraphResult in ShopifySharp 6.x
                    _logger.LogInformation("Sending GraphQL request to Shopify");
                    var graphResult = await _graphService.PostAsync(request);

                    _logger.LogInformation("GraphResult type: {Type}", graphResult.GetType().FullName);

                    // Get raw JSON string from IJsonElement
                    // IJsonElement wraps JsonElement, try to get the underlying value
                    string raw;
                    var jsonInterface = graphResult.Json;

                    // Try to get RawText via reflection (IJsonElement may have GetRawText method)
                    var getRawTextMethod = jsonInterface.GetType().GetMethod("GetRawText");
                    if (getRawTextMethod != null)
                    {
                        raw = (string?)getRawTextMethod.Invoke(jsonInterface, null) ?? string.Empty;
                    }
                    else
                    {
                        // Fallback: serialize the object
                        raw = JsonSerializer.Serialize(jsonInterface);
                    }

                    _logger.LogInformation("Raw GraphQL response (first 1000 chars): {Response}",
                        raw.Length > 1000 ? raw.Substring(0, 1000) : raw);

                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        _logger.LogWarning("Raw response string is empty");
                        break;
                    }

                    // Parse the raw JSON to standard JsonDocument for inspection
                    ProductsQueryResult? result = null;
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    using (var doc = JsonDocument.Parse(raw))
                    {
                        var root = doc.RootElement;
                        _logger.LogInformation("Response root ValueKind: {Kind}", root.ValueKind);

                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            var props = root.EnumerateObject().Select(p => p.Name).ToList();
                            _logger.LogInformation("Response root properties: {Props}", string.Join(", ", props));

                            // Case 1: Response has "data" wrapper - {"data": {"products": {...}}}
                            if (root.TryGetProperty("data", out var dataEl))
                            {
                                _logger.LogInformation("Found 'data' property");
                                result = JsonSerializer.Deserialize<ProductsQueryResult>(raw, options);
                            }
                            // Case 2: Response is already the "data" content - {"products": {...}}
                            else if (root.TryGetProperty("products", out _))
                            {
                                _logger.LogInformation("Found 'products' directly at root");
                                result = new ProductsQueryResult
                                {
                                    Data = JsonSerializer.Deserialize<ProductsData>(raw, options)
                                };
                            }
                            else
                            {
                                _logger.LogWarning("Unexpected response structure. Root properties: {Props}", string.Join(", ", props));
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Response is not an object. ValueKind: {Kind}", root.ValueKind);
                        }
                    }

                    if (result?.Data?.Products?.Edges == null || result.Data.Products.Edges.Count == 0)
                    {
                        _logger.LogWarning("No products in response. Data is null: {DataNull}, Products is null: {ProductsNull}",
                            result?.Data == null, result?.Data?.Products == null);
                        break;
                    }

                    _logger.LogInformation("Retrieved {Count} products in this page", result.Data.Products.Edges.Count);

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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching products from Shopify GraphQL API");
                    throw;
                }
            }

            _logger.LogInformation("Total products retrieved: {Count}", all.Count);
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
                {
                    var value = prop.GetValue(variables);
                    if (value != null)
                        variableDict[prop.Name] = value;
                }
            }

            var request = new GraphRequest
            {
                Query = gql,
                Variables = variableDict.Count == 0 ? null : variableDict
            };

            var graphResult = await _graphService.PostAsync(request);

            // Extract raw JSON using same approach as GetAllProductsAsync
            string raw;
            var jsonInterface = graphResult.Json;

            // Try to get RawText via reflection (IJsonElement may have GetRawText method)
            var getRawTextMethod = jsonInterface.GetType().GetMethod("GetRawText");
            if (getRawTextMethod != null)
            {
                raw = (string?)getRawTextMethod.Invoke(jsonInterface, null) ?? string.Empty;
            }
            else
            {
                // Fallback: serialize the object
                raw = JsonSerializer.Serialize(jsonInterface);
            }

            if (string.IsNullOrWhiteSpace(raw)) return null;

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
            _logger.LogInformation("GetProductsAsync called with first={First}", first);
            var query = BuildSearchQuery(filter);
            _logger.LogInformation("Search query: {Query}", query ?? "(null)");

            // Use same approach as GetAllProductsAsync which works
            var request = new GraphRequest
            {
                Query = @"query ($first: Int!, $query: String) {
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
                            inventoryQuantity
                            selectedOptions {
                              name
                              value
                            }
                          }
                        }
                      }
                    }
                  }
                }",
                Variables = new Dictionary<string, object>
                {
                    { "first", first },
                    { "query", query! }
                }
            };

            // Remove null query from variables
            if (query == null)
                request.Variables.Remove("query");

            var graphResult = await _graphService.PostAsync(request);
            var raw = NormalizeResponseToJson(graphResult);

            _logger.LogInformation("Raw response length: {Length}", raw?.Length ?? 0);
            if (!string.IsNullOrWhiteSpace(raw) && raw.Length < 2000)
                _logger.LogInformation("Raw response: {Raw}", raw);

            if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<ProductDto>();

            using var doc = JsonDocument.Parse(raw);
            // Response may be either { "data": { ... } } or already the inner data object.
            var root = doc.RootElement;
            JsonElement dataEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) dataEl = d;
            else dataEl = root;

            if (!dataEl.TryGetProperty("products", out var productsEl))
            {
                _logger.LogWarning("No 'products' property found in response");
                return Array.Empty<ProductDto>();
            }

            _logger.LogInformation("Found 'products' property in response");
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
                                // Parse selectedOptions array
                                string? option1 = null, option2 = null, option3 = null;
                                if (v.TryGetProperty("selectedOptions", out var selOpts) && selOpts.ValueKind == JsonValueKind.Array)
                                {
                                    var optList = selOpts.EnumerateArray().ToList();
                                    if (optList.Count > 0) option1 = optList[0].TryGetProperty("value", out var v1) ? v1.GetString() : null;
                                    if (optList.Count > 1) option2 = optList[1].TryGetProperty("value", out var v2) ? v2.GetString() : null;
                                    if (optList.Count > 2) option3 = optList[2].TryGetProperty("value", out var v3) ? v3.GetString() : null;
                                }
                                // Parse inventoryQuantity
                                int? inventoryQuantity = null;
                                if (v.TryGetProperty("inventoryQuantity", out var invQty) && invQty.ValueKind == JsonValueKind.Number)
                                    inventoryQuantity = invQty.GetInt32();
                                variants.Add(new VariantDto(vid, vtitle, sku, price, option1, option2, option3, inventoryQuantity));
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
                                // Parse selectedOptions array
                                string? option1 = null, option2 = null, option3 = null;
                                if (vnode.TryGetProperty("selectedOptions", out var selOpts) && selOpts.ValueKind == JsonValueKind.Array)
                                {
                                    var optList = selOpts.EnumerateArray().ToList();
                                    if (optList.Count > 0) option1 = optList[0].TryGetProperty("value", out var v1) ? v1.GetString() : null;
                                    if (optList.Count > 1) option2 = optList[1].TryGetProperty("value", out var v2) ? v2.GetString() : null;
                                    if (optList.Count > 2) option3 = optList[2].TryGetProperty("value", out var v3) ? v3.GetString() : null;
                                }
                                // Parse inventoryQuantity
                                int? inventoryQuantity = null;
                                if (vnode.TryGetProperty("inventoryQuantity", out var invQty) && invQty.ValueKind == JsonValueKind.Number)
                                    inventoryQuantity = invQty.GetInt32();
                                variants.Add(new VariantDto(vid, vtitle, sku, price, option1, option2, option3, inventoryQuantity));
                            }
                        }
                    }

                    var dto = new ProductDto(gid, ParseShopifyId(gid), title, handle, tags, variants);
                    list.Add(dto);
                    _logger.LogInformation("Added product: {Title} with {VariantCount} variants", title, variants.Count);
                }
            }

            _logger.LogInformation("GetProductsAsync returning {Count} products", list.Count);
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

        public async Task<ProductDto> CreateProductAsync(CreateProductInput input)
        {
            // Build variants array for the mutation - only if variants are provided
            var variantsJson = new List<object>();

            if (input.Variants.Any())
            {
                foreach (var v in input.Variants)
                {
                    var optionValues = new List<object>();
                    if (!string.IsNullOrEmpty(v.Option1)) optionValues.Add(new { optionName = "Size", name = v.Option1 });
                    if (!string.IsNullOrEmpty(v.Option2)) optionValues.Add(new { optionName = "Color", name = v.Option2 });
                    if (!string.IsNullOrEmpty(v.Option3)) optionValues.Add(new { optionName = "Material", name = v.Option3 });

                    variantsJson.Add(new
                    {
                        price = v.Price.ToString("F2"),
                        sku = v.Sku,
                        optionValues = optionValues.Count > 0 ? optionValues : null
                    });
                }
            }

            // Build images array if provided
            var imagesJson = input.ImageUrls?
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => new { src = url })
                .ToList();

            var gql = @"mutation CreateProduct($input: ProductInput!) {
              productCreate(input: $input) {
                product {
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
                            inventoryQuantity
                    }
                  }
                }
                userErrors {
                  field
                  message
                }
              }
            }";

            // Build the input object dynamically
            var inputObj = new Dictionary<string, object?>
            {
                { "title", input.Title },
                { "descriptionHtml", input.Description },
                { "vendor", input.Vendor },
                { "productType", input.ProductType },
                { "tags", input.Tags }
            };

            // Only add variants if there are any
            if (variantsJson.Count > 0)
            {
                inputObj["variants"] = variantsJson;
            }

            // Only add images if there are any
            if (imagesJson != null && imagesJson.Count > 0)
            {
                inputObj["images"] = imagesJson;
            }

            var variables = new { input = inputObj };

            var raw = await SendGraphQueryRawAsync(gql, variables);
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Empty response from Shopify.");

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            JsonElement dataEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) dataEl = d;
            else dataEl = root;

            var createEl = dataEl.GetProperty("productCreate");

            // Check for user errors
            var userErrors = createEl.TryGetProperty("userErrors", out var ue) && ue.ValueKind == JsonValueKind.Array
                ? ue.EnumerateArray().ToList()
                : new List<JsonElement>();

            if (userErrors.Count > 0)
            {
                var msgs = userErrors.Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "");
                throw new InvalidOperationException("Product create failed: " + string.Join("; ", msgs));
            }

            var productEl = createEl.GetProperty("product");
            var gid = productEl.GetProperty("id").GetString() ?? string.Empty;
            var title = productEl.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty;
            var handle = productEl.TryGetProperty("handle", out var h) ? h.GetString() : null;

            var tags = new List<string>();
            if (productEl.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var ti in tagsEl.EnumerateArray())
                    if (ti.ValueKind == JsonValueKind.String) tags.Add(ti.GetString()!);
            }

            var variants = new List<VariantDto>();
            if (productEl.TryGetProperty("variants", out var variantsEl) &&
                variantsEl.TryGetProperty("nodes", out var nodesEl) &&
                nodesEl.ValueKind == JsonValueKind.Array)
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
                    variants.Add(new VariantDto(vid, vtitle, sku, price, null, null, null));
                }
            }

            return new ProductDto(gid, ParseShopifyId(gid), title, handle, tags, variants);
        }

        public async Task<ProductDto?> GetProductByIdAsync(long productId)
        {
            _logger.LogInformation("GetProductByIdAsync called for productId: {ProductId}", productId);

            var gql = @"query GetProduct($id: ID!) {
              product(id: $id) {
                id
                title
                descriptionHtml
                handle
                vendor
                productType
                tags
                images(first: 50) {
                  nodes {
                    id
                    url
                    altText
                    width
                    height
                  }
                }
                variants(first: 50) {
                  nodes {
                    id
                    title
                    sku
                    price
                    inventoryQuantity
                    selectedOptions {
                      name
                      value
                    }
                  }
                }
              }
            }";

            var productGid = $"gid://shopify/Product/{productId}";
            _logger.LogInformation("Querying Shopify for product GID: {ProductGid}", productGid);

            var raw = await SendGraphQueryRawAsync(gql, new { id = productGid });
            _logger.LogInformation("Raw response from Shopify (first 500 chars): {Response}",
                string.IsNullOrWhiteSpace(raw) ? "NULL/EMPTY" : (raw.Length > 500 ? raw.Substring(0, 500) : raw));

            if (string.IsNullOrWhiteSpace(raw)) return null;

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            JsonElement dataEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) dataEl = d;
            else dataEl = root;

            if (!dataEl.TryGetProperty("product", out var productEl) || productEl.ValueKind == JsonValueKind.Null)
                return null;

            var gid = productEl.GetProperty("id").GetString() ?? string.Empty;
            var title = productEl.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty;
            var handle = productEl.TryGetProperty("handle", out var h) ? h.GetString() : null;
            var description = productEl.TryGetProperty("descriptionHtml", out var desc) ? desc.GetString() : null;
            var vendor = productEl.TryGetProperty("vendor", out var vend) ? vend.GetString() : null;
            var productType = productEl.TryGetProperty("productType", out var pt) ? pt.GetString() : null;

            var tags = new List<string>();
            if (productEl.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var ti in tagsEl.EnumerateArray())
                    if (ti.ValueKind == JsonValueKind.String) tags.Add(ti.GetString()!);
            }

            // Parse images
            var images = new List<ProductImageDto>();
            if (productEl.TryGetProperty("images", out var imagesEl) &&
                imagesEl.TryGetProperty("nodes", out var imageNodes) &&
                imageNodes.ValueKind == JsonValueKind.Array)
            {
                int position = 0;
                foreach (var img in imageNodes.EnumerateArray())
                {
                    var imgId = img.TryGetProperty("id", out var imgIdProp) ? imgIdProp.GetString() ?? string.Empty : string.Empty;
                    var imgUrl = img.TryGetProperty("url", out var imgUrlProp) ? imgUrlProp.GetString() ?? string.Empty : string.Empty;
                    var imgAlt = img.TryGetProperty("altText", out var imgAltProp) && imgAltProp.ValueKind != JsonValueKind.Null ? imgAltProp.GetString() : null;
                    int? imgWidth = img.TryGetProperty("width", out var imgWidthProp) && imgWidthProp.ValueKind == JsonValueKind.Number ? imgWidthProp.GetInt32() : null;
                    int? imgHeight = img.TryGetProperty("height", out var imgHeightProp) && imgHeightProp.ValueKind == JsonValueKind.Number ? imgHeightProp.GetInt32() : null;
                    images.Add(new ProductImageDto(imgId, imgUrl, imgAlt, position++, imgWidth, imgHeight));
                }
            }

            var variants = new List<VariantDto>();
            if (productEl.TryGetProperty("variants", out var variantsEl) &&
                variantsEl.TryGetProperty("nodes", out var nodesEl) &&
                nodesEl.ValueKind == JsonValueKind.Array)
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

                    // Extract options from selectedOptions
                    string? option1 = null, option2 = null, option3 = null;
                    if (v.TryGetProperty("selectedOptions", out var optionsEl) && optionsEl.ValueKind == JsonValueKind.Array)
                    {
                        var options = optionsEl.EnumerateArray().ToList();
                        if (options.Count > 0) option1 = options[0].TryGetProperty("value", out var v1) ? v1.GetString() : null;
                        if (options.Count > 1) option2 = options[1].TryGetProperty("value", out var v2) ? v2.GetString() : null;
                        if (options.Count > 2) option3 = options[2].TryGetProperty("value", out var v3) ? v3.GetString() : null;
                    }

                    // Parse inventoryQuantity
                    int? inventoryQuantity = null;
                    if (v.TryGetProperty("inventoryQuantity", out var invQty) && invQty.ValueKind == JsonValueKind.Number)
                        inventoryQuantity = invQty.GetInt32();
                    variants.Add(new VariantDto(vid, vtitle, sku, price, option1, option2, option3, inventoryQuantity));
                }
            }

            return new ProductDto(gid, ParseShopifyId(gid), title, handle, tags, variants, images, description, vendor, productType);
        }

        public async Task<ProductDto> UpdateProductAsync(UpdateProductInput input)
        {
            var productGid = $"gid://shopify/Product/{input.ProductId}";

            var gql = @"mutation UpdateProduct($input: ProductInput!) {
              productUpdate(input: $input) {
                product {
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
                            inventoryQuantity
                    }
                  }
                }
                userErrors {
                  field
                  message
                }
              }
            }";

            var variables = new
            {
                input = new
                {
                    id = productGid,
                    title = input.Title,
                    descriptionHtml = input.Description,
                    vendor = input.Vendor,
                    productType = input.ProductType,
                    tags = input.Tags
                }
            };

            var raw = await SendGraphQueryRawAsync(gql, variables);
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Empty response from Shopify.");

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            JsonElement dataEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) dataEl = d;
            else dataEl = root;

            var updateEl = dataEl.GetProperty("productUpdate");

            // Check for user errors
            var userErrors = updateEl.TryGetProperty("userErrors", out var ue) && ue.ValueKind == JsonValueKind.Array
                ? ue.EnumerateArray().ToList()
                : new List<JsonElement>();

            if (userErrors.Count > 0)
            {
                var msgs = userErrors.Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "");
                throw new InvalidOperationException("Product update failed: " + string.Join("; ", msgs));
            }

            var productEl = updateEl.GetProperty("product");
            var gid = productEl.GetProperty("id").GetString() ?? string.Empty;
            var title = productEl.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty;
            var handle = productEl.TryGetProperty("handle", out var h) ? h.GetString() : null;

            var tags = new List<string>();
            if (productEl.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var ti in tagsEl.EnumerateArray())
                    if (ti.ValueKind == JsonValueKind.String) tags.Add(ti.GetString()!);
            }

            var variants = new List<VariantDto>();
            if (productEl.TryGetProperty("variants", out var variantsEl) &&
                variantsEl.TryGetProperty("nodes", out var nodesEl) &&
                nodesEl.ValueKind == JsonValueKind.Array)
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
                    variants.Add(new VariantDto(vid, vtitle, sku, price, null, null, null));
                }
            }

            // Update variants if provided
            foreach (var variantInput in input.Variants)
            {
                if (variantInput.IsNew)
                {
                    // Create new variant
                    await CreateVariantAsync(productGid, variantInput.Title ?? "Default",
                        variantInput.Price, variantInput.Sku,
                        variantInput.Option1, variantInput.Option2, variantInput.Option3);
                }
                else if (!string.IsNullOrEmpty(variantInput.VariantId))
                {
                    // Update existing variant
                    await UpdateVariantAsync(variantInput.VariantId,
                        variantInput.Title, variantInput.Price, variantInput.Sku,
                        variantInput.Option1, variantInput.Option2, variantInput.Option3);
                }
            }

            return new ProductDto(gid, ParseShopifyId(gid), title, handle, tags, variants);
        }

        public async Task DeleteProductAsync(long productId)
        {
            var productGid = $"gid://shopify/Product/{productId}";

            var gql = @"mutation DeleteProduct($input: ProductDeleteInput!) {
              productDelete(input: $input) {
                deletedProductId
                userErrors {
                  field
                  message
                }
              }
            }";

            var variables = new
            {
                input = new
                {
                    id = productGid
                }
            };

            var raw = await SendGraphQueryRawAsync(gql, variables);
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Empty response from Shopify.");

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            JsonElement dataEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) dataEl = d;
            else dataEl = root;

            var deleteEl = dataEl.GetProperty("productDelete");

            // Check for user errors
            var userErrors = deleteEl.TryGetProperty("userErrors", out var ue) && ue.ValueKind == JsonValueKind.Array
                ? ue.EnumerateArray().ToList()
                : new List<JsonElement>();

            if (userErrors.Count > 0)
            {
                var msgs = userErrors.Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "");
                throw new InvalidOperationException("Product delete failed: " + string.Join("; ", msgs));
            }

            _logger.LogInformation("Product {ProductId} deleted successfully", productId);
        }

        public async Task<IReadOnlyList<ProductImageDto>> GetProductImagesAsync(long productId)
        {
            var product = await GetProductByIdAsync(productId);
            return product?.Images ?? Array.Empty<ProductImageDto>();
        }

        public async Task<ProductImageDto> UploadProductImageAsync(UploadProductImageInput input)
        {
            var productGid = $"gid://shopify/Product/{input.ProductId}";

            // If we have a URL, use it directly via productAppendImages
            if (!string.IsNullOrWhiteSpace(input.ImageUrl))
            {
                var gql = @"mutation AppendImages($input: ProductAppendImagesInput!) {
                  productAppendImages(input: $input) {
                    newImages {
                      id
                      url
                      altText
                      width
                      height
                    }
                    userErrors {
                      field
                      message
                    }
                  }
                }";

                var variables = new
                {
                    input = new
                    {
                        id = productGid,
                        images = new[]
                        {
                            new
                            {
                                src = input.ImageUrl,
                                altText = input.Alt
                            }
                        }
                    }
                };

                var raw = await SendGraphQueryRawAsync(gql, variables);
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("Empty response from Shopify.");

                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                JsonElement dataEl;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) dataEl = d;
                else dataEl = root;

                var appendEl = dataEl.GetProperty("productAppendImages");

                // Check for user errors
                var userErrors = appendEl.TryGetProperty("userErrors", out var ue) && ue.ValueKind == JsonValueKind.Array
                    ? ue.EnumerateArray().ToList()
                    : new List<JsonElement>();

                if (userErrors.Count > 0)
                {
                    var msgs = userErrors.Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "");
                    throw new InvalidOperationException("Image upload failed: " + string.Join("; ", msgs));
                }

                if (appendEl.TryGetProperty("newImages", out var newImages) &&
                    newImages.ValueKind == JsonValueKind.Array &&
                    newImages.GetArrayLength() > 0)
                {
                    var img = newImages.EnumerateArray().First();
                    var imgId = img.TryGetProperty("id", out var imgIdProp) ? imgIdProp.GetString() ?? string.Empty : string.Empty;
                    var imgUrl = img.TryGetProperty("url", out var imgUrlProp) ? imgUrlProp.GetString() ?? string.Empty : string.Empty;
                    var imgAlt = img.TryGetProperty("altText", out var imgAltProp) && imgAltProp.ValueKind != JsonValueKind.Null ? imgAltProp.GetString() : null;
                    int? imgWidth = img.TryGetProperty("width", out var imgWidthProp) && imgWidthProp.ValueKind == JsonValueKind.Number ? imgWidthProp.GetInt32() : null;
                    int? imgHeight = img.TryGetProperty("height", out var imgHeightProp) && imgHeightProp.ValueKind == JsonValueKind.Number ? imgHeightProp.GetInt32() : null;
                    return new ProductImageDto(imgId, imgUrl, imgAlt, null, imgWidth, imgHeight);
                }

                throw new InvalidOperationException("No image was created.");
            }

            // For base64 file upload, use staged uploads
            if (!string.IsNullOrWhiteSpace(input.Base64Data))
            {
                // First, create a staged upload
                var stagedUploadGql = @"mutation stagedUploadsCreate($input: [StagedUploadInput!]!) {
                  stagedUploadsCreate(input: $input) {
                    stagedTargets {
                      url
                      resourceUrl
                      parameters {
                        name
                        value
                      }
                    }
                    userErrors {
                      field
                      message
                    }
                  }
                }";

                var fileSize = Convert.FromBase64String(input.Base64Data).Length;
                var stagedVars = new
                {
                    input = new[]
                    {
                        new
                        {
                            filename = input.FileName ?? "image.jpg",
                            mimeType = input.ContentType ?? "image/jpeg",
                            resource = "PRODUCT_IMAGE",
                            fileSize = fileSize.ToString(),
                            httpMethod = "POST"
                        }
                    }
                };

                var stagedRaw = await SendGraphQueryRawAsync(stagedUploadGql, stagedVars);
                if (string.IsNullOrWhiteSpace(stagedRaw))
                    throw new InvalidOperationException("Empty response from staged upload creation.");

                using var stagedDoc = JsonDocument.Parse(stagedRaw);
                var stagedRoot = stagedDoc.RootElement;
                JsonElement stagedDataEl;
                if (stagedRoot.ValueKind == JsonValueKind.Object && stagedRoot.TryGetProperty("data", out var sd)) stagedDataEl = sd;
                else stagedDataEl = stagedRoot;

                var stagedCreate = stagedDataEl.GetProperty("stagedUploadsCreate");

                // Check for errors
                var stagedErrors = stagedCreate.TryGetProperty("userErrors", out var sue) && sue.ValueKind == JsonValueKind.Array
                    ? sue.EnumerateArray().ToList()
                    : new List<JsonElement>();

                if (stagedErrors.Count > 0)
                {
                    var msgs = stagedErrors.Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "");
                    throw new InvalidOperationException("Staged upload creation failed: " + string.Join("; ", msgs));
                }

                if (!stagedCreate.TryGetProperty("stagedTargets", out var targets) ||
                    targets.ValueKind != JsonValueKind.Array ||
                    targets.GetArrayLength() == 0)
                {
                    throw new InvalidOperationException("No staged upload target returned.");
                }

                var target = targets.EnumerateArray().First();
                var uploadUrl = target.GetProperty("url").GetString() ?? "";
                var resourceUrl = target.GetProperty("resourceUrl").GetString() ?? "";

                // Get parameters for the upload
                var parameters = new Dictionary<string, string>();
                if (target.TryGetProperty("parameters", out var paramsEl) && paramsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var param in paramsEl.EnumerateArray())
                    {
                        var name = param.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                        var value = param.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "";
                        if (!string.IsNullOrEmpty(name))
                            parameters[name] = value;
                    }
                }

                // Upload the file to the staged URL
                using var httpClient = new System.Net.Http.HttpClient();
                using var formContent = new System.Net.Http.MultipartFormDataContent();

                // Add all parameters first
                foreach (var param in parameters)
                {
                    formContent.Add(new System.Net.Http.StringContent(param.Value), param.Key);
                }

                // Add the file
                var fileBytes = Convert.FromBase64String(input.Base64Data);
                var fileContent = new System.Net.Http.ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(input.ContentType ?? "image/jpeg");
                formContent.Add(fileContent, "file", input.FileName ?? "image.jpg");

                var uploadResponse = await httpClient.PostAsync(uploadUrl, formContent);
                if (!uploadResponse.IsSuccessStatusCode)
                {
                    var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"File upload failed: {uploadResponse.StatusCode} - {errorContent}");
                }

                // Append the uploaded image to the product using the resource URL
                var fileCreateGql = @"mutation ProductAppendImages($input: ProductAppendImagesInput!) {
                  productAppendImages(input: $input) {
                    newImages {
                      id
                      url
                      altText
                      width
                      height
                    }
                    userErrors {
                      field
                      message
                    }
                  }
                }";

                var appendVars = new
                {
                    input = new
                    {
                        id = productGid,
                        images = new[]
                        {
                            new
                            {
                                src = resourceUrl,
                                altText = input.Alt
                            }
                        }
                    }
                };

                var appendRaw = await SendGraphQueryRawAsync(fileCreateGql, appendVars);
                if (string.IsNullOrWhiteSpace(appendRaw))
                    throw new InvalidOperationException("Empty response from image append.");

                using var appendDoc = JsonDocument.Parse(appendRaw);
                var appendRoot = appendDoc.RootElement;
                JsonElement appendDataEl;
                if (appendRoot.ValueKind == JsonValueKind.Object && appendRoot.TryGetProperty("data", out var ad)) appendDataEl = ad;
                else appendDataEl = appendRoot;

                var appendEl = appendDataEl.GetProperty("productAppendImages");

                var appendErrors = appendEl.TryGetProperty("userErrors", out var aue) && aue.ValueKind == JsonValueKind.Array
                    ? aue.EnumerateArray().ToList()
                    : new List<JsonElement>();

                if (appendErrors.Count > 0)
                {
                    var msgs = appendErrors.Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "");
                    throw new InvalidOperationException("Image append failed: " + string.Join("; ", msgs));
                }

                if (appendEl.TryGetProperty("newImages", out var newImgs) &&
                    newImgs.ValueKind == JsonValueKind.Array &&
                    newImgs.GetArrayLength() > 0)
                {
                    var img = newImgs.EnumerateArray().First();
                    var imgId = img.TryGetProperty("id", out var imgIdProp) ? imgIdProp.GetString() ?? string.Empty : string.Empty;
                    var imgUrl = img.TryGetProperty("url", out var imgUrlProp) ? imgUrlProp.GetString() ?? string.Empty : string.Empty;
                    var imgAlt = img.TryGetProperty("altText", out var imgAltProp) && imgAltProp.ValueKind != JsonValueKind.Null ? imgAltProp.GetString() : null;
                    int? imgWidth = img.TryGetProperty("width", out var imgWidthProp) && imgWidthProp.ValueKind == JsonValueKind.Number ? imgWidthProp.GetInt32() : null;
                    int? imgHeight = img.TryGetProperty("height", out var imgHeightProp) && imgHeightProp.ValueKind == JsonValueKind.Number ? imgHeightProp.GetInt32() : null;
                    return new ProductImageDto(imgId, imgUrl, imgAlt, null, imgWidth, imgHeight);
                }

                throw new InvalidOperationException("No image was created.");
            }

            throw new ArgumentException("Either ImageUrl or Base64Data must be provided.");
        }

        public async Task DeleteProductImageAsync(long productId, string imageId)
        {
            var productGid = $"gid://shopify/Product/{productId}";

            var gql = @"mutation productDeleteImages($id: ID!, $imageIds: [ID!]!) {
              productDeleteImages(id: $id, imageIds: $imageIds) {
                deletedImageIds
                userErrors {
                  field
                  message
                }
              }
            }";

            var variables = new
            {
                id = productGid,
                imageIds = new[] { imageId }
            };

            var raw = await SendGraphQueryRawAsync(gql, variables);
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Empty response from Shopify.");

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            JsonElement dataEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) dataEl = d;
            else dataEl = root;

            var deleteEl = dataEl.GetProperty("productDeleteImages");

            // Check for user errors
            var userErrors = deleteEl.TryGetProperty("userErrors", out var ue) && ue.ValueKind == JsonValueKind.Array
                ? ue.EnumerateArray().ToList()
                : new List<JsonElement>();

            if (userErrors.Count > 0)
            {
                var msgs = userErrors.Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "");
                throw new InvalidOperationException("Image delete failed: " + string.Join("; ", msgs));
            }

            _logger.LogInformation("Image {ImageId} deleted from product {ProductId}", imageId, productId);
        }

        public async Task<ProductImageDto> UpdateProductImageAltAsync(long productId, string imageId, string altText)
        {
            var productGid = $"gid://shopify/Product/{productId}";

            var gql = @"mutation productImageUpdate($productId: ID!, $image: ImageInput!) {
              productImageUpdate(productId: $productId, image: $image) {
                image {
                  id
                  url
                  altText
                  width
                  height
                }
                userErrors {
                  field
                  message
                }
              }
            }";

            var variables = new
            {
                productId = productGid,
                image = new
                {
                    id = imageId,
                    altText = altText
                }
            };

            var raw = await SendGraphQueryRawAsync(gql, variables);
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Empty response from Shopify.");

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            JsonElement dataEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) dataEl = d;
            else dataEl = root;

            var updateEl = dataEl.GetProperty("productImageUpdate");

            // Check for user errors
            var userErrors = updateEl.TryGetProperty("userErrors", out var ue) && ue.ValueKind == JsonValueKind.Array
                ? ue.EnumerateArray().ToList()
                : new List<JsonElement>();

            if (userErrors.Count > 0)
            {
                var msgs = userErrors.Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "");
                throw new InvalidOperationException("Image update failed: " + string.Join("; ", msgs));
            }

            var img = updateEl.GetProperty("image");
            var imgId = img.TryGetProperty("id", out var imgIdProp) ? imgIdProp.GetString() ?? string.Empty : string.Empty;
            var imgUrl = img.TryGetProperty("url", out var imgUrlProp) ? imgUrlProp.GetString() ?? string.Empty : string.Empty;
            var imgAlt = img.TryGetProperty("altText", out var imgAltProp) && imgAltProp.ValueKind != JsonValueKind.Null ? imgAltProp.GetString() : null;
            int? imgWidth = img.TryGetProperty("width", out var imgWidthProp) && imgWidthProp.ValueKind == JsonValueKind.Number ? imgWidthProp.GetInt32() : null;
            int? imgHeight = img.TryGetProperty("height", out var imgHeightProp) && imgHeightProp.ValueKind == JsonValueKind.Number ? imgHeightProp.GetInt32() : null;

            return new ProductImageDto(imgId, imgUrl, imgAlt, null, imgWidth, imgHeight);
        }
    }
}

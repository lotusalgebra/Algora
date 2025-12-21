using System.Collections.Generic;

namespace Algora.Infrastructure.Shopify.Models
{
    // Root object matching the "data" section for the products query
    public class ProductsQueryResult
    {
        public ProductsData? Data { get; set; }
    }

    public class ProductsData
    {
        public ProductConnection? Products { get; set; }
    }

    public class ProductConnection
    {
        public List<ProductEdge>? Edges { get; set; }
        public PageInfo? PageInfo { get; set; }
    }

    public class ProductEdge
    {
        public ProductNode? Node { get; set; }
        public string? Cursor { get; set; }
    }

    public class ProductNode
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? DescriptionHtml { get; set; }
        public string? Vendor { get; set; }
        public List<string>? Tags { get; set; }
        public VariantConnection? Variants { get; set; }
    }

    public class VariantConnection
    {
        public List<VariantEdge>? Edges { get; set; }
    }

    public class VariantEdge
    {
        public VariantNode? Node { get; set; }
    }

    public class VariantNode
    {
        public string? Price { get; set; }
    }

    public class PageInfo
    {
        public bool HasNextPage { get; set; }
    }
}
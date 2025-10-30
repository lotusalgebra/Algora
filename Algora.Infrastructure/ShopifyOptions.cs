using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Infrastructure
{
    public record ShopifyOptions
    {
        public string ApiKey { get; init; } = string.Empty;
        public string ApiSecret { get; init; } = string.Empty;
        public string Scopes { get; init; } = string.Empty;
        public string AppUrl { get; init; } = string.Empty;
        public bool Embedded { get; init; } = true;
    }
}

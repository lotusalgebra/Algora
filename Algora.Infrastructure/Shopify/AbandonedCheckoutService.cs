using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ShopifySharp;
using ShopifySharp.Filters;

namespace Algora.Infrastructure.Shopify
{
    // Local models for abandoned checkout data
    public class AbandonedCheckout
    {
        public long? Id { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? TotalPrice { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? AbandonedCheckoutUrl { get; set; }
        public string? RecoveryUrl { get; set; }
        public bool? BuyerAcceptsMarketing { get; set; }
        public string? Currency { get; set; }
        public List<AbandonedCheckoutLineItem> LineItems { get; set; } = new();
        public AbandonedCheckoutCustomer? Customer { get; set; }
        public AbandonedCheckoutAddress? ShippingAddress { get; set; }
    }

    public class AbandonedCheckoutLineItem
    {
        public string? Title { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
        public string? VariantTitle { get; set; }
        public string? Vendor { get; set; }
    }

    public class AbandonedCheckoutCustomer
    {
        public long? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class AbandonedCheckoutAddress
    {
        public string? Name { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? Country { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
    }

    /// <summary>
    /// Service to fetch abandoned checkouts from Shopify using the REST Admin API.
    /// </summary>
    public class AbandonedCheckoutService
    {
        private readonly string _shopDomain;
        private readonly string _accessToken;
        private readonly ILogger? _logger;

        public AbandonedCheckoutService(string shopDomain, string accessToken, ILogger? logger = null)
        {
            _shopDomain = shopDomain;
            _accessToken = accessToken;
            _logger = logger;
        }

        /// <summary>
        /// Lists abandoned checkouts from Shopify.
        /// </summary>
        public async Task<List<AbandonedCheckout>> ListAsync(AbandonedCheckoutListFilter filter)
        {
            try
            {
#pragma warning disable CS0618 // CheckoutService is obsolete but still functional
                var service = new CheckoutService(_shopDomain, _accessToken);
#pragma warning restore CS0618

                var shopifyFilter = new CheckoutListFilter
                {
                    Limit = filter.Limit,
                    CreatedAtMin = filter.CreatedAtMin,
                    CreatedAtMax = filter.CreatedAtMax,
                    UpdatedAtMin = filter.UpdatedAtMin,
                    UpdatedAtMax = filter.UpdatedAtMax,
                    Status = filter.Status ?? "open" // open = abandoned
                };

                _logger?.LogInformation("Fetching abandoned checkouts from Shopify: Domain={Domain}, Limit={Limit}, Status={Status}",
                    _shopDomain, filter.Limit, shopifyFilter.Status);

                var result = await service.ListAsync(shopifyFilter);
                var checkouts = result?.Items ?? Enumerable.Empty<Checkout>();

                _logger?.LogInformation("Retrieved {Count} abandoned checkouts from Shopify", checkouts.Count());

                return checkouts.Select(c => MapToAbandonedCheckout(c)).ToList();
            }
            catch (ShopifyException ex)
            {
                _logger?.LogError(ex, "Shopify API error fetching abandoned checkouts: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error fetching abandoned checkouts from Shopify");
                throw;
            }
        }

        /// <summary>
        /// Gets a single abandoned checkout by ID.
        /// </summary>
        public async Task<AbandonedCheckout?> GetAsync(long id)
        {
            try
            {
#pragma warning disable CS0618 // CheckoutService is obsolete but still functional
                var service = new CheckoutService(_shopDomain, _accessToken);
#pragma warning restore CS0618

                _logger?.LogInformation("Fetching abandoned checkout {Id} from Shopify", id);

                // CheckoutService doesn't have GetAsync by ID directly - we need to list and filter
                // or use a different approach. For now, list recent and find by ID.
                var filter = new CheckoutListFilter
                {
                    Limit = 250,
                    Status = "open"
                };

                var result = await service.ListAsync(filter);
                var checkout = result?.Items?.FirstOrDefault(c => c.Id == id);

                if (checkout == null)
                {
                    _logger?.LogWarning("Abandoned checkout {Id} not found", id);
                    return null;
                }

                return MapToAbandonedCheckout(checkout);
            }
            catch (ShopifyException ex)
            {
                _logger?.LogError(ex, "Shopify API error fetching abandoned checkout {Id}: {Message}", id, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error fetching abandoned checkout {Id} from Shopify", id);
                throw;
            }
        }

        /// <summary>
        /// Counts abandoned checkouts matching the filter.
        /// </summary>
        public async Task<int> CountAsync(AbandonedCheckoutListFilter? filter = null)
        {
            try
            {
#pragma warning disable CS0618 // CheckoutService is obsolete but still functional
                var service = new CheckoutService(_shopDomain, _accessToken);
#pragma warning restore CS0618

                var shopifyFilter = new CheckoutCountFilter
                {
                    CreatedAtMin = filter?.CreatedAtMin,
                    CreatedAtMax = filter?.CreatedAtMax,
                    UpdatedAtMin = filter?.UpdatedAtMin,
                    UpdatedAtMax = filter?.UpdatedAtMax,
                    Status = filter?.Status ?? "open"
                };

                return await service.CountAsync(shopifyFilter);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error counting abandoned checkouts");
                return 0;
            }
        }

        private static AbandonedCheckout MapToAbandonedCheckout(Checkout c)
        {
            return new AbandonedCheckout
            {
                Id = c.Id,
                Email = c.Email,
                Phone = c.Phone,
                TotalPrice = c.TotalPrice?.ToString(),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                AbandonedCheckoutUrl = c.AbandonedCheckoutUrl,
                RecoveryUrl = c.AbandonedCheckoutUrl, // Same URL for recovery
                BuyerAcceptsMarketing = c.BuyerAcceptsMarketing,
                Currency = c.Currency,
                Customer = c.Customer == null ? null : new AbandonedCheckoutCustomer
                {
                    Id = c.Customer.Id,
                    FirstName = c.Customer.FirstName,
                    LastName = c.Customer.LastName,
                    Email = c.Customer.Email,
                    Phone = c.Customer.Phone
                },
                ShippingAddress = c.ShippingAddress == null ? null : new AbandonedCheckoutAddress
                {
                    Name = c.ShippingAddress.Name,
                    Address1 = c.ShippingAddress.Address1,
                    Address2 = c.ShippingAddress.Address2,
                    City = c.ShippingAddress.City,
                    Province = c.ShippingAddress.Province,
                    Country = c.ShippingAddress.Country,
                    Zip = c.ShippingAddress.Zip,
                    Phone = c.ShippingAddress.Phone
                },
                LineItems = (c.LineItems ?? new List<CheckoutLineItem>()).Select(li => new AbandonedCheckoutLineItem
                {
                    Title = li.Title,
                    Quantity = li.Quantity,
                    Price = li.Price,
                    VariantTitle = li.VariantTitle,
                    Vendor = li.Vendor
                }).ToList()
            };
        }
    }
}

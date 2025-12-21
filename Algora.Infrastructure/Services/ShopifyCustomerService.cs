using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using ShopifySharp;

namespace Algora.Infrastructure.Shopify;

/// <summary>
/// Service that wraps Shopify Sharp's customer API and maps domain DTOs.
/// </summary>
public class ShopifyCustomerService : IShopifyCustomerService
{
    private readonly IShopContext _context;
    private readonly ILogger<ShopifyCustomerService> _logger;

    /// <summary>
    /// Create a new instance of <see cref="ShopifyCustomerService"/>.
    /// </summary>
    /// <param name="context">Shop context containing shop domain and access token.</param>
    /// <param name="logger">Logger instance.</param>
    public ShopifyCustomerService(IShopContext context, ILogger<ShopifyCustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new <see cref="CustomerService"/> for the configured shop context.
    /// </summary>
    /// <returns>A configured <see cref="CustomerService"/> instance.</returns>
    private CustomerService CreateService() =>
        new(_context.ShopDomain, _context.AccessToken);

    /// <summary>
    /// Retrieve a page of customers from Shopify.
    /// </summary>
    /// <param name="limit">Maximum number of customers to return (page size).</param>
    /// <returns>An enumerable of <see cref="CustomerDto"/> representing customers.</returns>
    public async Task<IEnumerable<CustomerDto>> GetAllAsync(int limit = 25)
    {
        var service = CreateService();

        var filter = new ShopifySharp.Filters.CustomerListFilter
        {
            Limit = limit,
            Fields = "id,first_name,last_name,email,verified_email,state,created_at"
        };

        var customers = await service.ListAsync(filter);

        var items = customers?.Items ?? Enumerable.Empty<Customer>();
        return items.Select(c => new CustomerDto
        {
            Id = c.Id ?? 0,
            FirstName = c.FirstName ?? "",
            LastName = c.LastName ?? "",
            Email = c.Email ?? "",
            VerifiedEmail = c.VerifiedEmail ?? false,
            State = c.State ?? "",
            CreatedAt = c.CreatedAt?.DateTime ?? DateTime.MinValue
        });
    }

    /// <summary>
    /// Get a single customer by numeric id.
    /// </summary>
    /// <param name="id">Shopify numeric customer id.</param>
    /// <returns>A <see cref="CustomerDto"/> if found; otherwise null.</returns>
    public async Task<CustomerDto?> GetByIdAsync(long id)
    {
        var service = CreateService();
        var c = await service.GetAsync(id);
        if (c == null) return null;

        return new CustomerDto
        {
            Id = c.Id ?? 0,
            FirstName = c.FirstName ?? "",
            LastName = c.LastName ?? "",
            Email = c.Email ?? "",
            VerifiedEmail = c.VerifiedEmail ?? false,
            State = c.State ?? "",
            CreatedAt = c.CreatedAt?.DateTime ?? DateTime.MinValue
        };
    }

    /// <summary>
    /// Create a new customer in Shopify.
    /// </summary>
    /// <param name="dto">Customer data to create.</param>
    /// <returns>The created <see cref="CustomerDto"/> or null if creation failed.</returns>
    public async Task<CustomerDto?> CreateAsync(CustomerDto dto)
    {
        var service = CreateService();

        var customer = new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            VerifiedEmail = dto.VerifiedEmail
        };

        var created = await service.CreateAsync(customer);

        return new CustomerDto
        {
            Id = created.Id ?? 0,
            FirstName = created.FirstName ?? "",
            LastName = created.LastName ?? "",
            Email = created.Email ?? "",
            VerifiedEmail = created.VerifiedEmail ?? false,
            State = created.State ?? "",
            CreatedAt = created.CreatedAt?.DateTime ?? DateTime.Now
        };
    }

    /// <summary>
    /// Update an existing Shopify customer.
    /// </summary>
    /// <param name="id">Numeric id of the customer to update.</param>
    /// <param name="dto">Customer data to apply to the existing customer.</param>
    /// <returns>The updated <see cref="CustomerDto"/> or null if update failed.</returns>
    public async Task<CustomerDto?> UpdateAsync(long id, CustomerDto dto)
    {
        var service = CreateService();

        var updated = await service.UpdateAsync(id, new Customer
        {
            Id = id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email
        });

        return new CustomerDto
        {
            Id = updated.Id ?? 0,
            FirstName = updated.FirstName ?? "",
            LastName = updated.LastName ?? "",
            Email = updated.Email ?? "",
            VerifiedEmail = updated.VerifiedEmail ?? false,
            State = updated.State ?? "",
            CreatedAt = updated.CreatedAt?.DateTime ?? DateTime.Now
        };
    }

    /// <summary>
    /// Delete a customer by numeric id.
    /// </summary>
    /// <param name="id">Numeric id of the customer to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteAsync(long id)
    {
        var service = CreateService();
        await service.DeleteAsync(id);
        _logger.LogInformation("Deleted customer {Id} successfully.", id);
    }
}

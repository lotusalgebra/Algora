using Algora.Application.DTOs.Admin;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Manages client/shop information for admin purposes.
    /// Provides client listing, filtering, and plan management.
    /// </summary>
    public interface IClientService
    {
        /// <summary>
        /// Gets a paginated list of clients with optional filtering.
        /// </summary>
        Task<ClientListResultDto> GetClientsAsync(ClientFilterDto filter);

        /// <summary>
        /// Gets all clients without pagination.
        /// </summary>
        Task<IEnumerable<ClientDto>> GetAllClientsAsync(bool activeOnly = false);

        /// <summary>
        /// Gets detailed information about a specific client.
        /// </summary>
        Task<ClientDetailDto?> GetClientDetailAsync(string shopDomain);

        /// <summary>
        /// Gets a client by their shop ID.
        /// </summary>
        Task<ClientDto?> GetClientByIdAsync(Guid shopId);

        /// <summary>
        /// Updates a client's plan (admin override, bypasses billing).
        /// </summary>
        Task<bool> UpdateClientPlanAsync(UpdateClientPlanDto dto, string adminEmail);

        /// <summary>
        /// Activates or deactivates a client.
        /// </summary>
        Task<bool> SetClientActiveStatusAsync(string shopDomain, bool isActive);

        /// <summary>
        /// Gets client statistics for dashboard.
        /// </summary>
        Task<ClientStatsDto> GetClientStatsAsync();

        /// <summary>
        /// Gets all unique plan names currently in use by clients.
        /// </summary>
        Task<IEnumerable<string>> GetActivePlanNamesAsync();
    }

    /// <summary>
    /// Statistics about clients for admin dashboard.
    /// </summary>
    public record ClientStatsDto
    {
        public int TotalClients { get; init; }
        public int ActiveClients { get; init; }
        public int InactiveClients { get; init; }
        public int TrialClients { get; init; }
        public int PaidClients { get; init; }
        public Dictionary<string, int> ClientsByPlan { get; init; } = new();
    }
}

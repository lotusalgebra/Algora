using Algora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Algora.Infrastructure.Data
{
    /// <summary>
    /// EF Core database context for application persistence.
    /// Exposes DbSet properties for domain entities and configures model mappings.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="AppDbContext"/>.
        /// </summary>
        /// <param name="options">The options used by a DbContext.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Shops table containing installed shops and their offline tokens.
        /// </summary>
        public DbSet<Shop> Shops { get; set; } = null!;

        /// <summary>
        /// Webhook log entries captured for auditing and replay.
        /// Contains raw payloads and metadata for incoming webhooks.
        /// </summary>
        public DbSet<WebhookLog> WebhookLogs { get; set; } = null!;

        /// <summary>
        /// Licenses table that stores licensing and billing state per shop.
        /// </summary>
        public DbSet<License> Licenses { get; set; } = null!;

        /// <summary>
        /// Configure entity mappings and default values.
        /// Use this method to centralize model configuration for EF Core.
        /// </summary>
        /// <param name="modelBuilder">The builder used to construct the model for the context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<License>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.PlanName).HasMaxLength(100);
                b.Property(x => x.ChargeId).HasMaxLength(200);
                b.Property(x => x.Status).HasMaxLength(50);
                b.Property(x => x.StartDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Add other entity configuration if needed
        }
    }
}
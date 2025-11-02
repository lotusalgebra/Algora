using Algora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Algora.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // EF Core requires public properties (get; set;) for DbSet discovery.
        public DbSet<Shop> Shops { get; set; } = null!;
        public DbSet<WebhookLog> WebhookLogs { get; set; } = null!;
        public DbSet<License> Licenses { get; set; } = null!;

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
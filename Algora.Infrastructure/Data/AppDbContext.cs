using Algora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<WebhookLog> WebhookLogs => Set<WebhookLog>();
}

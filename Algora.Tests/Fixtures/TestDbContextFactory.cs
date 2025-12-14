using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Algora.Tests.Fixtures;

/// <summary>
/// Factory for creating in-memory database contexts for testing.
/// </summary>
public static class TestDbContextFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static async Task<AppDbContext> CreateWithSeedDataAsync(string? dbName = null)
    {
        var context = Create(dbName);
        await SeedTestDataAsync(context);
        return context;
    }

    private static async Task SeedTestDataAsync(AppDbContext context)
    {
        // Add test subscribers
        context.EmailSubscribers.AddRange(
            new EmailSubscriber
            {
                ShopDomain = "test-shop.myshopify.com",
                Email = "subscriber1@test.com",
                FirstName = "John",
                LastName = "Doe",
                Status = "subscribed",
                Source = "manual",
                EmailOptIn = true
            },
            new EmailSubscriber
            {
                ShopDomain = "test-shop.myshopify.com",
                Email = "subscriber2@test.com",
                FirstName = "Jane",
                LastName = "Smith",
                Status = "subscribed",
                Source = "checkout",
                EmailOptIn = true
            },
            new EmailSubscriber
            {
                ShopDomain = "test-shop.myshopify.com",
                Email = "unsubscribed@test.com",
                FirstName = "Bob",
                LastName = "Wilson",
                Status = "unsubscribed",
                Source = "manual",
                EmailOptIn = false,
                UnsubscribedAt = DateTime.UtcNow.AddDays(-1)
            }
        );

        // Add test email lists
        context.EmailLists.AddRange(
            new EmailList
            {
                ShopDomain = "test-shop.myshopify.com",
                Name = "Newsletter",
                Description = "Main newsletter list",
                IsDefault = true,
                IsActive = true,
                SubscriberCount = 2
            },
            new EmailList
            {
                ShopDomain = "test-shop.myshopify.com",
                Name = "Promotions",
                Description = "Promotional emails",
                IsDefault = false,
                IsActive = true,
                SubscriberCount = 0
            }
        );

        // Add test segments
        context.CustomerSegments.Add(new CustomerSegment
        {
            ShopDomain = "test-shop.myshopify.com",
            Name = "VIP Customers",
            Description = "High-value customers",
            SegmentType = "dynamic",
            IsActive = true,
            MemberCount = 10
        });

        // Add test campaigns
        context.EmailCampaigns.Add(new EmailCampaign
        {
            ShopDomain = "test-shop.myshopify.com",
            Name = "Summer Sale",
            Subject = "Don't miss our summer sale!",
            Body = "<h1>Summer Sale</h1>",
            Status = "draft",
            CampaignType = "regular"
        });

        // Add test automation with steps
        var automation = new EmailAutomation
        {
            ShopDomain = "test-shop.myshopify.com",
            Name = "Welcome Series",
            Description = "Welcome new subscribers",
            TriggerType = "welcome",
            IsActive = true
        };

        await context.SaveChangesAsync();

        // Add step separately after automation is saved to get the ID
        var savedAutomation = await context.EmailAutomations.FirstAsync(a => a.Name == "Welcome Series");
        context.EmailAutomationSteps.Add(new EmailAutomationStep
        {
            AutomationId = savedAutomation.Id,
            StepOrder = 1,
            StepType = "email",
            Subject = "Welcome!",
            Body = "Welcome to our store",
            DelayMinutes = 0,
            IsActive = true
        });

        await context.SaveChangesAsync();
    }
}
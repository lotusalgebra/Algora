using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Algora.Infrastructure.Services.Communication;
using Algora.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Algora.Tests.Services;

public class EmailMarketingServiceTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private IEmailMarketingService _service = null!;
    private readonly Mock<ILogger<EmailMarketingService>> _loggerMock = new();
    private const string TestShopDomain = "test-shop.myshopify.com";

    public async Task InitializeAsync()
    {
        _context = await TestDbContextFactory.CreateWithSeedDataAsync();
        _service = new EmailMarketingService(_context, _loggerMock.Object);
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    #region Subscriber Tests

    [Fact]
    public async Task GetSubscriberAsync_ExistingEmail_ReturnsSubscriber()
    {
        // Act
        var result = await _service.GetSubscriberAsync(TestShopDomain, "subscriber1@test.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("subscriber1@test.com");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Status.Should().Be("subscribed");
    }

    [Fact]
    public async Task GetSubscriberAsync_NonExistentEmail_ReturnsNull()
    {
        // Act
        var result = await _service.GetSubscriberAsync(TestShopDomain, "nonexistent@test.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSubscriberAsync_CaseInsensitiveEmail_ReturnsSubscriber()
    {
        // Act
        var result = await _service.GetSubscriberAsync(TestShopDomain, "SUBSCRIBER1@TEST.COM");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("subscriber1@test.com");
    }

    [Fact]
    public async Task GetSubscribersAsync_ReturnsAllSubscribers()
    {
        // Act
        var result = await _service.GetSubscribersAsync(TestShopDomain);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetSubscribersAsync_WithStatusFilter_ReturnsFilteredSubscribers()
    {
        // Act
        var result = await _service.GetSubscribersAsync(TestShopDomain, status: "subscribed");

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(s => s.Status.Should().Be("subscribed"));
    }

    [Fact]
    public async Task GetSubscribersAsync_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var result = await _service.GetSubscribersAsync(TestShopDomain, page: 1, pageSize: 2);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSubscriberAsync_ValidDto_CreatesAndReturnsSubscriber()
    {
        // Arrange
        var dto = new CreateEmailSubscriberDto
        {
            Email = "newsubscriber@test.com",
            FirstName = "New",
            LastName = "Subscriber",
            Source = "api",
            EmailOptIn = true
        };

        // Act
        var result = await _service.CreateSubscriberAsync(TestShopDomain, dto);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("newsubscriber@test.com");
        result.FirstName.Should().Be("New");
        result.Status.Should().Be("subscribed");
        result.Source.Should().Be("api");

        // Verify persisted
        var persisted = await _service.GetSubscriberAsync(TestShopDomain, "newsubscriber@test.com");
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSubscriberAsync_NormalizesEmailToLowercase()
    {
        // Arrange
        var dto = new CreateEmailSubscriberDto { Email = "UPPERCASE@TEST.COM" };

        // Act
        var result = await _service.CreateSubscriberAsync(TestShopDomain, dto);

        // Assert
        result.Email.Should().Be("uppercase@test.com");
    }

    [Fact]
    public async Task UpdateSubscriberAsync_ValidUpdate_UpdatesAndReturnsSubscriber()
    {
        // Arrange
        var subscriber = await _context.EmailSubscribers.FirstAsync(s => s.Email == "subscriber1@test.com");
        var dto = new UpdateEmailSubscriberDto
        {
            FirstName = "UpdatedFirst",
            LastName = "UpdatedLast",
            Phone = "+1234567890"
        };

        // Act
        var result = await _service.UpdateSubscriberAsync(subscriber.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("UpdatedFirst");
        result.LastName.Should().Be("UpdatedLast");
        result.Phone.Should().Be("+1234567890");
    }

    [Fact]
    public async Task UpdateSubscriberAsync_NonExistentId_ThrowsException()
    {
        // Arrange
        var dto = new UpdateEmailSubscriberDto { FirstName = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateSubscriberAsync(99999, dto));
    }

    [Fact]
    public async Task UnsubscribeAsync_ExistingSubscriber_ReturnsTrue()
    {
        // Act
        var result = await _service.UnsubscribeAsync(TestShopDomain, "subscriber1@test.com", "No longer interested");

        // Assert
        result.Should().BeTrue();

        var subscriber = await _service.GetSubscriberAsync(TestShopDomain, "subscriber1@test.com");
        subscriber!.Status.Should().Be("unsubscribed");
        subscriber.UnsubscribedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UnsubscribeAsync_NonExistentSubscriber_ReturnsFalse()
    {
        // Act
        var result = await _service.UnsubscribeAsync(TestShopDomain, "nonexistent@test.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ImportSubscribersAsync_NewSubscribers_ImportsAll()
    {
        // Arrange
        var subscribers = new List<CreateEmailSubscriberDto>
        {
            new() { Email = "import1@test.com", FirstName = "Import1" },
            new() { Email = "import2@test.com", FirstName = "Import2" },
            new() { Email = "import3@test.com", FirstName = "Import3" }
        };

        // Act
        var count = await _service.ImportSubscribersAsync(TestShopDomain, subscribers);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task ImportSubscribersAsync_DuplicateEmails_SkipsDuplicates()
    {
        // Arrange
        var subscribers = new List<CreateEmailSubscriberDto>
        {
            new() { Email = "subscriber1@test.com" }, // existing
            new() { Email = "newimport@test.com" }    // new
        };

        // Act
        var count = await _service.ImportSubscribersAsync(TestShopDomain, subscribers);

        // Assert
        count.Should().Be(1);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task GetListsAsync_ReturnsAllLists()
    {
        // Act
        var result = await _service.GetListsAsync(TestShopDomain);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetListAsync_ExistingId_ReturnsList()
    {
        // Arrange
        var list = await _context.EmailLists.FirstAsync(l => l.Name == "Newsletter");

        // Act
        var result = await _service.GetListAsync(list.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Newsletter");
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task CreateListAsync_ValidDto_CreatesAndReturnsList()
    {
        // Arrange
        var dto = new CreateEmailListDto
        {
            Name = "New List",
            Description = "A new test list",
            IsDefault = false,
            DoubleOptIn = true
        };

        // Act
        var result = await _service.CreateListAsync(TestShopDomain, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New List");
        result.DoubleOptIn.Should().BeTrue();
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateListAsync_ValidUpdate_UpdatesAndReturnsList()
    {
        // Arrange
        var list = await _context.EmailLists.FirstAsync(l => l.Name == "Newsletter");
        var dto = new UpdateEmailListDto
        {
            Name = "Updated Newsletter",
            Description = "Updated description"
        };

        // Act
        var result = await _service.UpdateListAsync(list.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Newsletter");
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteListAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        var list = await _context.EmailLists.FirstAsync(l => l.Name == "Promotions");

        // Act
        var result = await _service.DeleteListAsync(list.Id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _service.GetListAsync(list.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task AddSubscriberToListAsync_ValidIds_ReturnsTrue()
    {
        // Arrange
        var list = await _context.EmailLists.FirstAsync(l => l.Name == "Promotions");
        var subscriber = await _context.EmailSubscribers.FirstAsync(s => s.Email == "subscriber1@test.com");

        // Act
        var result = await _service.AddSubscriberToListAsync(list.Id, subscriber.Id);

        // Assert
        result.Should().BeTrue();

        var updatedList = await _service.GetListAsync(list.Id);
        updatedList!.SubscriberCount.Should().Be(1);
    }

    [Fact]
    public async Task AddSubscriberToListAsync_AlreadyInList_ReturnsTrue()
    {
        // Arrange
        var list = await _context.EmailLists.FirstAsync(l => l.Name == "Promotions");
        var subscriber = await _context.EmailSubscribers.FirstAsync(s => s.Email == "subscriber1@test.com");
        await _service.AddSubscriberToListAsync(list.Id, subscriber.Id);

        // Act - try to add again
        var result = await _service.AddSubscriberToListAsync(list.Id, subscriber.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveSubscriberFromListAsync_ExistingEntry_ReturnsTrue()
    {
        // Arrange
        var list = await _context.EmailLists.FirstAsync(l => l.Name == "Promotions");
        var subscriber = await _context.EmailSubscribers.FirstAsync(s => s.Email == "subscriber1@test.com");
        await _service.AddSubscriberToListAsync(list.Id, subscriber.Id);

        // Act
        var result = await _service.RemoveSubscriberFromListAsync(list.Id, subscriber.Id);

        // Assert
        result.Should().BeTrue();

        var updatedList = await _service.GetListAsync(list.Id);
        updatedList!.SubscriberCount.Should().Be(0);
    }

    #endregion

    #region Segment Tests

    [Fact]
    public async Task GetSegmentsAsync_ReturnsAllSegments()
    {
        // Act
        var result = await _service.GetSegmentsAsync(TestShopDomain);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateSegmentAsync_ValidDto_CreatesAndReturnsSegment()
    {
        // Arrange
        var dto = new CreateCustomerSegmentDto
        {
            Name = "New Segment",
            Description = "Test segment",
            SegmentType = "static"
        };

        // Act
        var result = await _service.CreateSegmentAsync(TestShopDomain, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Segment");
        result.SegmentType.Should().Be("static");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSegmentAsync_ValidUpdate_UpdatesSegment()
    {
        // Arrange
        var segment = await _context.CustomerSegments.FirstAsync(s => s.Name == "VIP Customers");
        var dto = new UpdateCustomerSegmentDto
        {
            Name = "Updated VIP",
            Description = "Updated description"
        };

        // Act
        var result = await _service.UpdateSegmentAsync(segment.Id, dto);

        // Assert
        result.Name.Should().Be("Updated VIP");
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteSegmentAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        var segment = await _context.CustomerSegments.FirstAsync(s => s.Name == "VIP Customers");

        // Act
        var result = await _service.DeleteSegmentAsync(segment.Id);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Campaign Tests

    [Fact]
    public async Task GetCampaignAsync_ExistingId_ReturnsCampaign()
    {
        // Arrange
        var campaign = await _context.EmailCampaigns.FirstAsync(c => c.Name == "Summer Sale");

        // Act
        var result = await _service.GetCampaignAsync(campaign.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Summer Sale");
        result.Status.Should().Be("draft");
    }

    [Fact]
    public async Task GetCampaignsAsync_ReturnsAllCampaigns()
    {
        // Act
        var result = await _service.GetCampaignsAsync(TestShopDomain);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateCampaignAsync_ValidDto_CreatesAndReturnsCampaign()
    {
        // Arrange
        var dto = new CreateEmailCampaignDto
        {
            Name = "New Campaign",
            Subject = "Test Subject",
            Body = "<p>Test body</p>",
            CampaignType = "regular"
        };

        // Act
        var result = await _service.CreateCampaignAsync(TestShopDomain, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Campaign");
        result.Status.Should().Be("draft");
    }

    [Fact]
    public async Task UpdateCampaignAsync_DraftCampaign_UpdatesSuccessfully()
    {
        // Arrange
        var campaign = await _context.EmailCampaigns.FirstAsync(c => c.Name == "Summer Sale");
        var dto = new UpdateEmailCampaignDto
        {
            Name = "Updated Campaign",
            Subject = "Updated Subject"
        };

        // Act
        var result = await _service.UpdateCampaignAsync(campaign.Id, dto);

        // Assert
        result.Name.Should().Be("Updated Campaign");
        result.Subject.Should().Be("Updated Subject");
    }

    [Fact]
    public async Task ScheduleCampaignAsync_ValidCampaign_SetsScheduledStatus()
    {
        // Arrange
        var campaign = await _context.EmailCampaigns.FirstAsync(c => c.Name == "Summer Sale");
        var scheduledTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = await _service.ScheduleCampaignAsync(campaign.Id, scheduledTime);

        // Assert
        result.Should().BeTrue();

        var updated = await _service.GetCampaignAsync(campaign.Id);
        updated!.Status.Should().Be("scheduled");
        updated.ScheduledAt.Should().BeCloseTo(scheduledTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SendCampaignAsync_ValidCampaign_SetsSendingStatus()
    {
        // Arrange
        var campaign = await _context.EmailCampaigns.FirstAsync(c => c.Name == "Summer Sale");

        // Act
        var result = await _service.SendCampaignAsync(campaign.Id);

        // Assert
        result.Should().BeTrue();

        var updated = await _service.GetCampaignAsync(campaign.Id);
        updated!.Status.Should().Be("sending");
    }

    [Fact]
    public async Task PauseCampaignAsync_ValidCampaign_SetsPausedStatus()
    {
        // Arrange
        var campaign = await _context.EmailCampaigns.FirstAsync(c => c.Name == "Summer Sale");

        // Act
        var result = await _service.PauseCampaignAsync(campaign.Id);

        // Assert
        result.Should().BeTrue();

        var updated = await _service.GetCampaignAsync(campaign.Id);
        updated!.Status.Should().Be("paused");
    }

    [Fact]
    public async Task CancelCampaignAsync_ValidCampaign_SetsCancelledStatus()
    {
        // Arrange
        var campaign = await _context.EmailCampaigns.FirstAsync(c => c.Name == "Summer Sale");

        // Act
        var result = await _service.CancelCampaignAsync(campaign.Id);

        // Assert
        result.Should().BeTrue();

        var updated = await _service.GetCampaignAsync(campaign.Id);
        updated!.Status.Should().Be("cancelled");
    }

    [Fact]
    public async Task GetCampaignStatsAsync_ValidCampaign_ReturnsStats()
    {
        // Arrange
        var campaign = await _context.EmailCampaigns.FirstAsync(c => c.Name == "Summer Sale");

        // Act
        var result = await _service.GetCampaignStatsAsync(campaign.Id);

        // Assert
        result.Should().NotBeNull();
        result.CampaignId.Should().Be(campaign.Id);
    }

    [Fact]
    public async Task DeleteCampaignAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        var campaign = await _context.EmailCampaigns.FirstAsync(c => c.Name == "Summer Sale");
        var campaignId = campaign.Id;

        // Act
        var result = await _service.DeleteCampaignAsync(campaignId);

        // Assert
        result.Should().BeTrue();

        var deleted = await _service.GetCampaignAsync(campaignId);
        deleted.Should().BeNull();
    }

    #endregion

    #region Automation Tests

    [Fact]
    public async Task GetAutomationAsync_ExistingId_ReturnsAutomation()
    {
        // Arrange
        var automation = await _context.EmailAutomations.FirstAsync(a => a.Name == "Welcome Series");

        // Act
        var result = await _service.GetAutomationAsync(automation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Welcome Series");
        result.TriggerType.Should().Be("welcome");
        result.Steps.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAutomationsAsync_ReturnsAllAutomations()
    {
        // Act
        var result = await _service.GetAutomationsAsync(TestShopDomain);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAutomationAsync_WithSteps_CreatesAutomationAndSteps()
    {
        // Arrange
        var dto = new CreateEmailAutomationDto
        {
            Name = "Abandoned Cart",
            Description = "Recover abandoned carts",
            TriggerType = "abandoned_cart",
            Steps =
            [
                new CreateEmailAutomationStepDto
                {
                    StepOrder = 1,
                    StepType = "email",
                    Subject = "Did you forget something?",
                    Body = "Complete your purchase",
                    DelayMinutes = 60
                },
                new CreateEmailAutomationStepDto
                {
                    StepOrder = 2,
                    StepType = "email",
                    Subject = "Last chance!",
                    Body = "Your cart is waiting",
                    DelayMinutes = 1440
                }
            ]
        };

        // Act
        var result = await _service.CreateAutomationAsync(TestShopDomain, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Abandoned Cart");
        result.IsActive.Should().BeFalse(); // New automations start inactive
        result.Steps.Should().HaveCount(2);
    }

    [Fact]
    public async Task ActivateAutomationAsync_InactiveAutomation_ReturnsTrue()
    {
        // Arrange
        var automation = await _context.EmailAutomations.FirstAsync(a => a.Name == "Welcome Series");
        await _service.DeactivateAutomationAsync(automation.Id);

        // Act
        var result = await _service.ActivateAutomationAsync(automation.Id);

        // Assert
        result.Should().BeTrue();

        var updated = await _service.GetAutomationAsync(automation.Id);
        updated!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAutomationAsync_ActiveAutomation_ReturnsTrue()
    {
        // Arrange
        var automation = await _context.EmailAutomations.FirstAsync(a => a.Name == "Welcome Series");

        // Act
        var result = await _service.DeactivateAutomationAsync(automation.Id);

        // Assert
        result.Should().BeTrue();

        var updated = await _service.GetAutomationAsync(automation.Id);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task EnrollInAutomationAsync_ActiveAutomation_ReturnsTrue()
    {
        // Arrange
        var automation = await _context.EmailAutomations.FirstAsync(a => a.Name == "Welcome Series");

        // Act
        var result = await _service.EnrollInAutomationAsync(automation.Id, "test@example.com", customerId: null);

        // Assert
        result.Should().BeTrue();

        var updated = await _service.GetAutomationAsync(automation.Id);
        updated!.TotalEnrolled.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EnrollInAutomationAsync_InactiveAutomation_ReturnsFalse()
    {
        // Arrange
        var automation = await _context.EmailAutomations.FirstAsync(a => a.Name == "Welcome Series");
        await _service.DeactivateAutomationAsync(automation.Id);

        // Act
        var result = await _service.EnrollInAutomationAsync(automation.Id, "test@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAutomationAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        var automation = await _context.EmailAutomations.FirstAsync(a => a.Name == "Welcome Series");
        var automationId = automation.Id;

        // Act
        var result = await _service.DeleteAutomationAsync(automationId);

        // Assert
        result.Should().BeTrue();

        var deleted = await _service.GetAutomationAsync(automationId);
        deleted.Should().BeNull();
    }

    #endregion
}
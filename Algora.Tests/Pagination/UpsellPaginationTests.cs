using Algora.Application.DTOs.Common;
using Algora.Application.DTOs.Inventory;
using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Algora.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OffersPage = Algora.Web.Pages.Upsell.Offers.IndexModel;
using ExperimentsPage = Algora.Web.Pages.Upsell.Experiments.IndexModel;
using AffinitiesPage = Algora.Web.Pages.Upsell.Affinities.IndexModel;

namespace Algora.Tests.Pagination;

public class UpsellOffersPaginationTests
{
    private readonly Mock<IUpsellRecommendationService> _recommendationServiceMock;
    private readonly Mock<IShopContext> _shopContextMock;
    private readonly Mock<ILogger<OffersPage>> _loggerMock;
    private readonly PaginatedResult<UpsellOfferDto> _testOffers;

    public UpsellOffersPaginationTests()
    {
        _recommendationServiceMock = new Mock<IUpsellRecommendationService>();
        _shopContextMock = new Mock<IShopContext>();
        _loggerMock = new Mock<ILogger<OffersPage>>();
        _testOffers = TestDataBuilders.CreatePaginatedOffers(25);

        _shopContextMock.Setup(x => x.ShopDomain).Returns(TestDataBuilders.TestShopDomain);
        _recommendationServiceMock
            .Setup(x => x.GetOffersAsync(TestDataBuilders.TestShopDomain, null, 1, 500))
            .ReturnsAsync(_testOffers);
    }

    [Fact]
    public async Task OnGetDataAsync_ReturnsCorrectPageSize()
    {
        // Arrange
        var pageModel = new OffersPage(
            _recommendationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 10);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsTotal.Should().Be(25);
        response.Data.Should().HaveCount(10);
    }

    [Fact]
    public async Task OnGetDataAsync_PaginatesCorrectly()
    {
        // Arrange
        var pageModel = new OffersPage(
            _recommendationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Get second page
        var result = await pageModel.OnGetDataAsync(draw: 2, start: 10, length: 10);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().HaveCount(10);
        response.Draw.Should().Be(2);
    }

    [Fact]
    public async Task OnGetDataAsync_FiltersOnSearch()
    {
        // Arrange
        var pageModel = new OffersPage(
            _recommendationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Search for "Recommended Product 1"
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, search: "Recommended Product 1");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsFiltered.Should().BeLessThan(response.RecordsTotal);
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByProductTitle()
    {
        // Arrange
        var pageModel = new OffersPage(
            _recommendationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by product title (column 0)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 0, sortDirection: "asc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByImpressions()
    {
        // Arrange
        var pageModel = new OffersPage(
            _recommendationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by impressions (column 3)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 3, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByConversions()
    {
        // Arrange
        var pageModel = new OffersPage(
            _recommendationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by conversions (column 5)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 5, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByRevenue()
    {
        // Arrange
        var pageModel = new OffersPage(
            _recommendationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by revenue (column 6)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 6, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_HandlesException()
    {
        // Arrange
        _recommendationServiceMock
            .Setup(x => x.GetOffersAsync(TestDataBuilders.TestShopDomain, null, 1, 500))
            .ThrowsAsync(new Exception("Test exception"));

        var pageModel = new OffersPage(
            _recommendationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Error.Should().NotBeNullOrEmpty();
    }
}

public class UpsellExperimentsPaginationTests
{
    private readonly Mock<IUpsellExperimentService> _experimentServiceMock;
    private readonly Mock<IShopContext> _shopContextMock;
    private readonly Mock<ILogger<ExperimentsPage>> _loggerMock;
    private readonly PaginatedResult<UpsellExperimentDto> _testExperiments;

    public UpsellExperimentsPaginationTests()
    {
        _experimentServiceMock = new Mock<IUpsellExperimentService>();
        _shopContextMock = new Mock<IShopContext>();
        _loggerMock = new Mock<ILogger<ExperimentsPage>>();
        _testExperiments = TestDataBuilders.CreatePaginatedExperiments(15);

        _shopContextMock.Setup(x => x.ShopDomain).Returns(TestDataBuilders.TestShopDomain);
        _experimentServiceMock
            .Setup(x => x.GetExperimentsAsync(TestDataBuilders.TestShopDomain, null, 1, 500))
            .ReturnsAsync(_testExperiments);
    }

    [Fact]
    public async Task OnGetDataAsync_ReturnsCorrectPageSize()
    {
        // Arrange
        var pageModel = new ExperimentsPage(
            _experimentServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 10);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsTotal.Should().Be(15);
        response.Data.Should().HaveCount(10);
    }

    [Fact]
    public async Task OnGetDataAsync_FiltersOnSearch()
    {
        // Arrange
        var pageModel = new ExperimentsPage(
            _experimentServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Search for "Experiment 01"
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, search: "Experiment 01");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsFiltered.Should().BeLessThan(response.RecordsTotal);
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByName()
    {
        // Arrange
        var pageModel = new ExperimentsPage(
            _experimentServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by name (column 0)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 0, sortDirection: "asc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByImpressions()
    {
        // Arrange
        var pageModel = new ExperimentsPage(
            _experimentServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by impressions (column 2)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 2, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByConversions()
    {
        // Arrange
        var pageModel = new ExperimentsPage(
            _experimentServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by conversions (column 3)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 3, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_HandlesException()
    {
        // Arrange
        _experimentServiceMock
            .Setup(x => x.GetExperimentsAsync(TestDataBuilders.TestShopDomain, null, 1, 500))
            .ThrowsAsync(new Exception("Test exception"));

        var pageModel = new ExperimentsPage(
            _experimentServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Error.Should().NotBeNullOrEmpty();
    }
}

public class ProductAffinitiesPaginationTests
{
    private readonly Mock<IProductAffinityService> _affinityServiceMock;
    private readonly Mock<IShopContext> _shopContextMock;
    private readonly Mock<ILogger<AffinitiesPage>> _loggerMock;
    private readonly PaginatedResult<ProductAffinityDto> _testAffinities;

    public ProductAffinitiesPaginationTests()
    {
        _affinityServiceMock = new Mock<IProductAffinityService>();
        _shopContextMock = new Mock<IShopContext>();
        _loggerMock = new Mock<ILogger<AffinitiesPage>>();
        _testAffinities = TestDataBuilders.CreatePaginatedAffinities(60);

        _shopContextMock.Setup(x => x.ShopDomain).Returns(TestDataBuilders.TestShopDomain);
        _affinityServiceMock
            .Setup(x => x.GetAllAffinitiesAsync(TestDataBuilders.TestShopDomain, null, 1, 1000))
            .ReturnsAsync(_testAffinities);
        _affinityServiceMock
            .Setup(x => x.GetAffinitySummaryAsync(TestDataBuilders.TestShopDomain))
            .ReturnsAsync(new AffinitySummaryDto());
    }

    [Fact]
    public async Task OnGetDataAsync_ReturnsCorrectPageSize()
    {
        // Arrange
        var pageModel = new AffinitiesPage(
            _affinityServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsTotal.Should().Be(60);
        response.Data.Should().HaveCount(25);
    }

    [Fact]
    public async Task OnGetDataAsync_PaginatesCorrectly()
    {
        // Arrange
        var pageModel = new AffinitiesPage(
            _affinityServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Get third page
        var result = await pageModel.OnGetDataAsync(draw: 3, start: 50, length: 25);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().HaveCount(10); // Only 10 remaining
        response.Draw.Should().Be(3);
    }

    [Fact]
    public async Task OnGetDataAsync_FiltersOnSearch()
    {
        // Arrange
        var pageModel = new AffinitiesPage(
            _affinityServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Search for "Source Product 001"
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, search: "Source Product 001");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsFiltered.Should().BeLessThan(response.RecordsTotal);
    }

    [Fact]
    public async Task OnGetDataAsync_SortsBySourceProduct()
    {
        // Arrange
        var pageModel = new AffinitiesPage(
            _affinityServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by source product (column 0)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 0, sortDirection: "asc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByConfidence()
    {
        // Arrange
        var pageModel = new AffinitiesPage(
            _affinityServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by confidence (column 3)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 3, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByLift()
    {
        // Arrange
        var pageModel = new AffinitiesPage(
            _affinityServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by lift (column 4)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 4, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByCoOccurrences()
    {
        // Arrange
        var pageModel = new AffinitiesPage(
            _affinityServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by co-occurrences (column 5)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 5, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_HandlesException()
    {
        // Arrange
        _affinityServiceMock
            .Setup(x => x.GetAllAffinitiesAsync(TestDataBuilders.TestShopDomain, null, 1, 1000))
            .ThrowsAsync(new Exception("Test exception"));

        var pageModel = new AffinitiesPage(
            _affinityServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Error.Should().NotBeNullOrEmpty();
    }
}

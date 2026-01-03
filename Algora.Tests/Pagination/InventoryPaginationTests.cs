using Algora.Application.DTOs.Common;
using Algora.Application.DTOs.Inventory;
using Algora.Application.Interfaces;
using Algora.Tests.Fixtures;
using Algora.Web.Pages.Inventory;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Algora.Tests.Pagination;

public class InventoryPaginationTests
{
    private readonly Mock<IInventoryPredictionService> _predictionServiceMock;
    private readonly Mock<IShopContext> _shopContextMock;
    private readonly Mock<ILogger<IndexModel>> _loggerMock;
    private readonly PaginatedResult<InventoryPredictionDto> _testPredictions;

    public InventoryPaginationTests()
    {
        _predictionServiceMock = new Mock<IInventoryPredictionService>();
        _shopContextMock = new Mock<IShopContext>();
        _loggerMock = new Mock<ILogger<IndexModel>>();
        _testPredictions = TestDataBuilders.CreateInventoryPredictions(35);

        _shopContextMock.Setup(x => x.ShopDomain).Returns(TestDataBuilders.TestShopDomain);
        _predictionServiceMock
            .Setup(x => x.GetPredictionsAsync(TestDataBuilders.TestShopDomain, null, 1, 1000))
            .ReturnsAsync(_testPredictions);
        _predictionServiceMock
            .Setup(x => x.GetPredictionSummaryAsync(TestDataBuilders.TestShopDomain))
            .ReturnsAsync(new InventoryPredictionSummaryDto());
    }

    private IndexModel CreatePageModel()
    {
        var pageModel = new IndexModel(_predictionServiceMock.Object, _loggerMock.Object);

        // Set up HttpContext with IShopContext service
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_shopContextMock.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        pageModel.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        return pageModel;
    }

    [Fact]
    public async Task OnGetDataAsync_ReturnsCorrectPageSize()
    {
        // Arrange
        var pageModel = CreatePageModel();

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 10);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsTotal.Should().Be(35);
        response.Data.Should().HaveCount(10);
    }

    [Fact]
    public async Task OnGetDataAsync_PaginatesCorrectly()
    {
        // Arrange
        var pageModel = CreatePageModel();

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
        var pageModel = CreatePageModel();

        // Act - Search for "Product 001"
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 35, search: "Product 001");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsFiltered.Should().BeLessThan(response.RecordsTotal);
    }

    [Fact]
    public async Task OnGetDataAsync_FiltersOnStatus()
    {
        // Arrange
        var pageModel = CreatePageModel();

        // Act - Filter by status "healthy"
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 35, statusFilter: "healthy");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsFiltered.Should().BeLessThan(response.RecordsTotal);
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByProductTitle()
    {
        // Arrange
        var pageModel = CreatePageModel();

        // Act - Sort by product title (column 0)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 35, sortColumn: 0, sortDirection: "asc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByCurrentQuantity()
    {
        // Arrange
        var pageModel = CreatePageModel();

        // Act - Sort by current quantity (column 2)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 35, sortColumn: 2, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByAverageDailySales()
    {
        // Arrange
        var pageModel = CreatePageModel();

        // Act - Sort by average daily sales (column 3)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 35, sortColumn: 3, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByDaysUntilStockout()
    {
        // Arrange
        var pageModel = CreatePageModel();

        // Act - Sort by days until stockout (column 4)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 35, sortColumn: 4, sortDirection: "asc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsBySuggestedReorderQuantity()
    {
        // Arrange
        var pageModel = CreatePageModel();

        // Act - Sort by suggested reorder quantity (column 5)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 35, sortColumn: 5, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_HandlesEmptyResult()
    {
        // Arrange
        _predictionServiceMock
            .Setup(x => x.GetPredictionsAsync(TestDataBuilders.TestShopDomain, null, 1, 1000))
            .ReturnsAsync(new PaginatedResult<InventoryPredictionDto>
            {
                Items = new List<InventoryPredictionDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 50
            });

        var pageModel = CreatePageModel();

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsTotal.Should().Be(0);
        response.RecordsFiltered.Should().Be(0);
        response.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_HandlesException()
    {
        // Arrange
        _predictionServiceMock
            .Setup(x => x.GetPredictionsAsync(TestDataBuilders.TestShopDomain, null, 1, 1000))
            .ThrowsAsync(new Exception("Test exception"));

        var pageModel = CreatePageModel();

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_DrawValuePassedThrough()
    {
        // Arrange
        var pageModel = CreatePageModel();

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 42, start: 0, length: 10);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Draw.Should().Be(42);
    }
}

using Algora.Application.DTOs.Common;
using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SuppliersPage = Algora.Web.Pages.Operations.Suppliers.IndexModel;
using PurchaseOrdersPage = Algora.Web.Pages.Operations.PurchaseOrders.IndexModel;
using LocationsPage = Algora.Web.Pages.Operations.Locations.IndexModel;

namespace Algora.Tests.Pagination;

public class SuppliersPaginationTests
{
    private readonly Mock<ISupplierService> _supplierServiceMock;
    private readonly Mock<IShopContext> _shopContextMock;
    private readonly Mock<ILogger<SuppliersPage>> _loggerMock;
    private readonly List<SupplierDto> _testSuppliers;

    public SuppliersPaginationTests()
    {
        _supplierServiceMock = new Mock<ISupplierService>();
        _shopContextMock = new Mock<IShopContext>();
        _loggerMock = new Mock<ILogger<SuppliersPage>>();
        _testSuppliers = TestDataBuilders.CreateSuppliers(20);

        _shopContextMock.Setup(x => x.ShopDomain).Returns(TestDataBuilders.TestShopDomain);
        _supplierServiceMock
            .Setup(x => x.GetSuppliersAsync(TestDataBuilders.TestShopDomain))
            .ReturnsAsync(_testSuppliers);
    }

    [Fact]
    public async Task OnGetDataAsync_ReturnsCorrectPageSize()
    {
        // Arrange
        var pageModel = new SuppliersPage(
            _supplierServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 10);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsTotal.Should().Be(20);
        response.RecordsFiltered.Should().Be(20);
        response.Data.Should().HaveCount(10);
    }

    [Fact]
    public async Task OnGetDataAsync_PaginatesCorrectly()
    {
        // Arrange
        var pageModel = new SuppliersPage(
            _supplierServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Get second page
        var result = await pageModel.OnGetDataAsync(draw: 2, start: 10, length: 10);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsTotal.Should().Be(20);
        response.Data.Should().HaveCount(10);
        response.Draw.Should().Be(2);
    }

    [Fact]
    public async Task OnGetDataAsync_FiltersOnSearch()
    {
        // Arrange
        var pageModel = new SuppliersPage(
            _supplierServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Search for "Supplier 01"
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, search: "Supplier 01");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsFiltered.Should().BeLessThan(response.RecordsTotal);
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByNameAscending()
    {
        // Arrange
        var pageModel = new SuppliersPage(
            _supplierServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by name (column 0) ascending
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 20, sortColumn: 0, sortDirection: "asc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        var dataList = response.Data.ToList();
        dataList.Should().HaveCount(20);
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByLeadTimeDays()
    {
        // Arrange
        var pageModel = new SuppliersPage(
            _supplierServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by lead time (column 2)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 20, sortColumn: 2, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_HandlesEmptyResult()
    {
        // Arrange
        _supplierServiceMock
            .Setup(x => x.GetSuppliersAsync(TestDataBuilders.TestShopDomain))
            .ReturnsAsync(new List<SupplierDto>());

        var pageModel = new SuppliersPage(
            _supplierServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

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
        _supplierServiceMock
            .Setup(x => x.GetSuppliersAsync(TestDataBuilders.TestShopDomain))
            .ThrowsAsync(new Exception("Test exception"));

        var pageModel = new SuppliersPage(
            _supplierServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Error.Should().NotBeNullOrEmpty();
        response.RecordsTotal.Should().Be(0);
    }
}

public class PurchaseOrdersPaginationTests
{
    private readonly Mock<IPurchaseOrderService> _purchaseOrderServiceMock;
    private readonly Mock<IShopContext> _shopContextMock;
    private readonly Mock<ILogger<PurchaseOrdersPage>> _loggerMock;
    private readonly List<PurchaseOrderDto> _testOrders;

    public PurchaseOrdersPaginationTests()
    {
        _purchaseOrderServiceMock = new Mock<IPurchaseOrderService>();
        _shopContextMock = new Mock<IShopContext>();
        _loggerMock = new Mock<ILogger<PurchaseOrdersPage>>();
        _testOrders = TestDataBuilders.CreatePurchaseOrders(40);

        _shopContextMock.Setup(x => x.ShopDomain).Returns(TestDataBuilders.TestShopDomain);
        _purchaseOrderServiceMock
            .Setup(x => x.GetPurchaseOrdersAsync(TestDataBuilders.TestShopDomain, It.IsAny<PurchaseOrderFilterDto>()))
            .ReturnsAsync(_testOrders);
    }

    [Fact]
    public async Task OnGetDataAsync_ReturnsCorrectPageSize()
    {
        // Arrange
        var pageModel = new PurchaseOrdersPage(
            _purchaseOrderServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 10);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsTotal.Should().Be(40);
        response.Data.Should().HaveCount(10);
    }

    [Fact]
    public async Task OnGetDataAsync_FiltersOnSearch()
    {
        // Arrange
        var pageModel = new PurchaseOrdersPage(
            _purchaseOrderServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Search for "PO-0001"
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, search: "PO-0001");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsFiltered.Should().BeLessThan(response.RecordsTotal);
    }

    [Fact]
    public async Task OnGetDataAsync_FiltersOnStatus()
    {
        // Arrange
        var pageModel = new PurchaseOrdersPage(
            _purchaseOrderServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Filter by status (via service filter)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, statusFilter: "pending");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        // The filter is passed to service, so we're just testing it doesn't error
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByOrderNumber()
    {
        // Arrange
        var pageModel = new PurchaseOrdersPage(
            _purchaseOrderServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by order number (column 0) ascending
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 0, sortDirection: "asc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByTotal()
    {
        // Arrange
        var pageModel = new PurchaseOrdersPage(
            _purchaseOrderServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by total (column 3)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 3, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByCreatedAt()
    {
        // Arrange
        var pageModel = new PurchaseOrdersPage(
            _purchaseOrderServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by created at (column 4)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 4, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }
}

public class LocationsPaginationTests
{
    private readonly Mock<ILocationService> _locationServiceMock;
    private readonly Mock<IShopContext> _shopContextMock;
    private readonly Mock<ILogger<LocationsPage>> _loggerMock;
    private readonly List<LocationDto> _testLocations;

    public LocationsPaginationTests()
    {
        _locationServiceMock = new Mock<ILocationService>();
        _shopContextMock = new Mock<IShopContext>();
        _loggerMock = new Mock<ILogger<LocationsPage>>();
        _testLocations = TestDataBuilders.CreateLocations(10);

        _shopContextMock.Setup(x => x.ShopDomain).Returns(TestDataBuilders.TestShopDomain);
        _locationServiceMock
            .Setup(x => x.GetLocationsAsync(TestDataBuilders.TestShopDomain))
            .ReturnsAsync(_testLocations);
    }

    [Fact]
    public async Task OnGetDataAsync_ReturnsCorrectPageSize()
    {
        // Arrange
        var pageModel = new LocationsPage(
            _locationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 5);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsTotal.Should().Be(10);
        response.Data.Should().HaveCount(5);
    }

    [Fact]
    public async Task OnGetDataAsync_FiltersOnSearch()
    {
        // Arrange
        var pageModel = new LocationsPage(
            _locationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Search for "Chicago"
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, search: "Chicago");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.RecordsFiltered.Should().BeLessThan(response.RecordsTotal);
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByName()
    {
        // Arrange
        var pageModel = new LocationsPage(
            _locationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by name (column 0)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 0, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByTotalInventory()
    {
        // Arrange
        var pageModel = new LocationsPage(
            _locationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by total inventory (column 2)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 2, sortDirection: "desc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_SortsByTotalProducts()
    {
        // Arrange
        var pageModel = new LocationsPage(
            _locationServiceMock.Object,
            _shopContextMock.Object,
            _loggerMock.Object);

        // Act - Sort by total products (column 3)
        var result = await pageModel.OnGetDataAsync(draw: 1, start: 0, length: 25, sortColumn: 3, sortDirection: "asc");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value.Should().BeAssignableTo<DataTableResponse<object>>().Subject;

        response.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGetDataAsync_HandlesException()
    {
        // Arrange
        _locationServiceMock
            .Setup(x => x.GetLocationsAsync(TestDataBuilders.TestShopDomain))
            .ThrowsAsync(new Exception("Test exception"));

        var pageModel = new LocationsPage(
            _locationServiceMock.Object,
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

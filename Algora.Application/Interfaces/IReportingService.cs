using Algora.Application.DTOs.Reporting;

namespace Algora.Application.Interfaces;

public interface IReportingService
{
    // Dashboard
    Task<ReportingDashboardDto> GetDashboardAsync(string shopDomain, DateRangeRequest request);

    // Sales
    Task<SalesReportDto> GetSalesReportAsync(string shopDomain, DateRangeRequest request);
    Task<List<SalesByPeriodDto>> GetSalesByPeriodAsync(string shopDomain, DateRangeRequest request, string period);

    // Products
    Task<ProductReportSummaryDto> GetProductReportAsync(string shopDomain, DateRangeRequest request);
    Task<List<ProductPerformanceDto>> GetProductPerformanceListAsync(string shopDomain, DateRangeRequest request, int take = 50);

    // Customers
    Task<CustomerReportDto> GetCustomerReportAsync(string shopDomain, DateRangeRequest request);
    Task<List<TopCustomerDto>> GetTopCustomersAsync(string shopDomain, DateRangeRequest request, int take = 20);

    // Financial
    Task<FinancialReportDto> GetFinancialReportAsync(string shopDomain, DateRangeRequest request);

    // Advertising
    Task<AdvertisingReportDto> GetAdvertisingReportAsync(string shopDomain, DateRangeRequest request);
    Task<List<CampaignPerformanceDto>> GetCampaignPerformanceAsync(string shopDomain, DateRangeRequest request, string? platform = null);

    // Export
    Task<byte[]> ExportSalesReportAsync(string shopDomain, DateRangeRequest request, string format);
    Task<byte[]> ExportProductReportAsync(string shopDomain, DateRangeRequest request, string format);
    Task<byte[]> ExportCustomerReportAsync(string shopDomain, DateRangeRequest request, string format);
}

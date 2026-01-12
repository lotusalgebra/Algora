using System.Globalization;
using System.Text;
using Algora.Application.DTOs.Reporting;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Algora.Infrastructure.Services.Reporting;

public class ReportingService : IReportingService
{
    private readonly AppDbContext _context;

    public ReportingService(AppDbContext context)
    {
        _context = context;
    }

    // ============= Dashboard =============

    public async Task<ReportingDashboardDto> GetDashboardAsync(string shopDomain, DateRangeRequest request)
    {
        var previousPeriodDays = (request.EndDate - request.StartDate).Days;
        var previousStart = request.StartDate.AddDays(-previousPeriodDays);
        var previousEnd = request.StartDate.AddDays(-1);

        // Current period metrics
        var currentOrders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain && o.OrderDate >= request.StartDate && o.OrderDate <= request.EndDate)
            .ToListAsync();

        var previousOrders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain && o.OrderDate >= previousStart && o.OrderDate <= previousEnd)
            .ToListAsync();

        var totalRevenue = currentOrders.Sum(o => o.GrandTotal);
        var previousRevenue = previousOrders.Sum(o => o.GrandTotal);
        var revenueGrowth = previousRevenue > 0 ? (totalRevenue - previousRevenue) / previousRevenue * 100 : 0;

        var totalOrders = currentOrders.Count;
        var previousOrderCount = previousOrders.Count;
        var orderGrowth = previousOrderCount > 0 ? (int)((totalOrders - previousOrderCount) / (decimal)previousOrderCount * 100) : 0;

        var currentCustomerIds = currentOrders.Where(o => o.CustomerId.HasValue).Select(o => o.CustomerId!.Value).Distinct().Count();
        var previousCustomerIds = previousOrders.Where(o => o.CustomerId.HasValue).Select(o => o.CustomerId!.Value).Distinct().Count();
        var customerGrowth = previousCustomerIds > 0 ? (int)((currentCustomerIds - previousCustomerIds) / (decimal)previousCustomerIds * 100) : 0;

        // Ads spend
        var currentAdsSpend = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain && a.SpendDate >= request.StartDate && a.SpendDate <= request.EndDate)
            .SumAsync(a => a.Amount);
        var previousAdsSpend = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain && a.SpendDate >= previousStart && a.SpendDate <= previousEnd)
            .SumAsync(a => a.Amount);
        var adsSpendGrowth = previousAdsSpend > 0 ? (currentAdsSpend - previousAdsSpend) / previousAdsSpend * 100 : 0;

        var adsRevenue = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain && a.SpendDate >= request.StartDate && a.SpendDate <= request.EndDate)
            .SumAsync(a => a.Revenue ?? 0);
        var roas = currentAdsSpend > 0 ? adsRevenue / currentAdsSpend : 0;

        var aov = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        // Sparklines
        var revenueSparkline = currentOrders
            .GroupBy(o => o.OrderDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesDataPoint(g.Key, g.Key.ToString("MMM dd"), g.Sum(o => o.GrandTotal)))
            .ToList();

        var ordersSparkline = currentOrders
            .GroupBy(o => o.OrderDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesDataPoint(g.Key, g.Key.ToString("MMM dd"), g.Count()))
            .ToList();

        // Top products
        var topProducts = await GetTopProductsAsync(shopDomain, request, 5);

        // Top customers
        var topCustomers = await GetTopCustomersAsync(shopDomain, request, 5);

        // Calculate net profit margin (simplified)
        var refunds = await _context.Refunds
            .Include(r => r.Order)
            .Where(r => r.Order.ShopDomain == shopDomain && r.RefundedAt >= request.StartDate && r.RefundedAt <= request.EndDate)
            .SumAsync(r => r.Amount);
        var netRevenue = totalRevenue - refunds;
        var netProfitMargin = totalRevenue > 0 ? (netRevenue - currentAdsSpend) / totalRevenue * 100 : 0;

        return new ReportingDashboardDto(
            totalRevenue,
            revenueGrowth,
            totalOrders,
            orderGrowth,
            currentCustomerIds,
            customerGrowth,
            currentAdsSpend,
            adsSpendGrowth,
            aov,
            roas,
            netProfitMargin,
            revenueSparkline,
            ordersSparkline,
            topProducts,
            topCustomers
        );
    }

    // ============= Sales =============

    public async Task<SalesReportDto> GetSalesReportAsync(string shopDomain, DateRangeRequest request)
    {
        var previousPeriodDays = (request.EndDate - request.StartDate).Days;
        var previousStart = request.StartDate.AddDays(-previousPeriodDays);
        var previousEnd = request.StartDate.AddDays(-1);

        var orders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain && o.OrderDate >= request.StartDate && o.OrderDate <= request.EndDate)
            .ToListAsync();

        var previousOrders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain && o.OrderDate >= previousStart && o.OrderDate <= previousEnd)
            .ToListAsync();

        var totalRevenue = orders.Sum(o => o.GrandTotal);
        var totalSubtotal = orders.Sum(o => o.Subtotal);
        var totalTax = orders.Sum(o => o.TaxTotal);
        var totalShipping = orders.Sum(o => o.ShippingTotal);
        var totalDiscounts = orders.Sum(o => o.DiscountTotal);
        var totalOrders = orders.Count;
        var aov = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        var previousRevenue = previousOrders.Sum(o => o.GrandTotal);
        var revenueGrowth = previousRevenue > 0 ? (totalRevenue - previousRevenue) / previousRevenue * 100 : 0;

        var previousOrderCount = previousOrders.Count;
        var orderGrowth = previousOrderCount > 0 ? (int)((totalOrders - previousOrderCount) / (decimal)previousOrderCount * 100) : 0;

        // Revenue by day of week
        var revenueByDay = orders
            .GroupBy(o => o.OrderDate.DayOfWeek.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(o => o.GrandTotal));

        // Orders by status
        var ordersByStatus = orders
            .GroupBy(o => o.FinancialStatus ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        // Daily trends
        var dailyRevenue = orders
            .GroupBy(o => o.OrderDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesDataPoint(g.Key, g.Key.ToString("MMM dd"), g.Sum(o => o.GrandTotal)))
            .ToList();

        var dailyOrders = orders
            .GroupBy(o => o.OrderDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesDataPoint(g.Key, g.Key.ToString("MMM dd"), g.Count()))
            .ToList();

        return new SalesReportDto(
            totalRevenue, totalSubtotal, totalTax, totalShipping, totalDiscounts,
            totalOrders, aov, revenueGrowth, orderGrowth,
            revenueByDay, ordersByStatus, dailyRevenue, dailyOrders
        );
    }

    public async Task<List<SalesByPeriodDto>> GetSalesByPeriodAsync(string shopDomain, DateRangeRequest request, string period)
    {
        var orders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain && o.OrderDate >= request.StartDate && o.OrderDate <= request.EndDate)
            .ToListAsync();

        var groupedData = orders
            .GroupBy(o => GetPeriodKey(o.OrderDate, period))
            .Select(g => new
            {
                Key = g.Key,
                Revenue = g.Sum(o => o.GrandTotal),
                OrderCount = g.Count(),
                PeriodStart = g.Min(o => o.OrderDate),
                PeriodEnd = g.Max(o => o.OrderDate)
            })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.PeriodNumber)
            .ToList();

        var results = new List<SalesByPeriodDto>();
        SalesByPeriodDto? previous = null;

        foreach (var group in groupedData)
        {
            var aov = group.OrderCount > 0 ? group.Revenue / group.OrderCount : 0;
            var growth = previous != null && previous.Revenue > 0
                ? (group.Revenue - previous.Revenue) / previous.Revenue * 100
                : 0;

            var periodLabel = GetPeriodLabel(group.PeriodStart, period);
            var dto = new SalesByPeriodDto(periodLabel, group.PeriodStart, group.PeriodEnd, group.Revenue, group.OrderCount, aov, growth);
            results.Add(dto);
            previous = dto;
        }

        return results;
    }

    // ============= Products =============

    public async Task<ProductReportSummaryDto> GetProductReportAsync(string shopDomain, DateRangeRequest request)
    {
        var products = await _context.Products
            .Where(p => p.ShopDomain == shopDomain && p.IsActive)
            .ToListAsync();

        var orderLines = await _context.OrderLines
            .Include(ol => ol.Order)
            .Where(ol => ol.Order.ShopDomain == shopDomain && ol.Order.OrderDate >= request.StartDate && ol.Order.OrderDate <= request.EndDate)
            .ToListAsync();

        var productSales = orderLines
            .GroupBy(ol => ol.ProductTitle)
            .Select(g => new
            {
                ProductTitle = g.Key,
                TotalQuantity = g.Sum(ol => ol.Quantity),
                TotalRevenue = g.Sum(ol => ol.LineTotal)
            })
            .ToDictionary(x => x.ProductTitle);

        var productPerformance = products.Select(p =>
        {
            var sales = productSales.GetValueOrDefault(p.Title);
            var qty = sales?.TotalQuantity ?? 0;
            var revenue = sales?.TotalRevenue ?? 0;
            var avgPrice = qty > 0 ? revenue / qty : p.Price;
            var grossProfit = p.CostOfGoodsSold.HasValue && qty > 0 ? revenue - (p.CostOfGoodsSold.Value * qty) : (decimal?)null;
            var margin = grossProfit.HasValue && revenue > 0 ? grossProfit.Value / revenue * 100 : (decimal?)null;

            return new ProductPerformanceDto(
                p.Id, p.Title, p.Vendor, p.ProductType,
                qty, revenue, avgPrice, p.InventoryQuantity,
                p.CostOfGoodsSold, grossProfit, margin
            );
        }).ToList();

        var bestSellers = productPerformance.OrderByDescending(p => p.TotalRevenue).Take(10).ToList();
        var lowStock = productPerformance.Where(p => p.InventoryQuantity < 10 && p.InventoryQuantity > 0).OrderBy(p => p.InventoryQuantity).Take(10).ToList();
        var noSales = productPerformance.Where(p => p.TotalQuantitySold == 0).Take(10).ToList();

        var revenueByVendor = productPerformance
            .Where(p => !string.IsNullOrEmpty(p.Vendor))
            .GroupBy(p => p.Vendor!)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalRevenue));

        var revenueByType = productPerformance
            .Where(p => !string.IsNullOrEmpty(p.ProductType))
            .GroupBy(p => p.ProductType!)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalRevenue));

        return new ProductReportSummaryDto(
            products.Count,
            productPerformance.Count(p => p.TotalQuantitySold > 0),
            productPerformance.Sum(p => p.TotalRevenue),
            productPerformance.Sum(p => p.TotalQuantitySold),
            products.Any() ? products.Average(p => p.Price) : 0,
            bestSellers, lowStock, noSales,
            revenueByVendor, revenueByType
        );
    }

    public async Task<List<ProductPerformanceDto>> GetProductPerformanceListAsync(string shopDomain, DateRangeRequest request, int take = 50)
    {
        var products = await _context.Products
            .Where(p => p.ShopDomain == shopDomain && p.IsActive)
            .ToListAsync();

        var orderLines = await _context.OrderLines
            .Include(ol => ol.Order)
            .Where(ol => ol.Order.ShopDomain == shopDomain && ol.Order.OrderDate >= request.StartDate && ol.Order.OrderDate <= request.EndDate)
            .ToListAsync();

        var productSales = orderLines
            .GroupBy(ol => ol.ProductTitle)
            .ToDictionary(g => g.Key, g => new { Qty = g.Sum(ol => ol.Quantity), Revenue = g.Sum(ol => ol.LineTotal) });

        return products.Select(p =>
        {
            var sales = productSales.GetValueOrDefault(p.Title);
            var qty = sales?.Qty ?? 0;
            var revenue = sales?.Revenue ?? 0;
            var avgPrice = qty > 0 ? revenue / qty : p.Price;
            var grossProfit = p.CostOfGoodsSold.HasValue && qty > 0 ? revenue - (p.CostOfGoodsSold.Value * qty) : (decimal?)null;
            var margin = grossProfit.HasValue && revenue > 0 ? grossProfit.Value / revenue * 100 : (decimal?)null;

            return new ProductPerformanceDto(p.Id, p.Title, p.Vendor, p.ProductType, qty, revenue, avgPrice, p.InventoryQuantity, p.CostOfGoodsSold, grossProfit, margin);
        })
        .OrderByDescending(p => p.TotalRevenue)
        .Take(take)
        .ToList();
    }

    private async Task<List<ProductPerformanceDto>> GetTopProductsAsync(string shopDomain, DateRangeRequest request, int take)
    {
        return await GetProductPerformanceListAsync(shopDomain, request, take);
    }

    // ============= Customers =============

    public async Task<CustomerReportDto> GetCustomerReportAsync(string shopDomain, DateRangeRequest request)
    {
        var clvData = await _context.CustomerLifetimeValues
            .Where(c => c.ShopDomain == shopDomain)
            .ToListAsync();

        var orders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain && o.OrderDate >= request.StartDate && o.OrderDate <= request.EndDate)
            .ToListAsync();

        // Get all customer IDs who placed orders in the current period
        var currentPeriodCustomerIds = orders
            .Where(o => o.CustomerId.HasValue)
            .Select(o => o.CustomerId!.Value)
            .Distinct()
            .ToList();

        // Get customers who had orders BEFORE the current period (returning customers)
        var customersWithPriorOrders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain &&
                        o.CustomerId.HasValue &&
                        currentPeriodCustomerIds.Contains(o.CustomerId!.Value) &&
                        o.OrderDate < request.StartDate)
            .Select(o => o.CustomerId!.Value)
            .Distinct()
            .ToListAsync();

        // New customers = those in current period who had NO orders before
        var newCustomerIds = currentPeriodCustomerIds
            .Except(customersWithPriorOrders)
            .ToList();

        // Returning customers = those in current period who HAD orders before
        var returningCustomerIds = currentPeriodCustomerIds
            .Intersect(customersWithPriorOrders)
            .ToList();

        var avgLTV = clvData.Any() ? clvData.Average(c => c.PredictedLifetimeValue) : 0;

        var customersBySegment = clvData
            .GroupBy(c => c.Segment ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        var segmentDetails = clvData
            .GroupBy(c => c.Segment ?? "Unknown")
            .Select(g => new CustomerSegmentDto(
                g.Key,
                g.Count(),
                g.Sum(c => c.TotalSpent),
                g.Average(c => c.TotalSpent),
                g.Average(c => c.PredictedLifetimeValue),
                (int)g.Average(c => c.TotalOrders)
            ))
            .ToList();

        // Customer acquisition trend
        var acquisitionTrend = orders
            .Where(o => o.CustomerId.HasValue)
            .GroupBy(o => o.OrderDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesDataPoint(g.Key, g.Key.ToString("MMM dd"), g.Select(o => o.CustomerId).Distinct().Count()))
            .ToList();

        return new CustomerReportDto(
            clvData.Count,
            newCustomerIds.Count,
            returningCustomerIds.Count,
            avgLTV,
            customersBySegment,
            acquisitionTrend,
            segmentDetails
        );
    }

    public async Task<List<TopCustomerDto>> GetTopCustomersAsync(string shopDomain, DateRangeRequest request, int take = 20)
    {
        var clvData = await _context.CustomerLifetimeValues
            .Include(c => c.Customer)
            .Where(c => c.ShopDomain == shopDomain)
            .OrderByDescending(c => c.TotalSpent)
            .Take(take)
            .ToListAsync();

        return clvData.Select(c => new TopCustomerDto(
            c.CustomerId,
            c.Customer?.Email ?? "",
            $"{c.Customer?.FirstName} {c.Customer?.LastName}".Trim(),
            c.TotalSpent,
            c.TotalOrders,
            c.AverageOrderValue,
            c.Segment ?? "Unknown",
            c.FirstOrderDate,
            c.LastOrderDate
        )).ToList();
    }

    // ============= Financial =============

    public async Task<FinancialReportDto> GetFinancialReportAsync(string shopDomain, DateRangeRequest request)
    {
        var orders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain && o.OrderDate >= request.StartDate && o.OrderDate <= request.EndDate)
            .ToListAsync();

        var orderIds = orders.Select(o => o.Id).ToList();

        var refunds = await _context.Refunds
            .Include(r => r.Order)
            .Where(r => r.Order.ShopDomain == shopDomain && r.RefundedAt >= request.StartDate && r.RefundedAt <= request.EndDate)
            .ToListAsync();

        var adsSpends = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain && a.SpendDate >= request.StartDate && a.SpendDate <= request.EndDate)
            .ToListAsync();

        // Get order lines for COGS calculation
        var orderLines = await _context.OrderLines
            .Where(ol => orderIds.Contains(ol.OrderId))
            .ToListAsync();

        // Get products to lookup COGS
        var productTitles = orderLines.Select(ol => ol.ProductTitle).Distinct().ToList();
        var products = await _context.Products
            .Where(p => p.ShopDomain == shopDomain && productTitles.Contains(p.Title))
            .ToDictionaryAsync(p => p.Title, p => p.CostOfGoodsSold);

        var grossRevenue = orders.Sum(o => o.GrandTotal);
        var totalRefunds = refunds.Sum(r => r.Amount);
        var netRevenue = grossRevenue - totalRefunds;
        var totalAdsSpend = adsSpends.Sum(a => a.Amount);

        // Calculate COGS from order lines and product costs
        var totalCOGS = orderLines.Sum(ol =>
        {
            var cogs = products.GetValueOrDefault(ol.ProductTitle);
            return cogs.HasValue ? cogs.Value * ol.Quantity : 0m;
        });

        var grossProfit = netRevenue - totalCOGS;
        var grossProfitMargin = netRevenue > 0 ? grossProfit / netRevenue * 100 : 0;

        var netProfit = grossProfit - totalAdsSpend;
        var netProfitMargin = netRevenue > 0 ? netProfit / netRevenue * 100 : 0;

        // Revenue vs Refunds by day
        var revenueByDay = orders.GroupBy(o => o.OrderDate.Date).ToDictionary(g => g.Key, g => g.Sum(o => o.GrandTotal));
        var refundsByDay = refunds.GroupBy(r => r.RefundedAt.Date).ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        var allDates = revenueByDay.Keys.Union(refundsByDay.Keys).OrderBy(d => d).ToList();
        var revenueVsRefunds = allDates.Select(d => new TimeSeriesDataPoint(
            d, d.ToString("MMM dd"),
            revenueByDay.GetValueOrDefault(d, 0),
            refundsByDay.GetValueOrDefault(d, 0)
        )).ToList();

        // Profit trend
        var profitTrend = allDates.Select(d => new TimeSeriesDataPoint(
            d, d.ToString("MMM dd"),
            revenueByDay.GetValueOrDefault(d, 0) - refundsByDay.GetValueOrDefault(d, 0)
        )).ToList();

        // Refunds by reason
        var refundsByReason = refunds
            .GroupBy(r => r.Reason ?? "No reason")
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        // Recent refunds
        var recentRefunds = refunds
            .OrderByDescending(r => r.RefundedAt)
            .Take(10)
            .Select(r => new RefundSummaryDto(r.Id, r.OrderId, r.Order.OrderNumber ?? "", r.Amount, r.Reason, r.RefundedAt))
            .ToList();

        return new FinancialReportDto(
            grossRevenue, totalRefunds, netRevenue, totalCOGS,
            grossProfit, grossProfitMargin, totalAdsSpend,
            netProfit, netProfitMargin,
            revenueVsRefunds, profitTrend, refundsByReason, recentRefunds
        );
    }

    // ============= Advertising =============

    public async Task<AdvertisingReportDto> GetAdvertisingReportAsync(string shopDomain, DateRangeRequest request)
    {
        var adsData = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain && a.SpendDate >= request.StartDate && a.SpendDate <= request.EndDate)
            .ToListAsync();

        var totalSpend = adsData.Sum(a => a.Amount);
        var totalRevenue = adsData.Sum(a => a.Revenue ?? 0);
        var roas = totalSpend > 0 ? totalRevenue / totalSpend : 0;

        var totalImpressions = adsData.Sum(a => a.Impressions ?? 0);
        var totalClicks = adsData.Sum(a => a.Clicks ?? 0);
        var totalConversions = adsData.Sum(a => a.Conversions ?? 0);

        var ctr = totalImpressions > 0 ? (decimal)totalClicks / totalImpressions * 100 : 0;
        var conversionRate = totalClicks > 0 ? (decimal)totalConversions / totalClicks * 100 : 0;
        var cpc = totalClicks > 0 ? totalSpend / totalClicks : 0;
        var cpa = totalConversions > 0 ? totalSpend / totalConversions : 0;

        // Metrics by platform
        var metricsByPlatform = adsData
            .GroupBy(a => a.Platform ?? "Unknown")
            .ToDictionary(g => g.Key, g =>
            {
                var spend = g.Sum(a => a.Amount);
                var revenue = g.Sum(a => a.Revenue ?? 0);
                var impressions = g.Sum(a => a.Impressions ?? 0);
                var clicks = g.Sum(a => a.Clicks ?? 0);
                var conversions = g.Sum(a => a.Conversions ?? 0);

                return new AdPlatformMetricsDto(
                    g.Key, spend, revenue,
                    spend > 0 ? revenue / spend : 0,
                    impressions, clicks, conversions,
                    impressions > 0 ? (decimal)clicks / impressions * 100 : 0,
                    clicks > 0 ? (decimal)conversions / clicks * 100 : 0
                );
            });

        // Top campaigns
        var topCampaigns = await GetCampaignPerformanceAsync(shopDomain, request, null);

        // Spend vs Revenue trend
        var spendVsRevenue = adsData
            .GroupBy(a => a.SpendDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesDataPoint(
                g.Key, g.Key.ToString("MMM dd"),
                g.Sum(a => a.Amount),
                g.Sum(a => a.Revenue ?? 0)
            ))
            .ToList();

        return new AdvertisingReportDto(
            totalSpend, totalRevenue, roas,
            totalImpressions, totalClicks, totalConversions,
            ctr, conversionRate, cpc, cpa,
            metricsByPlatform, topCampaigns.Take(10).ToList(), spendVsRevenue
        );
    }

    public async Task<List<CampaignPerformanceDto>> GetCampaignPerformanceAsync(string shopDomain, DateRangeRequest request, string? platform = null)
    {
        var query = _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain && a.SpendDate >= request.StartDate && a.SpendDate <= request.EndDate);

        if (!string.IsNullOrEmpty(platform))
            query = query.Where(a => a.Platform == platform);

        var adsData = await query.ToListAsync();

        return adsData
            .GroupBy(a => new { a.CampaignName, a.Platform })
            .Select(g =>
            {
                var spend = g.Sum(a => a.Amount);
                var revenue = g.Sum(a => a.Revenue ?? 0);
                var impressions = g.Sum(a => a.Impressions ?? 0);
                var clicks = g.Sum(a => a.Clicks ?? 0);
                var conversions = g.Sum(a => a.Conversions ?? 0);

                return new CampaignPerformanceDto(
                    g.Key.CampaignName ?? "Unknown",
                    g.Key.Platform ?? "Unknown",
                    spend, revenue,
                    spend > 0 ? revenue / spend : 0,
                    impressions, clicks, conversions,
                    impressions > 0 ? (decimal)clicks / impressions * 100 : 0,
                    clicks > 0 ? (decimal)conversions / clicks * 100 : 0,
                    clicks > 0 ? spend / clicks : 0,
                    conversions > 0 ? spend / conversions : 0
                );
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();
    }

    // ============= Export =============

    public async Task<byte[]> ExportSalesReportAsync(string shopDomain, DateRangeRequest request, string format)
    {
        var report = await GetSalesReportAsync(shopDomain, request);
        var sb = new StringBuilder();

        sb.AppendLine("Sales Report");
        sb.AppendLine($"Period: {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine($"Total Revenue,{report.TotalRevenue}");
        sb.AppendLine($"Total Orders,{report.TotalOrders}");
        sb.AppendLine($"Average Order Value,{report.AverageOrderValue}");
        sb.AppendLine($"Revenue Growth %,{report.RevenueGrowthPercent}");
        sb.AppendLine();
        sb.AppendLine("Daily Revenue");
        sb.AppendLine("Date,Revenue");
        foreach (var point in report.DailyRevenue)
            sb.AppendLine($"{point.Label},{point.Value}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportProductReportAsync(string shopDomain, DateRangeRequest request, string format)
    {
        var products = await GetProductPerformanceListAsync(shopDomain, request, 1000);
        var sb = new StringBuilder();

        sb.AppendLine("Product,Vendor,Type,Units Sold,Revenue,Avg Price,Inventory");
        foreach (var p in products)
            sb.AppendLine($"\"{p.ProductTitle}\",\"{p.Vendor}\",\"{p.ProductType}\",{p.TotalQuantitySold},{p.TotalRevenue},{p.AverageSellingPrice},{p.InventoryQuantity}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportCustomerReportAsync(string shopDomain, DateRangeRequest request, string format)
    {
        var customers = await GetTopCustomersAsync(shopDomain, request, 1000);
        var sb = new StringBuilder();

        sb.AppendLine("Email,Name,Total Spent,Orders,AOV,Segment");
        foreach (var c in customers)
            sb.AppendLine($"\"{c.CustomerEmail}\",\"{c.CustomerName}\",{c.TotalSpent},{c.TotalOrders},{c.AverageOrderValue},\"{c.Segment}\"");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ============= Helpers =============

    private static (int Year, int PeriodNumber) GetPeriodKey(DateTime date, string period)
    {
        return period.ToLower() switch
        {
            "week" => (date.Year, CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday)),
            "month" => (date.Year, date.Month),
            "quarter" => (date.Year, (date.Month - 1) / 3 + 1),
            _ => (date.Year, date.DayOfYear)
        };
    }

    private static string GetPeriodLabel(DateTime date, string period)
    {
        return period.ToLower() switch
        {
            "week" => $"Week {CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}, {date.Year}",
            "month" => date.ToString("MMM yyyy"),
            "quarter" => $"Q{(date.Month - 1) / 3 + 1} {date.Year}",
            _ => date.ToString("MMM dd, yyyy")
        };
    }
}

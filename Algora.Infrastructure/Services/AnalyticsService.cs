using Algora.Application.DTOs.Analytics;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Complete implementation of IAnalyticsService for dashboard metrics, product performance,
/// CLV calculations, cohort analysis, profit margins, and ads spend tracking.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(AppDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== Dashboard Methods ====================

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(string shopDomain, AnalyticsTimePeriod period)
    {
        var (startDate, endDate) = GetDateRange(period);
        var (prevStartDate, prevEndDate) = GetPreviousDateRange(period);

        // Current period metrics
        var orders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain &&
                       o.OrderDate >= startDate &&
                       o.OrderDate <= endDate)
            .ToListAsync();

        // Previous period metrics for comparison
        var prevOrders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain &&
                       o.OrderDate >= prevStartDate &&
                       o.OrderDate < startDate)
            .ToListAsync();

        // Calculate COGS for current period
        var orderLineItems = await _context.OrderLines
            .Where(ol => orders.Select(o => o.Id).Contains(ol.OrderId))
            .ToListAsync();

        var totalCOGS = await CalculateCOGSForOrderLines(shopDomain, orderLineItems);

        // Calculate ads spend for current period
        var adsSpend = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                       a.SpendDate >= startDate &&
                       a.SpendDate <= endDate)
            .SumAsync(a => (decimal?)a.Amount) ?? 0;

        // Calculate refunds for current period
        var refunds = await _context.Refunds
            .Where(r => orders.Select(o => o.Id).Contains(r.OrderId))
            .SumAsync(r => (decimal?)r.Amount) ?? 0;

        // Current metrics
        var totalRevenue = orders.Sum(o => o.GrandTotal);
        var totalOrders = orders.Count;
        var aov = totalOrders > 0 ? totalRevenue / totalOrders : 0;
        var grossProfit = totalRevenue - totalCOGS;
        var netProfit = grossProfit - adsSpend - refunds;
        var grossProfitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;
        var netProfitMargin = totalRevenue > 0 ? (netProfit / totalRevenue) * 100 : 0;

        // Previous metrics for comparison
        var prevRevenue = prevOrders.Sum(o => o.GrandTotal);
        var prevOrderCount = prevOrders.Count;
        var prevAov = prevOrderCount > 0 ? prevRevenue / prevOrderCount : 0;

        // Calculate changes
        var revenueChange = CalculatePercentageChange(totalRevenue, prevRevenue);
        var ordersChange = CalculatePercentageChange(totalOrders, prevOrderCount);
        var aovChange = CalculatePercentageChange(aov, prevAov);

        // Customer metrics
        var customerIds = orders.Where(o => o.CustomerId.HasValue).Select(o => o.CustomerId!.Value).Distinct().ToList();
        var newCustomers = 0;
        var returningCustomers = 0;

        foreach (var customerId in customerIds)
        {
            var firstOrder = await _context.Orders
                .Where(o => o.ShopDomain == shopDomain && o.CustomerId == customerId)
                .OrderBy(o => o.OrderDate)
                .FirstOrDefaultAsync();

            if (firstOrder != null && firstOrder.OrderDate >= startDate && firstOrder.OrderDate <= endDate)
            {
                newCustomers++;
            }
            else
            {
                returningCustomers++;
            }
        }

        // Total units sold
        var totalUnitsSold = orderLineItems.Sum(ol => ol.Quantity);

        return new DashboardSummaryDto(
            TotalRevenue: totalRevenue,
            RevenueChange: revenueChange,
            TotalOrders: totalOrders,
            OrdersChange: ordersChange,
            AverageOrderValue: aov,
            AovChange: aovChange,
            GrossProfit: grossProfit,
            GrossProfitMargin: grossProfitMargin,
            NetProfit: netProfit,
            NetProfitMargin: netProfitMargin,
            NewCustomers: newCustomers,
            ReturningCustomers: returningCustomers,
            ConversionRate: 0, // Would need session data
            TotalUnitsSold: totalUnitsSold
        );
    }

    public async Task<SalesTrendDto> GetSalesTrendAsync(string shopDomain, AnalyticsTimePeriod period)
    {
        var (startDate, endDate) = GetDateRange(period);

        var orders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain &&
                       o.OrderDate >= startDate &&
                       o.OrderDate <= endDate)
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        var orderIds = orders.Select(o => o.Id).ToList();

        var orderLines = await _context.OrderLines
            .Where(ol => orderIds.Contains(ol.OrderId))
            .ToListAsync();

        var adsSpends = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                       a.SpendDate >= startDate &&
                       a.SpendDate <= endDate)
            .ToListAsync();

        // Group by date
        var groupedData = orders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Orders = g.ToList(),
                Revenue = g.Sum(o => o.GrandTotal)
            })
            .OrderBy(x => x.Date)
            .ToList();

        var dataPoints = new List<SalesTrendPointDto>();

        foreach (var group in groupedData)
        {
            var dayOrderIds = group.Orders.Select(o => o.Id).ToList();
            var dayOrderLines = orderLines.Where(ol => dayOrderIds.Contains(ol.OrderId)).ToList();
            var dayCOGS = await CalculateCOGSForOrderLines(shopDomain, dayOrderLines);
            var dayAdsSpend = adsSpends.Where(a => a.SpendDate.Date == group.Date).Sum(a => a.Amount);
            var dayProfit = group.Revenue - dayCOGS - dayAdsSpend;

            dataPoints.Add(new SalesTrendPointDto(
                Date: group.Date,
                Revenue: group.Revenue,
                Profit: dayProfit,
                Orders: group.Orders.Count,
                COGS: dayCOGS,
                AdsSpend: dayAdsSpend
            ));
        }

        return new SalesTrendDto(
            DataPoints: dataPoints,
            TotalRevenue: dataPoints.Sum(d => d.Revenue),
            TotalProfit: dataPoints.Sum(d => d.Profit),
            TotalOrders: dataPoints.Sum(d => d.Orders)
        );
    }

    public async Task<CostBreakdownDto> GetCostBreakdownAsync(string shopDomain, AnalyticsTimePeriod period)
    {
        var (startDate, endDate) = GetDateRange(period);

        var orders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain &&
                       o.OrderDate >= startDate &&
                       o.OrderDate <= endDate)
            .ToListAsync();

        var revenue = orders.Sum(o => o.GrandTotal);

        var orderIds = orders.Select(o => o.Id).ToList();
        var orderLines = await _context.OrderLines
            .Where(ol => orderIds.Contains(ol.OrderId))
            .ToListAsync();

        var cogs = await CalculateCOGSForOrderLines(shopDomain, orderLines);

        var adsSpend = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                       a.SpendDate >= startDate &&
                       a.SpendDate <= endDate)
            .SumAsync(a => (decimal?)a.Amount) ?? 0;

        var refunds = await _context.Refunds
            .Where(r => orderIds.Contains(r.OrderId))
            .SumAsync(r => (decimal?)r.Amount) ?? 0;

        var grossProfit = revenue - cogs;
        var netProfit = grossProfit - adsSpend - refunds;

        return new CostBreakdownDto(
            Revenue: revenue,
            COGS: cogs,
            AdsSpend: adsSpend,
            Refunds: refunds,
            GrossProfit: grossProfit,
            NetProfit: netProfit
        );
    }

    // ==================== Product Methods ====================

    public async Task<ProductHeatmapDto> GetProductHeatmapAsync(string shopDomain, AnalyticsTimePeriod period, int limit = 50)
    {
        var (startDate, endDate) = GetDateRange(period);

        var orders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain &&
                       o.OrderDate >= startDate &&
                       o.OrderDate <= endDate)
            .Select(o => o.Id)
            .ToListAsync();

        var orderLines = await _context.OrderLines
            .Where(ol => orders.Contains(ol.OrderId) && ol.PlatformProductId.HasValue)
            .ToListAsync();

        var productGroups = orderLines
            .GroupBy(ol => ol.PlatformProductId!.Value)
            .Select(g => new
            {
                PlatformProductId = g.Key,
                ProductTitle = g.First().ProductTitle,
                Sku = g.First().Sku,
                QuantitySold = g.Sum(ol => ol.Quantity),
                Revenue = g.Sum(ol => ol.LineTotal),
                OrderLines = g.ToList()
            })
            .ToList();

        var products = new List<ProductPerformanceDto>();
        var totalRevenue = productGroups.Sum(p => p.Revenue);

        foreach (var group in productGroups)
        {
            var cogs = await CalculateCOGSForOrderLines(shopDomain, group.OrderLines);
            var profit = group.Revenue - cogs;
            var profitMargin = group.Revenue > 0 ? (profit / group.Revenue) * 100 : 0;

            var product = await _context.Products
                .Where(p => p.ShopDomain == shopDomain && p.PlatformProductId == group.PlatformProductId)
                .FirstOrDefaultAsync();

            var imageUrl = await _context.ProductImages
                .Where(pi => product != null && pi.ProductId == product.Id)
                .OrderBy(pi => pi.Position)
                .Select(pi => pi.Src)
                .FirstOrDefaultAsync();

            var performanceLevel = profitMargin >= 40 ? "excellent" :
                                  profitMargin >= 25 ? "good" :
                                  profitMargin >= 10 ? "average" : "poor";

            products.Add(new ProductPerformanceDto(
                ProductId: product?.Id ?? 0,
                PlatformProductId: group.PlatformProductId,
                ProductTitle: group.ProductTitle,
                Sku: group.Sku,
                ImageUrl: imageUrl,
                QuantitySold: group.QuantitySold,
                Revenue: group.Revenue,
                COGS: cogs,
                Profit: profit,
                ProfitMargin: profitMargin,
                PerformanceLevel: performanceLevel,
                RevenueShare: totalRevenue > 0 ? (group.Revenue / totalRevenue) * 100 : 0
            ));
        }

        var sortedProducts = products.OrderByDescending(p => p.Revenue).Take(limit).ToList();

        return new ProductHeatmapDto(
            Products: sortedProducts,
            SortBy: "revenue",
            TotalProducts: products.Count
        );
    }

    public async Task<List<TopProductDto>> GetTopProductsAsync(string shopDomain, AnalyticsTimePeriod period, string sortBy = "revenue", int limit = 10)
    {
        var (startDate, endDate) = GetDateRange(period);

        var orders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain &&
                       o.OrderDate >= startDate &&
                       o.OrderDate <= endDate)
            .Select(o => o.Id)
            .ToListAsync();

        var orderLines = await _context.OrderLines
            .Where(ol => orders.Contains(ol.OrderId) && ol.PlatformProductId.HasValue)
            .ToListAsync();

        var productGroups = orderLines
            .GroupBy(ol => ol.PlatformProductId!.Value)
            .Select(g => new
            {
                PlatformProductId = g.Key,
                ProductTitle = g.First().ProductTitle,
                Sku = g.First().Sku,
                QuantitySold = g.Sum(ol => ol.Quantity),
                Revenue = g.Sum(ol => ol.LineTotal),
                OrderLines = g.ToList()
            })
            .ToList();

        var topProducts = new List<TopProductDto>();

        foreach (var group in productGroups)
        {
            var cogs = await CalculateCOGSForOrderLines(shopDomain, group.OrderLines);
            var profit = group.Revenue - cogs;

            var product = await _context.Products
                .Where(p => p.ShopDomain == shopDomain && p.PlatformProductId == group.PlatformProductId)
                .FirstOrDefaultAsync();

            var imageUrl = await _context.ProductImages
                .Where(pi => product != null && pi.ProductId == product.Id)
                .OrderBy(pi => pi.Position)
                .Select(pi => pi.Src)
                .FirstOrDefaultAsync();

            topProducts.Add(new TopProductDto(
                ProductId: product?.Id ?? 0,
                ProductTitle: group.ProductTitle,
                Sku: group.Sku,
                ImageUrl: imageUrl,
                QuantitySold: group.QuantitySold,
                Revenue: group.Revenue,
                Profit: profit
            ));
        }

        var sorted = sortBy.ToLower() switch
        {
            "quantity" => topProducts.OrderByDescending(p => p.QuantitySold),
            "profit" => topProducts.OrderByDescending(p => p.Profit),
            _ => topProducts.OrderByDescending(p => p.Revenue)
        };

        return sorted.Take(limit).ToList();
    }

    // ==================== CLV Methods ====================

    public async Task<ClvReportDto> GetClvReportAsync(string shopDomain)
    {
        var clvRecords = await _context.CustomerLifetimeValues
            .Include(c => c.Customer)
            .Where(c => c.ShopDomain == shopDomain)
            .ToListAsync();

        if (!clvRecords.Any())
        {
            // Calculate CLV if not already done
            await RecalculateClvAsync(shopDomain);
            clvRecords = await _context.CustomerLifetimeValues
                .Include(c => c.Customer)
                .Where(c => c.ShopDomain == shopDomain)
                .ToListAsync();
        }

        var totalCustomers = clvRecords.Count;
        var totalClv = clvRecords.Sum(c => c.PredictedLifetimeValue);
        var averageClv = totalCustomers > 0 ? totalClv / totalCustomers : 0;
        var highValueCount = clvRecords.Count(c => c.Segment == "high_value");
        var highValuePercentage = totalCustomers > 0 ? (decimal)highValueCount / totalCustomers * 100 : 0;
        var churnedCount = clvRecords.Count(c => c.Segment == "churned");
        var churnRate = totalCustomers > 0 ? (decimal)churnedCount / totalCustomers * 100 : 0;

        var summary = new ClvSummaryDto(
            AverageClv: averageClv,
            TotalClv: totalClv,
            TotalCustomers: totalCustomers,
            HighValuePercentage: highValuePercentage,
            ChurnRate: churnRate
        );

        var topCustomers = clvRecords
            .OrderByDescending(c => c.PredictedLifetimeValue)
            .Take(10)
            .Select(c => MapToClvDto(c))
            .ToList();

        var atRiskCustomers = clvRecords
            .Where(c => c.Segment == "at_risk")
            .OrderByDescending(c => c.TotalSpent)
            .Take(10)
            .Select(c => MapToClvDto(c))
            .ToList();

        var segments = clvRecords
            .GroupBy(c => c.Segment)
            .Select(g => new ClvSegmentDto(
                Segment: g.Key,
                CustomerCount: g.Count(),
                TotalValue: g.Sum(c => c.PredictedLifetimeValue),
                AverageValue: g.Average(c => c.PredictedLifetimeValue),
                PercentageOfTotal: totalCustomers > 0 ? (decimal)g.Count() / totalCustomers * 100 : 0
            ))
            .OrderByDescending(s => s.AverageValue)
            .ToList();

        return new ClvReportDto(
            Summary: summary,
            TopCustomers: topCustomers,
            AtRiskCustomers: atRiskCustomers,
            Segments: segments
        );
    }

    public async Task<int> RecalculateClvAsync(string shopDomain)
    {
        _logger.LogInformation("Recalculating CLV for shop: {ShopDomain}", shopDomain);

        var customers = await _context.Customers
            .Where(c => c.ShopDomain == shopDomain)
            .Include(c => c.Orders)
            .ToListAsync();

        var recalculatedCount = 0;

        foreach (var customer in customers)
        {
            var orders = customer.Orders.OrderBy(o => o.OrderDate).ToList();
            if (!orders.Any()) continue;

            var totalOrders = orders.Count;
            var totalSpent = orders.Sum(o => o.GrandTotal);
            var averageOrderValue = totalSpent / totalOrders;
            var firstOrderDate = orders.First().OrderDate;
            var lastOrderDate = orders.Last().OrderDate;
            var daysSinceLastOrder = (DateTime.UtcNow - lastOrderDate).Days;

            decimal? averageDaysBetweenOrders = null;
            if (totalOrders > 1)
            {
                var daysBetweenOrders = new List<int>();
                for (int i = 1; i < orders.Count; i++)
                {
                    daysBetweenOrders.Add((orders[i].OrderDate - orders[i - 1].OrderDate).Days);
                }
                averageDaysBetweenOrders = (decimal)daysBetweenOrders.Average();
            }

            // Calculate profit
            var orderIds = orders.Select(o => o.Id).ToList();
            var orderLines = await _context.OrderLines
                .Where(ol => orderIds.Contains(ol.OrderId))
                .ToListAsync();
            var totalCOGS = await CalculateCOGSForOrderLines(shopDomain, orderLines);
            var totalProfit = totalSpent - totalCOGS;

            // Predict lifetime value (simple model: current spend + projected future spend)
            var customerLifespanDays = (lastOrderDate - firstOrderDate).Days;
            var predictedLifetimeValue = totalSpent;

            if (totalOrders > 1 && averageDaysBetweenOrders.HasValue && averageDaysBetweenOrders.Value > 0)
            {
                var expectedLifespanYears = 3; // Assume 3 year customer lifespan
                var expectedFutureOrders = (expectedLifespanYears * 365) / (double)averageDaysBetweenOrders.Value;
                predictedLifetimeValue = totalSpent + ((decimal)expectedFutureOrders * averageOrderValue);
            }

            // Segment the customer
            var segment = DetermineCustomerSegment(totalSpent, totalOrders, daysSinceLastOrder, averageDaysBetweenOrders);

            var existingClv = await _context.CustomerLifetimeValues
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.CustomerId == customer.Id);

            if (existingClv != null)
            {
                existingClv.TotalOrders = totalOrders;
                existingClv.TotalSpent = totalSpent;
                existingClv.AverageOrderValue = averageOrderValue;
                existingClv.FirstOrderDate = firstOrderDate;
                existingClv.LastOrderDate = lastOrderDate;
                existingClv.DaysSinceLastOrder = daysSinceLastOrder;
                existingClv.AverageDaysBetweenOrders = averageDaysBetweenOrders;
                existingClv.PredictedLifetimeValue = predictedLifetimeValue;
                existingClv.Segment = segment;
                existingClv.TotalProfit = totalProfit;
                existingClv.CalculatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CustomerLifetimeValues.Add(new CustomerLifetimeValue
                {
                    ShopDomain = shopDomain,
                    CustomerId = customer.Id,
                    TotalOrders = totalOrders,
                    TotalSpent = totalSpent,
                    AverageOrderValue = averageOrderValue,
                    FirstOrderDate = firstOrderDate,
                    LastOrderDate = lastOrderDate,
                    DaysSinceLastOrder = daysSinceLastOrder,
                    AverageDaysBetweenOrders = averageDaysBetweenOrders,
                    PredictedLifetimeValue = predictedLifetimeValue,
                    Segment = segment,
                    TotalProfit = totalProfit,
                    CalculatedAt = DateTime.UtcNow
                });
            }

            recalculatedCount++;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("CLV recalculated for {Count} customers", recalculatedCount);

        return recalculatedCount;
    }

    public async Task<CustomerLifetimeValueDto?> GetCustomerClvAsync(string shopDomain, int customerId)
    {
        var clv = await _context.CustomerLifetimeValues
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.CustomerId == customerId);

        return clv != null ? MapToClvDto(clv) : null;
    }

    // ==================== Cohort Methods ====================

    public async Task<CohortAnalysisDto> GetCohortAnalysisAsync(string shopDomain, int monthsBack = 12)
    {
        var startDate = DateTime.UtcNow.AddMonths(-monthsBack).Date;

        var customers = await _context.Customers
            .Where(c => c.ShopDomain == shopDomain)
            .Include(c => c.Orders)
            .ToListAsync();

        var cohorts = new List<CohortDto>();

        for (int i = 0; i < monthsBack; i++)
        {
            var cohortDate = DateTime.UtcNow.AddMonths(-(monthsBack - i - 1));
            var cohortStart = new DateTime(cohortDate.Year, cohortDate.Month, 1);
            var cohortEnd = cohortStart.AddMonths(1);

            var cohortCustomers = customers
                .Where(c => c.Orders.Any() && c.Orders.Min(o => o.OrderDate) >= cohortStart && c.Orders.Min(o => o.OrderDate) < cohortEnd)
                .ToList();

            if (!cohortCustomers.Any()) continue;

            var periods = new List<CohortPeriodDto>();

            for (int period = 0; period <= monthsBack - i; period++)
            {
                var periodStart = cohortStart.AddMonths(period);
                var periodEnd = periodStart.AddMonths(1);

                var activeCustomers = cohortCustomers
                    .Count(c => c.Orders.Any(o => o.OrderDate >= periodStart && o.OrderDate < periodEnd));

                var revenue = cohortCustomers
                    .SelectMany(c => c.Orders)
                    .Where(o => o.OrderDate >= periodStart && o.OrderDate < periodEnd)
                    .Sum(o => o.GrandTotal);

                var retentionRate = (decimal)activeCustomers / cohortCustomers.Count * 100;

                periods.Add(new CohortPeriodDto(
                    PeriodNumber: period,
                    ActiveCustomers: activeCustomers,
                    RetentionRate: retentionRate,
                    Revenue: revenue
                ));
            }

            cohorts.Add(new CohortDto(
                CohortDate: cohortStart,
                CohortLabel: cohortStart.ToString("MMM yyyy"),
                InitialCustomers: cohortCustomers.Count,
                Periods: periods
            ));
        }

        var maxPeriods = cohorts.Any() ? cohorts.Max(c => c.Periods.Count) : 0;

        return new CohortAnalysisDto(
            Cohorts: cohorts,
            MaxPeriods: maxPeriods,
            PeriodType: "monthly"
        );
    }

    // ==================== Profit Methods ====================

    public async Task<ProfitMarginDto> GetProfitMarginAsync(string shopDomain, AnalyticsTimePeriod period)
    {
        var (startDate, endDate) = GetDateRange(period);

        var orders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain &&
                       o.OrderDate >= startDate &&
                       o.OrderDate <= endDate)
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        var orderIds = orders.Select(o => o.Id).ToList();

        var orderLines = await _context.OrderLines
            .Where(ol => orderIds.Contains(ol.OrderId))
            .ToListAsync();

        var totalRevenue = orders.Sum(o => o.GrandTotal);
        var totalCOGS = await CalculateCOGSForOrderLines(shopDomain, orderLines);

        var adsSpend = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                       a.SpendDate >= startDate &&
                       a.SpendDate <= endDate)
            .SumAsync(a => (decimal?)a.Amount) ?? 0;

        var refunds = await _context.Refunds
            .Where(r => orderIds.Contains(r.OrderId))
            .SumAsync(r => (decimal?)r.Amount) ?? 0;

        var grossProfit = totalRevenue - totalCOGS;
        var netProfit = grossProfit - adsSpend - refunds;
        var grossMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;
        var netMargin = totalRevenue > 0 ? (netProfit / totalRevenue) * 100 : 0;

        // Trend data by day
        var trendData = orders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.GrandTotal),
                OrderIds = g.Select(o => o.Id).ToList()
            })
            .OrderBy(x => x.Date)
            .ToList();

        var profitTrend = new List<ProfitByPeriodDto>();

        foreach (var day in trendData)
        {
            var dayOrderLines = orderLines.Where(ol => day.OrderIds.Contains(ol.OrderId)).ToList();
            var dayCOGS = await CalculateCOGSForOrderLines(shopDomain, dayOrderLines);
            var dayAdsSpend = await _context.AdsSpends
                .Where(a => a.ShopDomain == shopDomain && a.SpendDate.Date == day.Date)
                .SumAsync(a => (decimal?)a.Amount) ?? 0;
            var dayRefunds = await _context.Refunds
                .Where(r => day.OrderIds.Contains(r.OrderId))
                .SumAsync(r => (decimal?)r.Amount) ?? 0;

            var dayGrossProfit = day.Revenue - dayCOGS;
            var dayNetProfit = dayGrossProfit - dayAdsSpend - dayRefunds;
            var dayGrossMargin = day.Revenue > 0 ? (dayGrossProfit / day.Revenue) * 100 : 0;
            var dayNetMargin = day.Revenue > 0 ? (dayNetProfit / day.Revenue) * 100 : 0;

            profitTrend.Add(new ProfitByPeriodDto(
                Date: day.Date,
                Revenue: day.Revenue,
                GrossProfit: dayGrossProfit,
                NetProfit: dayNetProfit,
                GrossMargin: dayGrossMargin,
                NetMargin: dayNetMargin
            ));
        }

        var cogsPercentage = totalRevenue > 0 ? (totalCOGS / totalRevenue) * 100 : 0;
        var adsSpendPercentage = totalRevenue > 0 ? (adsSpend / totalRevenue) * 100 : 0;
        var refundsPercentage = totalRevenue > 0 ? (refunds / totalRevenue) * 100 : 0;
        var profitPercentage = totalRevenue > 0 ? (netProfit / totalRevenue) * 100 : 0;

        var breakdown = new ProfitBreakdownDto(
            COGSPercentage: cogsPercentage,
            AdsSpendPercentage: adsSpendPercentage,
            RefundsPercentage: refundsPercentage,
            ProfitPercentage: profitPercentage
        );

        return new ProfitMarginDto(
            TotalRevenue: totalRevenue,
            TotalCOGS: totalCOGS,
            TotalAdsSpend: adsSpend,
            TotalRefunds: refunds,
            GrossProfit: grossProfit,
            GrossProfitMargin: grossMargin,
            NetProfit: netProfit,
            NetProfitMargin: netMargin,
            TrendData: profitTrend,
            Breakdown: breakdown
        );
    }

    // ==================== Ads Spend Methods ====================

    public async Task<List<AdsSpendDto>> GetAdsSpendAsync(string shopDomain, AnalyticsTimePeriod period)
    {
        var (startDate, endDate) = GetDateRange(period);

        var adsSpends = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                       a.SpendDate >= startDate &&
                       a.SpendDate <= endDate)
            .OrderByDescending(a => a.SpendDate)
            .ToListAsync();

        return adsSpends.Select(a => new AdsSpendDto(
            Id: a.Id,
            Platform: a.Platform,
            CampaignName: a.CampaignName,
            CampaignId: a.CampaignId,
            SpendDate: a.SpendDate,
            Amount: a.Amount,
            Currency: a.Currency,
            Impressions: a.Impressions,
            Clicks: a.Clicks,
            Conversions: a.Conversions,
            Revenue: a.Revenue,
            Notes: a.Notes,
            ROAS: a.Revenue.HasValue && a.Amount > 0 ? a.Revenue.Value / a.Amount : null,
            CPC: a.Clicks.HasValue && a.Clicks.Value > 0 ? a.Amount / a.Clicks.Value : null,
            CPM: a.Impressions.HasValue && a.Impressions.Value > 0 ? (a.Amount / a.Impressions.Value) * 1000 : null
        )).ToList();
    }

    public async Task<AdsSpendSummaryDto> GetAdsSpendSummaryAsync(string shopDomain, AnalyticsTimePeriod period)
    {
        var (startDate, endDate) = GetDateRange(period);

        var adsSpends = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                       a.SpendDate >= startDate &&
                       a.SpendDate <= endDate)
            .ToListAsync();

        var totalSpend = adsSpends.Sum(a => a.Amount);
        var totalRevenue = adsSpends.Sum(a => a.Revenue ?? 0);
        var totalConversions = adsSpends.Sum(a => a.Conversions ?? 0);
        var overallROAS = totalSpend > 0 ? totalRevenue / totalSpend : 0;

        var byPlatform = adsSpends
            .GroupBy(a => a.Platform)
            .Select(g => new AdsPlatformSummaryDto(
                Platform: g.Key,
                Spend: g.Sum(a => a.Amount),
                Revenue: g.Sum(a => a.Revenue ?? 0),
                ROAS: g.Sum(a => a.Amount) > 0 ? g.Sum(a => a.Revenue ?? 0) / g.Sum(a => a.Amount) : 0,
                Conversions: g.Sum(a => a.Conversions ?? 0),
                SpendPercentage: totalSpend > 0 ? (g.Sum(a => a.Amount) / totalSpend) * 100 : 0
            ))
            .OrderByDescending(p => p.Spend)
            .ToList();

        return new AdsSpendSummaryDto(
            TotalSpend: totalSpend,
            TotalRevenue: totalRevenue,
            OverallROAS: overallROAS,
            TotalConversions: totalConversions,
            ByPlatform: byPlatform
        );
    }

    public async Task<AdsSpendDto> SaveAdsSpendAsync(string shopDomain, AdsSpendCreateDto dto, int? id = null)
    {
        AdsSpend entity;

        if (id.HasValue)
        {
            entity = await _context.AdsSpends.FindAsync(id.Value)
                ?? throw new InvalidOperationException($"AdsSpend with ID {id} not found");

            if (entity.ShopDomain != shopDomain)
                throw new UnauthorizedAccessException("Cannot modify ads spend for another shop");

            entity.Platform = dto.Platform;
            entity.CampaignName = dto.CampaignName;
            entity.CampaignId = dto.CampaignId;
            entity.SpendDate = dto.SpendDate;
            entity.Amount = dto.Amount;
            entity.Currency = dto.Currency;
            entity.Impressions = dto.Impressions;
            entity.Clicks = dto.Clicks;
            entity.Conversions = dto.Conversions;
            entity.Revenue = dto.Revenue;
            entity.Notes = dto.Notes;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            entity = new AdsSpend
            {
                ShopDomain = shopDomain,
                Platform = dto.Platform,
                CampaignName = dto.CampaignName,
                CampaignId = dto.CampaignId,
                SpendDate = dto.SpendDate,
                Amount = dto.Amount,
                Currency = dto.Currency,
                Impressions = dto.Impressions,
                Clicks = dto.Clicks,
                Conversions = dto.Conversions,
                Revenue = dto.Revenue,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdsSpends.Add(entity);
        }

        await _context.SaveChangesAsync();

        return new AdsSpendDto(
            Id: entity.Id,
            Platform: entity.Platform,
            CampaignName: entity.CampaignName,
            CampaignId: entity.CampaignId,
            SpendDate: entity.SpendDate,
            Amount: entity.Amount,
            Currency: entity.Currency,
            Impressions: entity.Impressions,
            Clicks: entity.Clicks,
            Conversions: entity.Conversions,
            Revenue: entity.Revenue,
            Notes: entity.Notes,
            ROAS: entity.Revenue.HasValue && entity.Amount > 0 ? entity.Revenue.Value / entity.Amount : null,
            CPC: entity.Clicks.HasValue && entity.Clicks.Value > 0 ? entity.Amount / entity.Clicks.Value : null,
            CPM: entity.Impressions.HasValue && entity.Impressions.Value > 0 ? (entity.Amount / entity.Impressions.Value) * 1000 : null
        );
    }

    public async Task<bool> DeleteAdsSpendAsync(string shopDomain, int id)
    {
        var entity = await _context.AdsSpends.FindAsync(id);
        if (entity == null) return false;

        if (entity.ShopDomain != shopDomain)
            throw new UnauthorizedAccessException("Cannot delete ads spend for another shop");

        _context.AdsSpends.Remove(entity);
        await _context.SaveChangesAsync();

        return true;
    }

    // ==================== Snapshot Methods ====================

    public async Task GenerateSnapshotAsync(string shopDomain, DateTime date)
    {
        _logger.LogInformation("Generating analytics snapshot for {ShopDomain} on {Date}", shopDomain, date);

        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        var orders = await _context.Orders
            .Where(o => o.ShopDomain == shopDomain &&
                       o.OrderDate >= startDate &&
                       o.OrderDate < endDate)
            .ToListAsync();

        var orderIds = orders.Select(o => o.Id).ToList();

        var orderLines = await _context.OrderLines
            .Where(ol => orderIds.Contains(ol.OrderId))
            .ToListAsync();

        var totalRevenue = orders.Sum(o => o.GrandTotal);
        var totalCOGS = await CalculateCOGSForOrderLines(shopDomain, orderLines);

        var adsSpend = await _context.AdsSpends
            .Where(a => a.ShopDomain == shopDomain && a.SpendDate.Date == startDate)
            .SumAsync(a => (decimal?)a.Amount) ?? 0;

        var refunds = await _context.Refunds
            .Where(r => orderIds.Contains(r.OrderId))
            .SumAsync(r => (decimal?)r.Amount) ?? 0;

        var grossProfit = totalRevenue - totalCOGS;
        var netProfit = grossProfit - adsSpend - refunds;

        var customerIds = orders.Where(o => o.CustomerId.HasValue).Select(o => o.CustomerId!.Value).Distinct().ToList();
        var newCustomers = 0;
        var returningCustomers = 0;

        foreach (var customerId in customerIds)
        {
            var firstOrder = await _context.Orders
                .Where(o => o.ShopDomain == shopDomain && o.CustomerId == customerId)
                .OrderBy(o => o.OrderDate)
                .FirstOrDefaultAsync();

            if (firstOrder != null && firstOrder.OrderDate.Date == startDate)
                newCustomers++;
            else
                returningCustomers++;
        }

        var totalUnitsSold = orderLines.Sum(ol => ol.Quantity);
        var averageOrderValue = orders.Count > 0 ? totalRevenue / orders.Count : 0;

        var existingSnapshot = await _context.AnalyticsSnapshots
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain &&
                                     s.SnapshotDate.Date == startDate &&
                                     s.PeriodType == "daily");

        if (existingSnapshot != null)
        {
            existingSnapshot.TotalOrders = orders.Count;
            existingSnapshot.TotalRevenue = totalRevenue;
            existingSnapshot.TotalCOGS = totalCOGS;
            existingSnapshot.TotalAdsSpend = adsSpend;
            existingSnapshot.GrossProfit = grossProfit;
            existingSnapshot.NetProfit = netProfit;
            existingSnapshot.TotalRefunds = refunds;
            existingSnapshot.NewCustomers = newCustomers;
            existingSnapshot.ReturningCustomers = returningCustomers;
            existingSnapshot.TotalUnitsSold = totalUnitsSold;
            existingSnapshot.AverageOrderValue = averageOrderValue;
        }
        else
        {
            _context.AnalyticsSnapshots.Add(new AnalyticsSnapshot
            {
                ShopDomain = shopDomain,
                SnapshotDate = startDate,
                PeriodType = "daily",
                TotalOrders = orders.Count,
                TotalRevenue = totalRevenue,
                TotalCOGS = totalCOGS,
                TotalAdsSpend = adsSpend,
                GrossProfit = grossProfit,
                NetProfit = netProfit,
                TotalRefunds = refunds,
                NewCustomers = newCustomers,
                ReturningCustomers = returningCustomers,
                TotalUnitsSold = totalUnitsSold,
                AverageOrderValue = averageOrderValue,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Snapshot generated successfully for {Date}", date);
    }

    public async Task<List<AnalyticsSnapshotDto>> GetSnapshotsAsync(string shopDomain, AnalyticsTimePeriod period)
    {
        var (startDate, endDate) = GetDateRange(period);

        var snapshots = await _context.AnalyticsSnapshots
            .Where(s => s.ShopDomain == shopDomain &&
                       s.SnapshotDate >= startDate &&
                       s.SnapshotDate <= endDate)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync();

        return snapshots.Select(s => new AnalyticsSnapshotDto(
            Id: s.Id,
            SnapshotDate: s.SnapshotDate,
            PeriodType: s.PeriodType,
            TotalOrders: s.TotalOrders,
            TotalRevenue: s.TotalRevenue,
            TotalCOGS: s.TotalCOGS,
            TotalAdsSpend: s.TotalAdsSpend,
            GrossProfit: s.GrossProfit,
            NetProfit: s.NetProfit,
            TotalRefunds: s.TotalRefunds,
            NewCustomers: s.NewCustomers,
            ReturningCustomers: s.ReturningCustomers,
            TotalUnitsSold: s.TotalUnitsSold,
            AverageOrderValue: s.AverageOrderValue,
            ConversionRate: s.ConversionRate
        )).ToList();
    }

    // ==================== Helper Methods ====================

    private async Task<decimal> CalculateCOGSForOrderLines(string shopDomain, List<OrderLine> orderLines)
    {
        decimal totalCOGS = 0;

        foreach (var orderLine in orderLines)
        {
            if (!orderLine.PlatformProductId.HasValue) continue;

            decimal? unitCOGS = null;

            // Try to get COGS from variant first
            if (orderLine.PlatformVariantId.HasValue)
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.PlatformVariantId == orderLine.PlatformVariantId.Value);

                unitCOGS = variant?.CostOfGoodsSold;
            }

            // Fall back to product COGS if variant COGS not found
            if (!unitCOGS.HasValue)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ShopDomain == shopDomain &&
                                             p.PlatformProductId == orderLine.PlatformProductId.Value);

                unitCOGS = product?.CostOfGoodsSold;
            }

            if (unitCOGS.HasValue)
            {
                totalCOGS += unitCOGS.Value * orderLine.Quantity;
            }
        }

        return totalCOGS;
    }

    private (DateTime startDate, DateTime endDate) GetDateRange(AnalyticsTimePeriod period)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        return period.PeriodType.ToLower() switch
        {
            "today" => (today, today.AddDays(1).AddSeconds(-1)),
            "7days" => (today.AddDays(-6), today.AddDays(1).AddSeconds(-1)),
            "30days" => (today.AddDays(-29), today.AddDays(1).AddSeconds(-1)),
            "90days" => (today.AddDays(-89), today.AddDays(1).AddSeconds(-1)),
            "12months" => (today.AddMonths(-12), today.AddDays(1).AddSeconds(-1)),
            "custom" when period.StartDate.HasValue && period.EndDate.HasValue =>
                (period.StartDate.Value.Date, period.EndDate.Value.Date.AddDays(1).AddSeconds(-1)),
            _ => (today.AddDays(-29), today.AddDays(1).AddSeconds(-1))
        };
    }

    private (DateTime startDate, DateTime endDate) GetPreviousDateRange(AnalyticsTimePeriod period)
    {
        var (currentStart, currentEnd) = GetDateRange(period);
        var daysDiff = (currentEnd - currentStart).Days;

        return (currentStart.AddDays(-daysDiff), currentStart.AddSeconds(-1));
    }

    private decimal CalculatePercentageChange(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return ((current - previous) / previous) * 100;
    }

    private int CalculatePercentageChange(int current, int previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return (int)(((decimal)(current - previous) / previous) * 100);
    }

    private string DetermineCustomerSegment(decimal totalSpent, int totalOrders, int daysSinceLastOrder, decimal? averageDaysBetweenOrders)
    {
        // Churned: No order in last 180 days
        if (daysSinceLastOrder > 180)
            return "churned";

        // At risk: No order in last 90 days but was active before
        if (daysSinceLastOrder > 90)
            return "at_risk";

        // High value: Spent over $1000 or 10+ orders
        if (totalSpent >= 1000 || totalOrders >= 10)
            return "high_value";

        // Medium value: Spent $200-$1000 or 3-9 orders
        if (totalSpent >= 200 || totalOrders >= 3)
            return "medium_value";

        // Low value: Less than above thresholds
        return "low_value";
    }

    private CustomerLifetimeValueDto MapToClvDto(CustomerLifetimeValue clv)
    {
        return new CustomerLifetimeValueDto(
            CustomerId: clv.CustomerId,
            CustomerName: $"{clv.Customer.FirstName} {clv.Customer.LastName}".Trim(),
            Email: clv.Customer.Email,
            TotalOrders: clv.TotalOrders,
            TotalSpent: clv.TotalSpent,
            AverageOrderValue: clv.AverageOrderValue,
            PredictedLifetimeValue: clv.PredictedLifetimeValue,
            Segment: clv.Segment,
            DaysSinceLastOrder: clv.DaysSinceLastOrder,
            FirstOrderDate: clv.FirstOrderDate,
            LastOrderDate: clv.LastOrderDate,
            TotalProfit: clv.TotalProfit
        );
    }
}

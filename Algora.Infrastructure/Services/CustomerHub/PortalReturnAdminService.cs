using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.CustomerHub;

/// <summary>
/// Service for managing portal return requests from the admin application
/// </summary>
public class PortalReturnAdminService : IPortalReturnAdminService
{
    private readonly PortalAdminDbContext _dbContext;
    private readonly IPortalReturnNotificationService _notificationService;
    private readonly ILogger<PortalReturnAdminService> _logger;

    public PortalReturnAdminService(
        PortalAdminDbContext dbContext,
        IPortalReturnNotificationService notificationService,
        ILogger<PortalReturnAdminService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PortalReturnPaginatedResult> GetReturnRequestsAsync(
        string shopDomain,
        string? status = null,
        string? search = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 25)
    {
        var query = _dbContext.ReturnRequests
            .Include(r => r.Items)
            .Where(r => r.ShopDomain == shopDomain)
            .AsQueryable();

        // Filter by status
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        // Filter by search (order number or email)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(r =>
                r.OrderNumber.ToLower().Contains(searchLower) ||
                r.CustomerEmail.ToLower().Contains(searchLower) ||
                r.Reason.ToLower().Contains(searchLower));
        }

        // Filter by date range
        if (startDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= endDate.Value.AddDays(1));
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Get paginated items
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new PortalReturnListDto(
                r.Id,
                r.CustomerEmail,
                r.OrderId,
                r.OrderNumber,
                r.RequestType,
                r.Status,
                r.Reason,
                r.Items.Sum(i => i.Quantity),
                r.Items.Sum(i => i.Quantity * i.UnitPrice),
                r.CreatedAt,
                r.ResolvedAt
            ))
            .ToListAsync();

        return new PortalReturnPaginatedResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PortalReturnDetailDto?> GetReturnRequestByIdAsync(string shopDomain, int requestId)
    {
        var request = await _dbContext.ReturnRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ShopDomain == shopDomain);

        if (request == null)
            return null;

        return new PortalReturnDetailDto
        {
            Id = request.Id,
            ShopDomain = request.ShopDomain,
            CustomerEmail = request.CustomerEmail,
            OrderId = request.OrderId,
            OrderNumber = request.OrderNumber,
            RequestType = request.RequestType,
            Status = request.Status,
            Reason = request.Reason,
            AdditionalComments = request.AdditionalComments,
            PreferredResolution = request.PreferredResolution,
            AdminNotes = request.AdminNotes,
            ReturnLabelUrl = request.ReturnLabelUrl,
            ReturnTrackingNumber = request.ReturnTrackingNumber,
            RefundAmount = request.RefundAmount,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            ResolvedAt = request.ResolvedAt,
            Items = request.Items.Select(i => new PortalReturnItemDto(
                i.Id,
                i.LineItemId,
                i.VariantId,
                i.Title,
                i.VariantTitle,
                i.Sku,
                i.ImageUrl,
                i.Quantity,
                i.UnitPrice,
                i.Quantity * i.UnitPrice,
                i.ItemReason,
                i.Condition
            )).ToList()
        };
    }

    public async Task<bool> UpdateReturnRequestAsync(string shopDomain, int requestId, UpdatePortalReturnDto dto)
    {
        var request = await _dbContext.ReturnRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ShopDomain == shopDomain);

        if (request == null)
            return false;

        if (!string.IsNullOrWhiteSpace(dto.Status))
            request.Status = dto.Status;

        if (dto.AdminNotes != null)
            request.AdminNotes = dto.AdminNotes;

        if (dto.ReturnLabelUrl != null)
            request.ReturnLabelUrl = dto.ReturnLabelUrl;

        if (dto.RefundAmount.HasValue)
            request.RefundAmount = dto.RefundAmount;

        request.UpdatedAt = DateTime.UtcNow;

        // Set resolved date for terminal statuses
        if (dto.Status == PortalReturnStatuses.Completed ||
            dto.Status == PortalReturnStatuses.Rejected ||
            dto.Status == PortalReturnStatuses.Cancelled)
        {
            request.ResolvedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Updated portal return request {RequestId} to status {Status}", requestId, dto.Status);

        return true;
    }

    public async Task<bool> ApproveReturnRequestAsync(string shopDomain, int requestId, string? adminNotes = null, string? returnLabelUrl = null)
    {
        var request = await _dbContext.ReturnRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ShopDomain == shopDomain);

        if (request == null || request.Status != PortalReturnStatuses.Pending)
            return false;

        request.Status = PortalReturnStatuses.Approved;
        request.AdminNotes = adminNotes;
        request.ReturnLabelUrl = returnLabelUrl;
        request.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Approved portal return request {RequestId}", requestId);

        // Send email notification
        try
        {
            var returnDetail = await GetReturnRequestByIdAsync(shopDomain, requestId);
            if (returnDetail != null)
            {
                await _notificationService.SendReturnApprovedNotificationAsync(shopDomain, returnDetail, returnLabelUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send approval notification for return request {RequestId}", requestId);
        }

        return true;
    }

    public async Task<bool> RejectReturnRequestAsync(string shopDomain, int requestId, string reason)
    {
        var request = await _dbContext.ReturnRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ShopDomain == shopDomain);

        if (request == null || request.Status != PortalReturnStatuses.Pending)
            return false;

        request.Status = PortalReturnStatuses.Rejected;
        request.AdminNotes = reason;
        request.UpdatedAt = DateTime.UtcNow;
        request.ResolvedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Rejected portal return request {RequestId}", requestId);

        // Send email notification
        try
        {
            var returnDetail = await GetReturnRequestByIdAsync(shopDomain, requestId);
            if (returnDetail != null)
            {
                await _notificationService.SendReturnRejectedNotificationAsync(shopDomain, returnDetail, reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rejection notification for return request {RequestId}", requestId);
        }

        return true;
    }

    public async Task<bool> CompleteReturnRequestAsync(string shopDomain, int requestId, decimal refundAmount, string? adminNotes = null)
    {
        var request = await _dbContext.ReturnRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ShopDomain == shopDomain);

        if (request == null ||
            (request.Status != PortalReturnStatuses.Approved && request.Status != PortalReturnStatuses.Processing))
            return false;

        request.Status = PortalReturnStatuses.Completed;
        request.RefundAmount = refundAmount;
        if (!string.IsNullOrWhiteSpace(adminNotes))
            request.AdminNotes = adminNotes;
        request.UpdatedAt = DateTime.UtcNow;
        request.ResolvedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Completed portal return request {RequestId} with refund amount {RefundAmount}", requestId, refundAmount);

        // Send email notification
        try
        {
            var returnDetail = await GetReturnRequestByIdAsync(shopDomain, requestId);
            if (returnDetail != null)
            {
                await _notificationService.SendReturnCompletedNotificationAsync(shopDomain, returnDetail, refundAmount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send completion notification for return request {RequestId}", requestId);
        }

        return true;
    }

    public async Task<PortalReturnStatsDto> GetReturnStatsAsync(string shopDomain)
    {
        var requests = await _dbContext.ReturnRequests
            .Include(r => r.Items)
            .Where(r => r.ShopDomain == shopDomain)
            .ToListAsync();

        var totalRefunded = requests
            .Where(r => r.Status == PortalReturnStatuses.Completed && r.RefundAmount.HasValue)
            .Sum(r => r.RefundAmount!.Value);

        var pendingValue = requests
            .Where(r => r.Status == PortalReturnStatuses.Pending ||
                       r.Status == PortalReturnStatuses.Approved ||
                       r.Status == PortalReturnStatuses.Processing)
            .Sum(r => r.Items.Sum(i => i.Quantity * i.UnitPrice));

        return new PortalReturnStatsDto(
            TotalRequests: requests.Count,
            PendingRequests: requests.Count(r => r.Status == PortalReturnStatuses.Pending),
            ApprovedRequests: requests.Count(r => r.Status == PortalReturnStatuses.Approved),
            ProcessingRequests: requests.Count(r => r.Status == PortalReturnStatuses.Processing),
            CompletedRequests: requests.Count(r => r.Status == PortalReturnStatuses.Completed),
            RejectedRequests: requests.Count(r => r.Status == PortalReturnStatuses.Rejected),
            TotalRefundedAmount: totalRefunded,
            PendingRefundValue: pendingValue
        );
    }
}

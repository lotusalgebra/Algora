using Algora.Application.DTOs.Inventory;
using Algora.Application.DTOs.Returns;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for managing return requests and the return portal.
/// </summary>
public class ReturnService : IReturnService
{
    private readonly AppDbContext _db;
    private readonly IShippoService _shippoService;
    private readonly ILogger<ReturnService> _logger;

    public ReturnService(
        AppDbContext db,
        IShippoService shippoService,
        ILogger<ReturnService> logger)
    {
        _db = db;
        _shippoService = shippoService;
        _logger = logger;
    }

    #region Return Requests

    public async Task<ReturnRequestDto> CreateReturnRequestAsync(string shopDomain, CreateReturnRequestDto dto)
    {
        // Get the order
        Order? order;
        if (dto.OrderId > 0)
        {
            order = await _db.Orders
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.ShopDomain == shopDomain);
        }
        else if (!string.IsNullOrEmpty(dto.OrderNumber) && !string.IsNullOrEmpty(dto.CustomerEmail))
        {
            order = await _db.Orders
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.ShopDomain == shopDomain &&
                                          o.OrderNumber == dto.OrderNumber &&
                                          o.CustomerEmail == dto.CustomerEmail);
        }
        else
        {
            throw new InvalidOperationException("Order ID or Order Number with Email is required");
        }

        if (order == null)
            throw new InvalidOperationException("Order not found");

        // Get reason
        var reason = await _db.ReturnReasons
            .FirstOrDefaultAsync(r => r.ShopDomain == shopDomain && r.Code == dto.ReasonCode && r.IsActive);

        // Generate request number
        var requestNumber = await GenerateRequestNumberAsync(shopDomain);

        // Create return request
        var returnRequest = new ReturnRequest
        {
            ShopDomain = shopDomain,
            OrderId = order.Id,
            PlatformOrderId = order.PlatformOrderId,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerEmail = order.CustomerEmail,
            CustomerName = order.Customer != null
                ? $"{order.Customer.FirstName} {order.Customer.LastName}".Trim()
                : order.CustomerEmail,
            RequestNumber = requestNumber,
            Status = "pending",
            ReturnReasonId = reason?.Id,
            ReasonCode = dto.ReasonCode,
            ReasonDescription = reason?.DisplayText ?? dto.ReasonCode,
            CustomerNote = dto.CustomerNote,
            Currency = order.Currency,
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        // Add return items
        decimal totalRefundAmount = 0;
        foreach (var itemDto in dto.Items)
        {
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == itemDto.OrderLineId);
            if (orderLine == null) continue;

            var refundAmount = orderLine.UnitPrice * itemDto.Quantity;
            totalRefundAmount += refundAmount;

            var returnItem = new ReturnItem
            {
                OrderLineId = orderLine.Id,
                PlatformProductId = orderLine.PlatformProductId,
                PlatformVariantId = orderLine.PlatformVariantId,
                ProductTitle = orderLine.ProductTitle,
                VariantTitle = orderLine.VariantTitle,
                Sku = orderLine.Sku,
                QuantityOrdered = orderLine.Quantity,
                QuantityReturned = itemDto.Quantity,
                UnitPrice = orderLine.UnitPrice,
                RefundAmount = refundAmount,
                ReturnReasonId = !string.IsNullOrEmpty(itemDto.ReasonCode)
                    ? (await _db.ReturnReasons.FirstOrDefaultAsync(r => r.ShopDomain == shopDomain && r.Code == itemDto.ReasonCode))?.Id
                    : null,
                CustomerNote = itemDto.CustomerNote,
                CreatedAt = DateTime.UtcNow
            };

            returnRequest.Items.Add(returnItem);
        }

        returnRequest.TotalRefundAmount = totalRefundAmount;

        _db.ReturnRequests.Add(returnRequest);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created return request {RequestNumber} for order {OrderNumber}",
            requestNumber, order.OrderNumber);

        // Check for auto-approval
        if (await EvaluateAutoApprovalAsync(returnRequest.Id))
        {
            returnRequest.IsAutoApproved = true;
            returnRequest.Status = "approved";
            returnRequest.ApprovedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Auto-approved return request {RequestNumber}", requestNumber);
        }

        return await GetReturnRequestAsync(returnRequest.Id) ?? throw new InvalidOperationException("Failed to create return request");
    }

    public async Task<ReturnRequestDto?> GetReturnRequestAsync(int returnRequestId)
    {
        var request = await _db.ReturnRequests
            .Include(r => r.Items)
            .Include(r => r.ReturnLabel)
            .Include(r => r.ReturnReason)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId);

        return request != null ? MapToDto(request) : null;
    }

    public async Task<ReturnRequestDto?> GetReturnRequestByNumberAsync(string shopDomain, string requestNumber)
    {
        var request = await _db.ReturnRequests
            .Include(r => r.Items)
            .Include(r => r.ReturnLabel)
            .Include(r => r.ReturnReason)
            .FirstOrDefaultAsync(r => r.ShopDomain == shopDomain && r.RequestNumber == requestNumber);

        return request != null ? MapToDto(request) : null;
    }

    public async Task<PaginatedResult<ReturnRequestDto>> GetReturnRequestsAsync(
        string shopDomain,
        string? status = null,
        string? searchTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _db.ReturnRequests
            .Include(r => r.Items)
            .Where(r => r.ShopDomain == shopDomain)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(r =>
                r.RequestNumber.Contains(searchTerm) ||
                r.OrderNumber.Contains(searchTerm) ||
                r.CustomerEmail.Contains(searchTerm) ||
                r.CustomerName.Contains(searchTerm));

        if (startDate.HasValue)
            query = query.Where(r => r.RequestedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.RequestedAt <= endDate.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<ReturnRequestDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ReturnSummaryDto> GetReturnSummaryAsync(string shopDomain)
    {
        var returns = await _db.ReturnRequests
            .Where(r => r.ShopDomain == shopDomain)
            .ToListAsync();

        var recentReturns = await _db.ReturnRequests
            .Include(r => r.Items)
            .Where(r => r.ShopDomain == shopDomain)
            .OrderByDescending(r => r.RequestedAt)
            .Take(10)
            .ToListAsync();

        return new ReturnSummaryDto
        {
            PendingCount = returns.Count(r => r.Status == "pending"),
            ApprovedCount = returns.Count(r => r.Status == "approved"),
            ShippedCount = returns.Count(r => r.Status == "shipped"),
            ReceivedCount = returns.Count(r => r.Status == "received"),
            TotalCount = returns.Count,
            TotalRefundAmount = returns.Where(r => r.Status == "refunded").Sum(r => r.TotalRefundAmount),
            TotalShippingCost = returns.Sum(r => r.ShippingCost),
            RecentReturns = recentReturns.Select(MapToDto).ToList()
        };
    }

    #endregion

    #region Status Management

    public async Task<ReturnRequestDto> ApproveReturnAsync(int returnRequestId, string? note = null)
    {
        var request = await _db.ReturnRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId);

        if (request == null)
            throw new InvalidOperationException($"Return request {returnRequestId} not found");

        if (request.Status != "pending")
            throw new InvalidOperationException($"Cannot approve return with status {request.Status}");

        request.Status = "approved";
        request.ApprovedAt = DateTime.UtcNow;
        request.ApprovalNote = note;
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Approved return request {RequestNumber}", request.RequestNumber);

        return await GetReturnRequestAsync(returnRequestId) ?? throw new InvalidOperationException("Failed to approve return");
    }

    public async Task<ReturnRequestDto> RejectReturnAsync(int returnRequestId, string reason)
    {
        var request = await _db.ReturnRequests.FindAsync(returnRequestId);

        if (request == null)
            throw new InvalidOperationException($"Return request {returnRequestId} not found");

        if (request.Status != "pending")
            throw new InvalidOperationException($"Cannot reject return with status {request.Status}");

        request.Status = "rejected";
        request.RejectedAt = DateTime.UtcNow;
        request.RejectionReason = reason;
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Rejected return request {RequestNumber}: {Reason}", request.RequestNumber, reason);

        return await GetReturnRequestAsync(returnRequestId) ?? throw new InvalidOperationException("Failed to reject return");
    }

    public async Task<ReturnRequestDto> MarkAsShippedAsync(int returnRequestId, string? trackingNumber = null, string? carrier = null)
    {
        var request = await _db.ReturnRequests
            .Include(r => r.ReturnLabel)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId);

        if (request == null)
            throw new InvalidOperationException($"Return request {returnRequestId} not found");

        if (request.Status != "approved")
            throw new InvalidOperationException($"Cannot mark as shipped - status is {request.Status}");

        request.Status = "shipped";
        request.ShippedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(trackingNumber))
            request.TrackingNumber = trackingNumber;

        if (!string.IsNullOrEmpty(carrier))
            request.TrackingCarrier = carrier;

        // Mark label as used
        if (request.ReturnLabel != null)
        {
            request.ReturnLabel.Status = "used";
            request.ReturnLabel.UsedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Marked return request {RequestNumber} as shipped", request.RequestNumber);

        return await GetReturnRequestAsync(returnRequestId) ?? throw new InvalidOperationException("Failed to update return");
    }

    public async Task<ReturnRequestDto> MarkAsReceivedAsync(int returnRequestId, List<ReturnItemConditionDto>? itemConditions = null)
    {
        var request = await _db.ReturnRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId);

        if (request == null)
            throw new InvalidOperationException($"Return request {returnRequestId} not found");

        if (request.Status != "shipped")
            throw new InvalidOperationException($"Cannot mark as received - status is {request.Status}");

        request.Status = "received";
        request.ReceivedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        // Update item conditions if provided
        if (itemConditions != null)
        {
            foreach (var condition in itemConditions)
            {
                var item = request.Items.FirstOrDefault(i => i.Id == condition.ReturnItemId);
                if (item != null)
                {
                    item.Condition = condition.Condition;
                    item.ConditionNote = condition.ConditionNote;
                    item.Restock = condition.Restock;
                }
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Marked return request {RequestNumber} as received", request.RequestNumber);

        return await GetReturnRequestAsync(returnRequestId) ?? throw new InvalidOperationException("Failed to update return");
    }

    public async Task<ReturnRequestDto> ProcessRefundAsync(int returnRequestId)
    {
        var request = await _db.ReturnRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId);

        if (request == null)
            throw new InvalidOperationException($"Return request {returnRequestId} not found");

        if (request.Status != "received")
            throw new InvalidOperationException($"Cannot process refund - status is {request.Status}");

        // Create refund record
        var refund = new Refund
        {
            OrderId = request.OrderId,
            Amount = request.TotalRefundAmount,
            Currency = request.Currency,
            Reason = request.ReasonDescription,
            Note = $"Return request {request.RequestNumber}",
            RefundedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.Refunds.Add(refund);
        await _db.SaveChangesAsync();

        // Update return request
        request.Status = "refunded";
        request.RefundId = refund.Id;
        request.RefundedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        // Mark items as restocked
        foreach (var item in request.Items.Where(i => i.Restock))
        {
            item.Restocked = true;
            item.RestockedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Processed refund for return request {RequestNumber}: ${Amount}",
            request.RequestNumber, request.TotalRefundAmount);

        return await GetReturnRequestAsync(returnRequestId) ?? throw new InvalidOperationException("Failed to process refund");
    }

    public async Task<ReturnRequestDto> CancelReturnAsync(int returnRequestId, string reason)
    {
        var request = await _db.ReturnRequests
            .Include(r => r.ReturnLabel)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId);

        if (request == null)
            throw new InvalidOperationException($"Return request {returnRequestId} not found");

        if (request.Status == "refunded")
            throw new InvalidOperationException("Cannot cancel a refunded return");

        request.Status = "cancelled";
        request.RejectionReason = reason;
        request.UpdatedAt = DateTime.UtcNow;

        // Void the label if not used
        if (request.ReturnLabelId.HasValue && request.ReturnLabel?.Status == "created")
        {
            await _shippoService.VoidLabelAsync(request.ReturnLabelId.Value);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Cancelled return request {RequestNumber}: {Reason}", request.RequestNumber, reason);

        return await GetReturnRequestAsync(returnRequestId) ?? throw new InvalidOperationException("Failed to cancel return");
    }

    #endregion

    #region Customer Portal

    public async Task<CustomerReturnEligibilityDto> CheckReturnEligibilityAsync(string shopDomain, int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ShopDomain == shopDomain);

        if (order == null)
        {
            return new CustomerReturnEligibilityDto
            {
                IsEligible = false,
                IneligibleReason = "Order not found"
            };
        }

        return await BuildEligibilityDto(shopDomain, order);
    }

    public async Task<CustomerReturnEligibilityDto?> CheckReturnEligibilityByOrderNumberAsync(
        string shopDomain, string orderNumber, string email)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.ShopDomain == shopDomain &&
                                      o.OrderNumber == orderNumber &&
                                      o.CustomerEmail.ToLower() == email.ToLower());

        if (order == null)
            return null;

        return await BuildEligibilityDto(shopDomain, order);
    }

    private async Task<CustomerReturnEligibilityDto> BuildEligibilityDto(string shopDomain, Order order)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);

        // Check if fulfilled
        var fulfillment = await _db.Fulfillments
            .Where(f => f.OrderId == order.Id && f.DeliveredAt != null)
            .OrderByDescending(f => f.DeliveredAt)
            .FirstOrDefaultAsync();

        DateTime? deliveredAt = fulfillment?.DeliveredAt;
        DateTime? returnDeadline = null;
        int daysRemaining = 0;

        if (settings.RequireDeliveryConfirmation && deliveredAt == null)
        {
            return new CustomerReturnEligibilityDto
            {
                IsEligible = false,
                IneligibleReason = "Order has not been delivered yet",
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate
            };
        }

        if (deliveredAt.HasValue)
        {
            returnDeadline = deliveredAt.Value.AddDays(settings.ReturnWindowDays);
            daysRemaining = Math.Max(0, (returnDeadline.Value - DateTime.UtcNow).Days);

            if (DateTime.UtcNow > returnDeadline)
            {
                return new CustomerReturnEligibilityDto
                {
                    IsEligible = false,
                    IneligibleReason = $"Return window has expired (was {settings.ReturnWindowDays} days from delivery)",
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    OrderDate = order.OrderDate,
                    DeliveredAt = deliveredAt,
                    ReturnDeadline = returnDeadline
                };
            }
        }

        // Get already returned quantities
        var existingReturns = await _db.ReturnItems
            .Include(ri => ri.ReturnRequest)
            .Where(ri => ri.ReturnRequest.OrderId == order.Id &&
                         ri.ReturnRequest.Status != "rejected" &&
                         ri.ReturnRequest.Status != "cancelled")
            .ToListAsync();

        var returnedByLine = existingReturns
            .GroupBy(ri => ri.OrderLineId)
            .ToDictionary(g => g.Key ?? 0, g => g.Sum(ri => ri.QuantityReturned));

        var eligibleItems = order.Lines
            .Select(line =>
            {
                var alreadyReturned = returnedByLine.GetValueOrDefault(line.Id, 0);
                return new EligibleOrderLineDto
                {
                    OrderLineId = line.Id,
                    PlatformProductId = line.PlatformProductId,
                    PlatformVariantId = line.PlatformVariantId,
                    ProductTitle = line.ProductTitle,
                    VariantTitle = line.VariantTitle,
                    Sku = line.Sku,
                    QuantityOrdered = line.Quantity,
                    QuantityAlreadyReturned = alreadyReturned,
                    QuantityReturnable = Math.Max(0, line.Quantity - alreadyReturned),
                    UnitPrice = line.UnitPrice
                };
            })
            .Where(item => item.QuantityReturnable > 0)
            .ToList();

        if (eligibleItems.Count == 0)
        {
            return new CustomerReturnEligibilityDto
            {
                IsEligible = false,
                IneligibleReason = "All items have already been returned",
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                DeliveredAt = deliveredAt,
                ReturnDeadline = returnDeadline
            };
        }

        return new CustomerReturnEligibilityDto
        {
            IsEligible = true,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            DeliveredAt = deliveredAt,
            ReturnDeadline = returnDeadline,
            DaysRemaining = daysRemaining,
            EligibleItems = eligibleItems
        };
    }

    #endregion

    #region Return Reasons

    public async Task<List<ReturnReasonDto>> GetActiveReasonsAsync(string shopDomain)
    {
        await EnsureDefaultReasonsAsync(shopDomain);

        var reasons = await _db.ReturnReasons
            .Where(r => r.ShopDomain == shopDomain && r.IsActive)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();

        return reasons.Select(MapReasonToDto).ToList();
    }

    public async Task<List<ReturnReasonDto>> GetAllReasonsAsync(string shopDomain)
    {
        await EnsureDefaultReasonsAsync(shopDomain);

        var reasons = await _db.ReturnReasons
            .Where(r => r.ShopDomain == shopDomain)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();

        return reasons.Select(MapReasonToDto).ToList();
    }

    public async Task<ReturnReasonDto> CreateReasonAsync(string shopDomain, CreateReturnReasonDto dto)
    {
        var reason = new ReturnReason
        {
            ShopDomain = shopDomain,
            Code = dto.Code,
            DisplayText = dto.DisplayText,
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            RequiresNote = dto.RequiresNote,
            IsDefect = dto.IsDefect,
            EligibleForAutoApproval = dto.EligibleForAutoApproval,
            CreatedAt = DateTime.UtcNow
        };

        _db.ReturnReasons.Add(reason);
        await _db.SaveChangesAsync();

        return MapReasonToDto(reason);
    }

    public async Task<ReturnReasonDto> UpdateReasonAsync(int reasonId, CreateReturnReasonDto dto)
    {
        var reason = await _db.ReturnReasons.FindAsync(reasonId);
        if (reason == null)
            throw new InvalidOperationException($"Return reason {reasonId} not found");

        reason.Code = dto.Code;
        reason.DisplayText = dto.DisplayText;
        reason.Description = dto.Description;
        reason.DisplayOrder = dto.DisplayOrder;
        reason.IsActive = dto.IsActive;
        reason.RequiresNote = dto.RequiresNote;
        reason.IsDefect = dto.IsDefect;
        reason.EligibleForAutoApproval = dto.EligibleForAutoApproval;
        reason.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return MapReasonToDto(reason);
    }

    public async Task DeleteReasonAsync(int reasonId)
    {
        var reason = await _db.ReturnReasons.FindAsync(reasonId);
        if (reason != null)
        {
            _db.ReturnReasons.Remove(reason);
            await _db.SaveChangesAsync();
        }
    }

    public async Task SeedDefaultReasonsAsync(string shopDomain)
    {
        await EnsureDefaultReasonsAsync(shopDomain);
    }

    private async Task EnsureDefaultReasonsAsync(string shopDomain)
    {
        var existingCount = await _db.ReturnReasons.CountAsync(r => r.ShopDomain == shopDomain);
        if (existingCount > 0) return;

        var defaults = new[]
        {
            new ReturnReason { Code = "wrong_size", DisplayText = "Wrong size or fit", DisplayOrder = 1, EligibleForAutoApproval = true },
            new ReturnReason { Code = "not_as_described", DisplayText = "Not as described", DisplayOrder = 2, RequiresNote = true, EligibleForAutoApproval = true },
            new ReturnReason { Code = "defective", DisplayText = "Defective or damaged", DisplayOrder = 3, RequiresNote = true, IsDefect = true, EligibleForAutoApproval = true },
            new ReturnReason { Code = "changed_mind", DisplayText = "Changed my mind", DisplayOrder = 4, EligibleForAutoApproval = true },
            new ReturnReason { Code = "wrong_item", DisplayText = "Wrong item sent", DisplayOrder = 5, RequiresNote = true, EligibleForAutoApproval = true },
            new ReturnReason { Code = "better_price", DisplayText = "Found better price", DisplayOrder = 6, EligibleForAutoApproval = false },
            new ReturnReason { Code = "other", DisplayText = "Other", DisplayOrder = 7, RequiresNote = true, EligibleForAutoApproval = false }
        };

        foreach (var reason in defaults)
        {
            reason.ShopDomain = shopDomain;
            reason.IsActive = true;
            reason.CreatedAt = DateTime.UtcNow;
        }

        _db.ReturnReasons.AddRange(defaults);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded default return reasons for shop {Shop}", shopDomain);
    }

    #endregion

    #region Settings

    public async Task<ReturnSettingsDto> GetSettingsAsync(string shopDomain)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);
        return MapSettingsToDto(settings);
    }

    public async Task<ReturnSettingsDto> UpdateSettingsAsync(string shopDomain, UpdateReturnSettingsDto dto)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);

        if (dto.IsEnabled.HasValue) settings.IsEnabled = dto.IsEnabled.Value;
        if (dto.AllowSelfService.HasValue) settings.AllowSelfService = dto.AllowSelfService.Value;
        if (dto.ReturnWindowDays.HasValue) settings.ReturnWindowDays = dto.ReturnWindowDays.Value;
        if (dto.RequireDeliveryConfirmation.HasValue) settings.RequireDeliveryConfirmation = dto.RequireDeliveryConfirmation.Value;
        if (dto.LabelExpirationDays.HasValue) settings.LabelExpirationDays = dto.LabelExpirationDays.Value;
        if (dto.AutoApprovalEnabled.HasValue) settings.AutoApprovalEnabled = dto.AutoApprovalEnabled.Value;
        if (dto.AutoApprovalMaxAmount.HasValue) settings.AutoApprovalMaxAmount = dto.AutoApprovalMaxAmount.Value;
        if (dto.AutoApprovalRequireReason.HasValue) settings.AutoApprovalRequireReason = dto.AutoApprovalRequireReason.Value;
        if (dto.ShippoApiKey != null) settings.ShippoApiKey = dto.ShippoApiKey;
        if (dto.StorePayShipping.HasValue) settings.StorePayShipping = dto.StorePayShipping.Value;
        if (dto.DefaultCarrier != null) settings.DefaultCarrier = dto.DefaultCarrier;
        if (dto.DefaultServiceLevel != null) settings.DefaultServiceLevel = dto.DefaultServiceLevel;
        if (dto.EmailNotificationsEnabled.HasValue) settings.EmailNotificationsEnabled = dto.EmailNotificationsEnabled.Value;
        if (dto.SmsNotificationsEnabled.HasValue) settings.SmsNotificationsEnabled = dto.SmsNotificationsEnabled.Value;
        if (dto.NotificationEmail != null) settings.NotificationEmail = dto.NotificationEmail;
        if (dto.PageTitle != null) settings.PageTitle = dto.PageTitle;
        if (dto.PolicyText != null) settings.PolicyText = dto.PolicyText;
        if (dto.LogoUrl != null) settings.LogoUrl = dto.LogoUrl;
        if (dto.PrimaryColor != null) settings.PrimaryColor = dto.PrimaryColor;

        if (dto.ReturnAddress != null)
        {
            settings.ReturnAddressName = dto.ReturnAddress.Name;
            settings.ReturnAddressCompany = dto.ReturnAddress.Company;
            settings.ReturnAddressStreet1 = dto.ReturnAddress.Street1;
            settings.ReturnAddressStreet2 = dto.ReturnAddress.Street2;
            settings.ReturnAddressCity = dto.ReturnAddress.City;
            settings.ReturnAddressState = dto.ReturnAddress.State;
            settings.ReturnAddressZip = dto.ReturnAddress.Zip;
            settings.ReturnAddressCountry = dto.ReturnAddress.Country;
            settings.ReturnAddressPhone = dto.ReturnAddress.Phone;
            settings.ReturnAddressEmail = dto.ReturnAddress.Email;
        }

        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated return settings for shop {Shop}", shopDomain);

        return MapSettingsToDto(settings);
    }

    private async Task<ReturnSettings> GetOrCreateSettingsAsync(string shopDomain)
    {
        var settings = await _db.ReturnSettings.FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings == null)
        {
            settings = new ReturnSettings
            {
                ShopDomain = shopDomain,
                PageTitle = "Return Portal",
                PolicyText = "We want you to be completely satisfied with your purchase. If you're not happy with your order, you can return it within our return window for a full refund.",
                CreatedAt = DateTime.UtcNow
            };
            _db.ReturnSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        return settings;
    }

    #endregion

    #region Analytics

    public async Task<ReturnAnalyticsDto> GetAnalyticsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _db.ReturnRequests
            .Include(r => r.Items)
            .Where(r => r.ShopDomain == shopDomain)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(r => r.RequestedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.RequestedAt <= endDate.Value);

        var returns = await query.ToListAsync();

        var totalOrders = await _db.Orders
            .Where(o => o.ShopDomain == shopDomain)
            .Where(o => !startDate.HasValue || o.OrderDate >= startDate.Value)
            .Where(o => !endDate.HasValue || o.OrderDate <= endDate.Value)
            .CountAsync();

        var returnsByReason = returns
            .GroupBy(r => r.ReasonDescription)
            .ToDictionary(g => g.Key, g => g.Count());

        var returnsByStatus = returns
            .GroupBy(r => r.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var topProducts = returns
            .SelectMany(r => r.Items)
            .GroupBy(i => new { i.PlatformProductId, i.ProductTitle })
            .Select(g => new TopReturnedProductDto
            {
                PlatformProductId = g.Key.PlatformProductId,
                ProductTitle = g.Key.ProductTitle,
                ReturnCount = g.Count(),
                TotalQuantityReturned = g.Sum(i => i.QuantityReturned),
                TotalRefundAmount = g.Sum(i => i.RefundAmount)
            })
            .OrderByDescending(p => p.ReturnCount)
            .Take(10)
            .ToList();

        var completedReturns = returns.Where(r => r.Status == "refunded" && r.ApprovedAt.HasValue && r.RefundedAt.HasValue).ToList();
        var avgProcessingDays = completedReturns.Count > 0
            ? (decimal)completedReturns.Average(r => (r.RefundedAt!.Value - r.ApprovedAt!.Value).TotalDays)
            : 0;

        return new ReturnAnalyticsDto
        {
            TotalReturns = returns.Count,
            PendingReturns = returns.Count(r => r.Status == "pending"),
            ApprovedReturns = returns.Count(r => r.Status == "approved"),
            ShippedReturns = returns.Count(r => r.Status == "shipped"),
            ReceivedReturns = returns.Count(r => r.Status == "received"),
            RefundedReturns = returns.Count(r => r.Status == "refunded"),
            RejectedReturns = returns.Count(r => r.Status == "rejected"),
            TotalRefundAmount = returns.Where(r => r.Status == "refunded").Sum(r => r.TotalRefundAmount),
            TotalShippingCost = returns.Sum(r => r.ShippingCost),
            ReturnRate = totalOrders > 0 ? (decimal)returns.Count / totalOrders * 100 : 0,
            AverageProcessingDays = avgProcessingDays,
            ReturnsByReason = returnsByReason,
            ReturnsByStatus = returnsByStatus,
            TopReturnedProducts = topProducts
        };
    }

    #endregion

    #region Auto-Approval

    public async Task<bool> EvaluateAutoApprovalAsync(int returnRequestId)
    {
        var request = await _db.ReturnRequests
            .Include(r => r.Items)
            .Include(r => r.ReturnReason)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId);

        if (request == null) return false;

        var settings = await GetOrCreateSettingsAsync(request.ShopDomain);

        if (!settings.AutoApprovalEnabled)
            return false;

        // Check return window
        var order = await _db.Orders.FindAsync(request.OrderId);
        if (order == null) return false;

        var fulfillment = await _db.Fulfillments
            .Where(f => f.OrderId == request.OrderId && f.DeliveredAt != null)
            .OrderByDescending(f => f.DeliveredAt)
            .FirstOrDefaultAsync();

        if (settings.RequireDeliveryConfirmation && fulfillment?.DeliveredAt == null)
            return false;

        if (fulfillment?.DeliveredAt != null)
        {
            var daysSinceDelivery = (DateTime.UtcNow - fulfillment.DeliveredAt.Value).TotalDays;
            if (daysSinceDelivery > settings.ReturnWindowDays)
                return false;
        }

        // Check amount
        if (request.TotalRefundAmount > settings.AutoApprovalMaxAmount)
            return false;

        // Check reason eligibility
        if (settings.AutoApprovalRequireReason && request.ReturnReason != null)
        {
            if (!request.ReturnReason.EligibleForAutoApproval)
                return false;
        }

        return true;
    }

    #endregion

    #region Helpers

    private async Task<string> GenerateRequestNumberAsync(string shopDomain)
    {
        var prefix = "RTN";
        var timestamp = DateTime.UtcNow.ToString("yyMMdd");
        var count = await _db.ReturnRequests.CountAsync(r => r.ShopDomain == shopDomain) + 1;
        return $"{prefix}-{timestamp}-{count:D4}";
    }

    private static ReturnRequestDto MapToDto(ReturnRequest r) => new()
    {
        Id = r.Id,
        ShopDomain = r.ShopDomain,
        RequestNumber = r.RequestNumber,
        Status = r.Status,
        OrderId = r.OrderId,
        PlatformOrderId = r.PlatformOrderId,
        OrderNumber = r.OrderNumber,
        CustomerId = r.CustomerId,
        CustomerEmail = r.CustomerEmail,
        CustomerName = r.CustomerName,
        ReturnReasonId = r.ReturnReasonId,
        ReasonCode = r.ReasonCode,
        ReasonDescription = r.ReasonDescription,
        CustomerNote = r.CustomerNote,
        IsAutoApproved = r.IsAutoApproved,
        ApprovalNote = r.ApprovalNote,
        RejectionReason = r.RejectionReason,
        TotalRefundAmount = r.TotalRefundAmount,
        ShippingCost = r.ShippingCost,
        Currency = r.Currency,
        TrackingNumber = r.TrackingNumber,
        TrackingUrl = r.TrackingUrl,
        TrackingCarrier = r.TrackingCarrier,
        RequestedAt = r.RequestedAt,
        ApprovedAt = r.ApprovedAt,
        RejectedAt = r.RejectedAt,
        ShippedAt = r.ShippedAt,
        ReceivedAt = r.ReceivedAt,
        RefundedAt = r.RefundedAt,
        ExpiresAt = r.ExpiresAt,
        Items = r.Items.Select(MapItemToDto).ToList(),
        Label = r.ReturnLabel != null ? MapLabelToDto(r.ReturnLabel) : null
    };

    private static ReturnItemDto MapItemToDto(ReturnItem i) => new()
    {
        Id = i.Id,
        OrderLineId = i.OrderLineId,
        PlatformProductId = i.PlatformProductId,
        PlatformVariantId = i.PlatformVariantId,
        ProductTitle = i.ProductTitle,
        VariantTitle = i.VariantTitle,
        Sku = i.Sku,
        ImageUrl = i.ImageUrl,
        QuantityOrdered = i.QuantityOrdered,
        QuantityReturned = i.QuantityReturned,
        UnitPrice = i.UnitPrice,
        RefundAmount = i.RefundAmount,
        ReasonCode = i.ReturnReason?.Code,
        CustomerNote = i.CustomerNote,
        Restock = i.Restock,
        Restocked = i.Restocked,
        Condition = i.Condition,
        ConditionNote = i.ConditionNote
    };

    private static ReturnLabelDto MapLabelToDto(ReturnLabel l) => new()
    {
        Id = l.Id,
        ShippoTransactionId = l.ShippoTransactionId,
        TrackingNumber = l.TrackingNumber,
        TrackingUrl = l.TrackingUrl,
        Carrier = l.Carrier,
        ServiceLevel = l.ServiceLevel,
        LabelUrl = l.LabelUrl,
        LabelFormat = l.LabelFormat,
        Cost = l.Cost,
        Currency = l.Currency,
        Status = l.Status,
        ExpiresAt = l.ExpiresAt,
        CreatedAt = l.CreatedAt
    };

    private static ReturnReasonDto MapReasonToDto(ReturnReason r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        DisplayText = r.DisplayText,
        Description = r.Description,
        DisplayOrder = r.DisplayOrder,
        IsActive = r.IsActive,
        RequiresNote = r.RequiresNote,
        IsDefect = r.IsDefect,
        EligibleForAutoApproval = r.EligibleForAutoApproval
    };

    private static ReturnSettingsDto MapSettingsToDto(ReturnSettings s) => new()
    {
        Id = s.Id,
        IsEnabled = s.IsEnabled,
        AllowSelfService = s.AllowSelfService,
        ReturnWindowDays = s.ReturnWindowDays,
        RequireDeliveryConfirmation = s.RequireDeliveryConfirmation,
        LabelExpirationDays = s.LabelExpirationDays,
        AutoApprovalEnabled = s.AutoApprovalEnabled,
        AutoApprovalMaxAmount = s.AutoApprovalMaxAmount,
        AutoApprovalRequireReason = s.AutoApprovalRequireReason,
        HasShippoApiKey = !string.IsNullOrEmpty(s.ShippoApiKey),
        StorePayShipping = s.StorePayShipping,
        DefaultCarrier = s.DefaultCarrier,
        DefaultServiceLevel = s.DefaultServiceLevel,
        ReturnAddress = new ReturnAddressDto
        {
            Name = s.ReturnAddressName,
            Company = s.ReturnAddressCompany,
            Street1 = s.ReturnAddressStreet1,
            Street2 = s.ReturnAddressStreet2,
            City = s.ReturnAddressCity,
            State = s.ReturnAddressState,
            Zip = s.ReturnAddressZip,
            Country = s.ReturnAddressCountry,
            Phone = s.ReturnAddressPhone,
            Email = s.ReturnAddressEmail
        },
        EmailNotificationsEnabled = s.EmailNotificationsEnabled,
        SmsNotificationsEnabled = s.SmsNotificationsEnabled,
        NotificationEmail = s.NotificationEmail,
        PageTitle = s.PageTitle,
        PolicyText = s.PolicyText,
        LogoUrl = s.LogoUrl,
        PrimaryColor = s.PrimaryColor
    };

    #endregion
}

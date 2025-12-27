using Algora.Application.DTOs;
using Algora.Application.DTOs.Common;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Algora.Web.Pages.AbandonedCheckouts;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAbandonedCartService _abandonedCartService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IAbandonedCartService abandonedCartService, ILogger<IndexModel> logger)
    {
        _abandonedCartService = abandonedCartService;
        _logger = logger;
    }

    public int TotalCarts { get; set; }
    public decimal PotentialRevenue { get; set; }
    public int RecoverableCarts { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Loading abandoned checkouts page");
            var carts = await _abandonedCartService.GetAllAsync();
            var cartsList = carts.ToList();
            TotalCarts = cartsList.Count;
            PotentialRevenue = cartsList.Sum(c => c.TotalPrice);
            RecoverableCarts = cartsList.Count(c => !string.IsNullOrEmpty(c.Email) || !string.IsNullOrEmpty(c.Phone));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load abandoned checkouts");
            ErrorMessage = "Failed to load abandoned checkouts. Please ensure the shop is connected.";
        }
    }

    /// <summary>
    /// AJAX handler for DataTables server-side processing.
    /// </summary>
    public async Task<IActionResult> OnGetDataAsync(
        int draw = 1,
        int start = 0,
        int length = 10,
        string? search = null,
        int sortColumn = 0,
        string sortDirection = "desc")
    {
        try
        {
            _logger.LogInformation("Fetching abandoned carts: start={Start}, length={Length}, search={Search}",
                start, length, search);

            var allCarts = await _abandonedCartService.GetAllAsync();
            var cartsList = allCarts.ToList();
            var totalRecords = cartsList.Count;

            // Apply search filter
            var filtered = cartsList.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                filtered = filtered.Where(c =>
                    (c.Email?.ToLower().Contains(searchLower) ?? false) ||
                    (c.Phone?.Contains(searchLower) ?? false) ||
                    c.Id.ToString().Contains(searchLower) ||
                    (c.Items?.Any(i => i.Title?.ToLower().Contains(searchLower) ?? false) ?? false));
            }

            var filteredList = filtered.ToList();
            var filteredCount = filteredList.Count;

            // Apply sorting
            filteredList = sortColumn switch
            {
                0 => sortDirection == "asc"
                    ? filteredList.OrderBy(c => c.Email ?? c.Phone ?? "").ToList()
                    : filteredList.OrderByDescending(c => c.Email ?? c.Phone ?? "").ToList(),
                3 => sortDirection == "asc"
                    ? filteredList.OrderBy(c => c.TotalPrice).ToList()
                    : filteredList.OrderByDescending(c => c.TotalPrice).ToList(),
                4 => sortDirection == "asc"
                    ? filteredList.OrderBy(c => c.AbandonedAt).ToList()
                    : filteredList.OrderByDescending(c => c.AbandonedAt).ToList(),
                _ => filteredList.OrderByDescending(c => c.AbandonedAt).ToList()
            };

            // Apply pagination
            var pagedData = filteredList
                .Skip(start)
                .Take(length)
                .Select(c =>
                {
                    var customerDisplay = !string.IsNullOrEmpty(c.Email)
                        ? c.Email.Split('@')[0]
                        : (!string.IsNullOrEmpty(c.Phone) ? c.Phone : "Unknown");

                    var firstItem = c.Items?.FirstOrDefault();
                    var itemCount = c.Items?.Count() ?? 0;
                    var itemDisplay = firstItem != null
                        ? (itemCount > 1 ? $"{firstItem.Title} (+{itemCount - 1})" : firstItem.Title)
                        : "No items";

                    return new
                    {
                        id = c.Id,
                        customerDisplay = customerDisplay,
                        email = c.Email,
                        phone = c.Phone,
                        itemDisplay = itemDisplay?.Length > 25 ? itemDisplay.Substring(0, 25) + "..." : itemDisplay,
                        itemTitle = firstItem?.Title ?? "",
                        itemCount = itemCount,
                        totalPrice = c.TotalPrice,
                        totalPriceFormatted = c.TotalPrice.ToString("C"),
                        abandonedAt = c.AbandonedAt?.ToString("MMM dd, h:mm tt") ?? "",
                        timeAgo = c.AbandonedAt.HasValue ? GetTimeAgo(c.AbandonedAt.Value) : "Unknown",
                        hasContact = !string.IsNullOrEmpty(c.Email) || !string.IsNullOrEmpty(c.Phone),
                        recoveryUrl = c.RecoveryUrl
                    };
                })
                .ToList();

            var response = new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredCount,
                Data = pagedData
            };

            return new JsonResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load abandoned carts data");
            return new JsonResult(new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = Enumerable.Empty<object>(),
                Error = "Failed to load abandoned carts"
            });
        }
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)}w ago";

        return dateTime.ToString("MMM dd");
    }

    public async Task<IActionResult> OnPostSendReminderAsync(long checkoutId)
    {
        try
        {
            _logger.LogInformation("Sending reminder for checkout {CheckoutId}", checkoutId);
            var result = await _abandonedCartService.SendReminderAsync(checkoutId);

            if (result)
            {
                SuccessMessage = "Reminder sent successfully!";
            }
            else
            {
                ErrorMessage = "Failed to send reminder. The checkout may no longer exist.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder for checkout {CheckoutId}", checkoutId);
            ErrorMessage = "Failed to send reminder. Please try again.";
        }

        return RedirectToPage();
    }
}

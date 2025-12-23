using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Exchanges;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IExchangeService _exchangeService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IExchangeService exchangeService,
        IShopContext shopContext,
        ILogger<DetailsModel> logger)
    {
        _exchangeService = exchangeService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public ExchangeDto? Exchange { get; set; }
    public List<ExchangeProductOptionDto> ProductOptions { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public string? Notes { get; set; }

    [BindProperty]
    public string? CancelReason { get; set; }

    [BindProperty]
    public List<ItemSelectionModel> ItemSelections { get; set; } = new();

    public class ItemSelectionModel
    {
        public int ExchangeItemId { get; set; }
        public int NewProductId { get; set; }
        public int? NewProductVariantId { get; set; }
        public string NewProductTitle { get; set; } = "";
        public string? NewVariantTitle { get; set; }
        public string? NewSku { get; set; }
        public decimal NewPrice { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadDataAsync(id);
        if (Exchange == null)
        {
            return NotFound();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        try
        {
            await _exchangeService.ApproveExchangeAsync(id, Notes);
            SuccessMessage = "Exchange approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving exchange {Id}", id);
            ErrorMessage = "Failed to approve exchange.";
        }

        await LoadDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostMarkReceivedAsync(int id)
    {
        try
        {
            await _exchangeService.MarkItemsReceivedAsync(id);
            SuccessMessage = "Items marked as received.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking items received for exchange {Id}", id);
            ErrorMessage = "Failed to mark items as received.";
        }

        await LoadDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        try
        {
            await _exchangeService.CompleteExchangeAsync(id);
            SuccessMessage = "Exchange completed successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing exchange {Id}", id);
            ErrorMessage = "Failed to complete exchange.";
        }

        await LoadDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CancelReason))
            {
                ErrorMessage = "Please provide a reason for cancellation.";
                await LoadDataAsync(id);
                return Page();
            }

            await _exchangeService.CancelExchangeAsync(id, CancelReason);
            SuccessMessage = "Exchange cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling exchange {Id}", id);
            ErrorMessage = "Failed to cancel exchange.";
        }

        await LoadDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateItemsAsync(int id)
    {
        try
        {
            if (ItemSelections == null || !ItemSelections.Any())
            {
                ErrorMessage = "Please select replacement products for each item.";
                await LoadDataAsync(id);
                return Page();
            }

            var updateDto = new UpdateExchangeItemsDto(
                ItemSelections.Select(s => new UpdateExchangeItemDto(
                    s.ExchangeItemId,
                    s.NewProductId,
                    s.NewProductVariantId,
                    s.NewProductTitle,
                    s.NewVariantTitle,
                    s.NewSku,
                    s.NewPrice
                ))
            );

            await _exchangeService.UpdateExchangeItemsAsync(id, updateDto);
            SuccessMessage = "Replacement items updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating exchange items for {Id}", id);
            ErrorMessage = "Failed to update replacement items.";
        }

        await LoadDataAsync(id);
        return Page();
    }

    private async Task LoadDataAsync(int id)
    {
        try
        {
            Exchange = await _exchangeService.GetExchangeAsync(id);
            if (Exchange != null)
            {
                var shopDomain = _shopContext.ShopDomain;
                ProductOptions = (await _exchangeService.GetExchangeOptionsAsync(shopDomain)).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading exchange {Id}", id);
            ErrorMessage = "Failed to load exchange details.";
        }
    }

    public string GetStatusColor(string status) => status switch
    {
        "pending" => "from-orange-500 to-yellow-300",
        "approved" => "from-blue-600 to-cyan-400",
        "shipped" => "from-purple-700 to-pink-500",
        "received" => "from-cyan-500 to-blue-400",
        "completed" => "from-green-600 to-lime-400",
        "cancelled" => "from-red-600 to-rose-400",
        _ => "from-gray-400 to-gray-600"
    };

    public string GetStatusIcon(string status) => status switch
    {
        "pending" => "fas fa-clock",
        "approved" => "fas fa-check",
        "shipped" => "fas fa-shipping-fast",
        "received" => "fas fa-box",
        "completed" => "fas fa-check-double",
        "cancelled" => "fas fa-times",
        _ => "fas fa-exchange-alt"
    };
}

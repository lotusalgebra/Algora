using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Exchanges;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IExchangeService _exchangeService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IExchangeService exchangeService,
        IShopContext shopContext,
        ILogger<CreateModel> logger)
    {
        _exchangeService = exchangeService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public ExchangeEligibilityDto? Eligibility { get; set; }
    public List<ExchangeProductOptionDto> ProductOptions { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? OrderId { get; set; }

    [BindProperty]
    public ExchangeFormModel ExchangeForm { get; set; } = new();

    public class ExchangeFormModel
    {
        public int OrderId { get; set; }
        public string CustomerEmail { get; set; } = "";
        public string? CustomerName { get; set; }
        public string? Notes { get; set; }
        public List<ExchangeItemFormModel> Items { get; set; } = new();
    }

    public class ExchangeItemFormModel
    {
        public bool Selected { get; set; }
        public int OrderLineId { get; set; }
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public string ProductTitle { get; set; } = "";
        public string? VariantTitle { get; set; }
        public string? Sku { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int MaxQuantity { get; set; }
        public string? Reason { get; set; }
        public string? CustomerNote { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!OrderId.HasValue)
        {
            ErrorMessage = "Please specify an order to create an exchange for.";
            return Page();
        }

        await LoadEligibilityAsync(OrderId.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostCheckEligibilityAsync()
    {
        if (!OrderId.HasValue)
        {
            ErrorMessage = "Please specify an order ID.";
            return Page();
        }

        await LoadEligibilityAsync(OrderId.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateExchangeAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;

            var selectedItems = ExchangeForm.Items.Where(i => i.Selected && i.Quantity > 0).ToList();
            if (!selectedItems.Any())
            {
                ErrorMessage = "Please select at least one item to exchange.";
                await LoadEligibilityAsync(ExchangeForm.OrderId);
                return Page();
            }

            var createDto = new CreateExchangeDto(
                shopDomain,
                ExchangeForm.OrderId,
                ExchangeForm.CustomerEmail,
                ExchangeForm.CustomerName,
                selectedItems.Select(i => new CreateExchangeItemDto(
                    i.OrderLineId,
                    i.ProductId,
                    i.ProductVariantId,
                    i.ProductTitle,
                    i.VariantTitle,
                    i.Sku,
                    i.Price,
                    i.Quantity,
                    i.Reason,
                    i.CustomerNote
                )),
                ExchangeForm.Notes
            );

            var exchange = await _exchangeService.CreateExchangeAsync(createDto);
            return RedirectToPage("./Details", new { id = exchange.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating exchange for order {OrderId}", ExchangeForm.OrderId);
            ErrorMessage = "Failed to create exchange. " + ex.Message;
            await LoadEligibilityAsync(ExchangeForm.OrderId);
            return Page();
        }
    }

    private async Task LoadEligibilityAsync(int orderId)
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            Eligibility = await _exchangeService.CheckEligibilityAsync(orderId);
            ProductOptions = (await _exchangeService.GetExchangeOptionsAsync(shopDomain)).ToList();

            if (Eligibility.IsEligible)
            {
                ExchangeForm.OrderId = orderId;
                ExchangeForm.Items = Eligibility.EligibleItems.Select(i => new ExchangeItemFormModel
                {
                    OrderLineId = i.OrderLineId,
                    ProductId = i.ProductId,
                    ProductVariantId = i.ProductVariantId,
                    ProductTitle = i.ProductTitle,
                    VariantTitle = i.VariantTitle,
                    Sku = i.Sku,
                    Price = i.Price,
                    Quantity = 0,
                    MaxQuantity = i.QuantityAvailableForExchange
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking eligibility for order {OrderId}", orderId);
            ErrorMessage = "Failed to check exchange eligibility.";
        }
    }
}

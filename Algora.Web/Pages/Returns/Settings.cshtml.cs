using Algora.Application.DTOs.Returns;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Returns;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly IReturnService _returnService;
    private readonly IShippoService _shippoService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(
        IReturnService returnService,
        IShippoService shippoService,
        IShopContext shopContext,
        ILogger<SettingsModel> logger)
    {
        _returnService = returnService;
        _shippoService = shippoService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public ReturnSettingsDto Settings { get; set; } = new();
    public bool ShippoConfigured { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public string PortalUrl => $"/returns/request?shop={_shopContext.ShopDomain}";

    [BindProperty]
    public UpdateReturnSettingsDto Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            Settings = await _returnService.GetSettingsAsync(_shopContext.ShopDomain);
            ShippoConfigured = await _shippoService.IsConfiguredAsync(_shopContext.ShopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading return settings");
            ErrorMessage = "Failed to load settings.";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await _returnService.UpdateSettingsAsync(_shopContext.ShopDomain, Input);
            SuccessMessage = "Settings saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving return settings");
            ErrorMessage = "Failed to save settings: " + ex.Message;
        }

        await OnGetAsync();
        return Page();
    }
}

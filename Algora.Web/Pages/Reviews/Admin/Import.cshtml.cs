using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Reviews.Admin;

[Authorize]
public class ImportModel : PageModel
{
    private readonly IShopContext _shopContext;
    private readonly ILogger<ImportModel> _logger;

    public ImportModel(
        IShopContext shopContext,
        ILogger<ImportModel> logger)
    {
        _shopContext = shopContext;
        _logger = logger;
    }

    public string ShopDomain => _shopContext.ShopDomain;
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }
}

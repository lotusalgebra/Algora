using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Algora.Web.Pages.Operations.Suppliers;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ISupplierService _supplierService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        ISupplierService supplierService,
        IShopContext shopContext,
        ILogger<CreateModel> logger)
    {
        _supplierService = supplierService;
        _shopContext = shopContext;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Code { get; set; }

        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        [Phone]
        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [Url]
        [StringLength(500)]
        public string? Website { get; set; }

        [Range(1, 365)]
        public int DefaultLeadTimeDays { get; set; } = 7;

        [Range(0, 1000000)]
        public decimal? MinimumOrderAmount { get; set; }

        [StringLength(100)]
        public string? PaymentTerms { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var dto = new CreateSupplierDto(
                _shopContext.ShopDomain,
                Input.Name,
                Input.Code,
                Input.Email,
                Input.Phone,
                Input.Address,
                Input.ContactPerson,
                Input.Website,
                Input.DefaultLeadTimeDays,
                Input.MinimumOrderAmount,
                Input.PaymentTerms,
                Input.Notes
            );

            await _supplierService.CreateSupplierAsync(dto);
            TempData["SuccessMessage"] = $"Supplier '{Input.Name}' created successfully.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier");
            ErrorMessage = "Failed to create supplier. Please try again.";
            return Page();
        }
    }
}

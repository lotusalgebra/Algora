using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Algora.Web.Pages.Operations.Suppliers;

[Authorize]
public class EditModel : PageModel
{
    private readonly ISupplierService _supplierService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        ISupplierService supplierService,
        ILogger<EditModel> logger)
    {
        _supplierService = supplierService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var supplier = await _supplierService.GetSupplierAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Code = supplier.Code,
                Email = supplier.Email,
                Phone = supplier.Phone,
                Address = supplier.Address,
                ContactPerson = supplier.ContactPerson,
                Website = supplier.Website,
                DefaultLeadTimeDays = supplier.DefaultLeadTimeDays,
                MinimumOrderAmount = supplier.MinimumOrderAmount,
                PaymentTerms = supplier.PaymentTerms,
                Notes = supplier.Notes,
                IsActive = supplier.IsActive
            };

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading supplier {SupplierId}", id);
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var dto = new UpdateSupplierDto(
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
                Input.Notes,
                Input.IsActive
            );

            await _supplierService.UpdateSupplierAsync(Input.Id, dto);
            TempData["SuccessMessage"] = $"Supplier '{Input.Name}' updated successfully.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier {SupplierId}", Input.Id);
            ErrorMessage = "Failed to update supplier. Please try again.";
            return Page();
        }
    }
}

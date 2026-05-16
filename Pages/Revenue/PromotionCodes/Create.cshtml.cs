using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.PromotionCodes;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public PromotionCode PromotionCode { get; set; } = new()
    {
        IsActive = true,
        ValidFrom = DateTime.Today,
        ValidTo = DateTime.Today.AddMonths(1)
    };

    public SelectList RatePlanOptions { get; set; } = default!;
    public SelectList RoomTypeOptions { get; set; } = default!;

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidatePromotion();

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        PromotionCode.Code = PromotionCode.Code.Trim().ToUpperInvariant();
        PromotionCode.ValidFrom = PromotionCode.ValidFrom.Date;
        PromotionCode.ValidTo = PromotionCode.ValidTo.Date;
        _context.PromotionCodes.Add(PromotionCode);
        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }

    private void ValidatePromotion()
    {
        if (PromotionCode.ValidTo.Date < PromotionCode.ValidFrom.Date)
        {
            ModelState.AddModelError("PromotionCode.ValidTo", "Valid to date cannot be before valid from date.");
        }

        if (PromotionCode.DiscountValue < 0)
        {
            ModelState.AddModelError("PromotionCode.DiscountValue", "Discount value cannot be negative.");
        }

        if (PromotionCode.DiscountType == DiscountType.Percentage && PromotionCode.DiscountValue > 100)
        {
            ModelState.AddModelError("PromotionCode.DiscountValue", "Percentage discount cannot exceed 100.");
        }
    }

    private async Task LoadOptionsAsync()
    {
        RatePlanOptions = new SelectList(
            await _context.RatePlans.OrderBy(r => r.Code).ToListAsync(),
            "Id",
            "Code");

        RoomTypeOptions = new SelectList(
            await _context.RoomTypes.OrderBy(r => r.Code).ToListAsync(),
            "Id",
            "Code");
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.PromotionCodes;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public PromotionCode PromotionCode { get; set; } = default!;

    public SelectList RatePlanOptions { get; set; } = default!;
    public SelectList RoomTypeOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var promotionCode = await _context.PromotionCodes.FindAsync(id);
        if (promotionCode == null)
        {
            return NotFound();
        }

        PromotionCode = promotionCode;
        await LoadOptionsAsync();
        return Page();
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
        _context.Attach(PromotionCode).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.PromotionCodes.AnyAsync(e => e.Id == PromotionCode.Id))
            {
                return NotFound();
            }

            throw;
        }

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
            "Code",
            PromotionCode.AppliesToRatePlanId);

        RoomTypeOptions = new SelectList(
            await _context.RoomTypes.OrderBy(r => r.Code).ToListAsync(),
            "Id",
            "Code",
            PromotionCode.AppliesToRoomTypeId);
    }
}

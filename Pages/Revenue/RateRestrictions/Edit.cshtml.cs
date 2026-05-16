using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RateRestrictions;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public RateRestriction RateRestriction { get; set; } = default!;

    public SelectList RatePlanOptions { get; set; } = default!;
    public SelectList RoomTypeOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var rateRestriction = await _context.RateRestrictions.FindAsync(id);
        if (rateRestriction == null)
        {
            return NotFound();
        }

        RateRestriction = rateRestriction;
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (RateRestriction.MaximumLengthOfStay.HasValue &&
            RateRestriction.MaximumLengthOfStay.Value < RateRestriction.MinimumLengthOfStay)
        {
            ModelState.AddModelError("RateRestriction.MaximumLengthOfStay", "Maximum length of stay cannot be lower than minimum length of stay.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        RateRestriction.RestrictionDate = RateRestriction.RestrictionDate.Date;
        _context.Attach(RateRestriction).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.RateRestrictions.AnyAsync(e => e.Id == RateRestriction.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToPage("Index");
    }

    private async Task LoadOptionsAsync()
    {
        RatePlanOptions = new SelectList(
            await _context.RatePlans.OrderBy(r => r.Code).ToListAsync(),
            "Id",
            "Code",
            RateRestriction.RatePlanId);

        RoomTypeOptions = new SelectList(
            await _context.RoomTypes.OrderBy(r => r.Code).ToListAsync(),
            "Id",
            "Code",
            RateRestriction.RoomTypeId);
    }
}

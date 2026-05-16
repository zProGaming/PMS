using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RateRestrictions;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public RateRestriction RateRestriction { get; set; } = new()
    {
        RestrictionDate = DateTime.Today,
        MinimumLengthOfStay = 1
    };

    public SelectList RatePlanOptions { get; set; } = default!;
    public SelectList RoomTypeOptions { get; set; } = default!;

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
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
        _context.RateRestrictions.Add(RateRestriction);
        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
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

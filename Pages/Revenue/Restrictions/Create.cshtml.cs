using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.Restrictions;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    [BindProperty] public RateRestriction RateRestriction { get; set; } = new() { RestrictionDate = DateTime.Today, MinimumLengthOfStay = 1 };
    public SelectList RatePlanOptions { get; set; } = default!;
    public SelectList RoomTypeOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateRestriction();
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(RateRestriction.RatePlanId, RateRestriction.RoomTypeId);
            return Page();
        }
        _context.RateRestrictions.Add(RateRestriction);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }

    private void ValidateRestriction()
    {
        if (RateRestriction.MinimumLengthOfStay < 1) ModelState.AddModelError("RateRestriction.MinimumLengthOfStay", "Minimum LOS must be at least 1.");
        if (RateRestriction.MaximumLengthOfStay is not null && RateRestriction.MaximumLengthOfStay < RateRestriction.MinimumLengthOfStay) ModelState.AddModelError("RateRestriction.MaximumLengthOfStay", "Maximum LOS must be greater than or equal to minimum LOS.");
    }

    private async Task LoadOptionsAsync(object? selectedRatePlan = null, object? selectedRoomType = null)
    {
        RatePlanOptions = new SelectList(await _context.RatePlans.AsNoTracking().OrderBy(ratePlan => ratePlan.Code).Select(ratePlan => new { ratePlan.Id, Name = ratePlan.Code + " - " + ratePlan.Name }).ToListAsync(), "Id", "Name", selectedRatePlan);
        RoomTypeOptions = new SelectList(await _context.RoomTypes.AsNoTracking().OrderBy(roomType => roomType.Code).Select(roomType => new { roomType.Id, Name = roomType.Code + " - " + roomType.Name }).ToListAsync(), "Id", "Name", selectedRoomType);
    }
}

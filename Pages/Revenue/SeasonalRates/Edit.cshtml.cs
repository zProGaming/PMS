using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.SeasonalRates;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    [BindProperty] public SeasonalRate SeasonalRate { get; set; } = default!;
    public SelectList RatePlanOptions { get; set; } = default!;
    public SelectList RoomTypeOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null) return NotFound();
        var rate = await _context.SeasonalRates.FindAsync(id);
        if (rate is null) return NotFound();
        SeasonalRate = rate;
        await LoadOptionsAsync(rate.RatePlanId, rate.RoomTypeId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateRate();
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(SeasonalRate.RatePlanId, SeasonalRate.RoomTypeId);
            return Page();
        }
        _context.Attach(SeasonalRate).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }

    private void ValidateRate()
    {
        if (SeasonalRate.EndDate < SeasonalRate.StartDate) ModelState.AddModelError("SeasonalRate.EndDate", "End date must be on or after start date.");
        if (SeasonalRate.Rate < 0 || SeasonalRate.ExtraAdultRate < 0 || SeasonalRate.ExtraChildRate < 0) ModelState.AddModelError(string.Empty, "Rates cannot be negative.");
    }

    private async Task LoadOptionsAsync(object? selectedRatePlan = null, object? selectedRoomType = null)
    {
        RatePlanOptions = new SelectList(await _context.RatePlans.AsNoTracking().OrderBy(ratePlan => ratePlan.Code).Select(ratePlan => new { ratePlan.Id, Name = ratePlan.Code + " - " + ratePlan.Name }).ToListAsync(), "Id", "Name", selectedRatePlan);
        RoomTypeOptions = new SelectList(await _context.RoomTypes.AsNoTracking().OrderBy(roomType => roomType.Code).Select(roomType => new { roomType.Id, Name = roomType.Code + " - " + roomType.Name }).ToListAsync(), "Id", "Name", selectedRoomType);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.SeasonalRates;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    [BindProperty] public SeasonalRate SeasonalRate { get; set; } = new() { IsActive = true, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1) };
    public SelectList RatePlanOptions { get; set; } = default!;
    public SelectList RoomTypeOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadOptionsAsync();
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
        _context.SeasonalRates.Add(SeasonalRate);
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
        RatePlanOptions = new SelectList(await _context.RatePlans.AsNoTracking().Where(ratePlan => ratePlan.IsActive).OrderBy(ratePlan => ratePlan.Code).Select(ratePlan => new { ratePlan.Id, Name = ratePlan.Code + " - " + ratePlan.Name }).ToListAsync(), "Id", "Name", selectedRatePlan);
        RoomTypeOptions = new SelectList(await _context.RoomTypes.AsNoTracking().Where(roomType => roomType.IsActive).OrderBy(roomType => roomType.Code).Select(roomType => new { roomType.Id, Name = roomType.Code + " - " + roomType.Name }).ToListAsync(), "Id", "Name", selectedRoomType);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RoomTypeRates;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public RoomTypeRate RoomTypeRate { get; set; } = new() { IsActive = true, EffectiveFrom = DateTime.Today, EffectiveTo = DateTime.Today.AddYears(1) };

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
            await LoadOptionsAsync(RoomTypeRate.RatePlanId, RoomTypeRate.RoomTypeId);
            return Page();
        }

        _context.RoomTypeRates.Add(RoomTypeRate);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }

    private void ValidateRate()
    {
        if (RoomTypeRate.EffectiveTo < RoomTypeRate.EffectiveFrom)
        {
            ModelState.AddModelError("RoomTypeRate.EffectiveTo", "Effective to must be on or after effective from.");
        }

        if (RoomTypeRate.BaseRate < 0 || RoomTypeRate.ExtraAdultRate < 0 || RoomTypeRate.ExtraChildRate < 0)
        {
            ModelState.AddModelError(string.Empty, "Rates cannot be negative.");
        }
    }

    private async Task LoadOptionsAsync(object? selectedRatePlan = null, object? selectedRoomType = null)
    {
        RatePlanOptions = new SelectList(await _context.RatePlans.AsNoTracking().Where(ratePlan => ratePlan.IsActive).OrderBy(ratePlan => ratePlan.Code).Select(ratePlan => new { ratePlan.Id, Name = ratePlan.Code + " - " + ratePlan.Name }).ToListAsync(), "Id", "Name", selectedRatePlan);
        RoomTypeOptions = new SelectList(await _context.RoomTypes.AsNoTracking().Where(roomType => roomType.IsActive).OrderBy(roomType => roomType.Code).Select(roomType => new { roomType.Id, Name = roomType.Code + " - " + roomType.Name }).ToListAsync(), "Id", "Name", selectedRoomType);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.BookingManagement.Settings;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public BookingEngineSetting Setting { get; set; } = new();

    public SelectList RatePlanOptions { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Setting = await _context.BookingEngineSettings.OrderBy(setting => setting.Id).FirstOrDefaultAsync()
            ?? new BookingEngineSetting();
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Setting.DepositPercentage < 0 || Setting.DepositPercentage > 100)
        {
            ModelState.AddModelError("Setting.DepositPercentage", "Deposit percentage must be between 0 and 100.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        Setting.UpdatedAt = DateTime.Now;

        if (Setting.Id == 0)
        {
            Setting.CreatedAt = DateTime.Now;
            _context.BookingEngineSettings.Add(Setting);
        }
        else
        {
            _context.Attach(Setting).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadOptionsAsync()
    {
        var ratePlans = await _context.RatePlans
            .AsNoTracking()
            .Where(ratePlan => ratePlan.IsActive)
            .OrderBy(ratePlan => ratePlan.Code)
            .Select(ratePlan => new { ratePlan.Id, Name = ratePlan.Code + " - " + ratePlan.Name })
            .ToListAsync();

        RatePlanOptions = new SelectList(ratePlans, "Id", "Name", Setting.DefaultRatePlanId);
    }
}

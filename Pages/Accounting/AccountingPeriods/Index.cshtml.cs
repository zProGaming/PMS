using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.AccountingPeriods;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<AccountingPeriod> Periods { get; private set; } = [];

    [BindProperty]
    public AccountingPeriod Input { get; set; } = new() { StartDate = DateTime.Today, EndDate = DateTime.Today };

    public async Task OnGetAsync() => Periods = await context.AccountingPeriods.AsNoTracking().OrderByDescending(period => period.StartDate).ToListAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.EndDate < Input.StartDate)
        {
            ModelState.AddModelError("Input.EndDate", "End date must be after start date.");
        }

        var overlaps = await context.AccountingPeriods.AnyAsync(period => Input.StartDate <= period.EndDate && Input.EndDate >= period.StartDate);
        if (overlaps)
        {
            ModelState.AddModelError(string.Empty, "Accounting periods should not overlap.");
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        Input.Status = AccountingPeriodStatus.Open;
        context.AccountingPeriods.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCloseAsync(int id)
    {
        var period = await context.AccountingPeriods.FindAsync(id);
        if (period?.Status == AccountingPeriodStatus.Open)
        {
            period.Status = AccountingPeriodStatus.Closed;
            period.ClosedBy = User.Identity?.Name ?? "System";
            period.ClosedAt = DateTime.Now;
            await context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLockAsync(int id)
    {
        var period = await context.AccountingPeriods.FindAsync(id);
        if (period?.Status == AccountingPeriodStatus.Closed)
        {
            period.Status = AccountingPeriodStatus.Locked;
            await context.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}

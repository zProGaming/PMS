using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Executive;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive.Snapshots;

public class IndexModel(ApplicationDbContext context, ExecutiveReportingService reportingService) : PageModel
{
    public IList<ExecutiveReportSnapshot> Snapshots { get; private set; } = [];

    [BindProperty]
    public SnapshotInput Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        Input.PeriodStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        Input.PeriodEnd = DateTime.Today;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        if (!PmsRoles.ExecutiveManagement.Any(role => User.IsInRole(role)))
        {
            return Forbid();
        }

        if (Input.PeriodEnd < Input.PeriodStart)
        {
            ModelState.AddModelError(string.Empty, "Period end must be on or after period start.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var userName = User.Identity?.Name ?? "Executive";
        await reportingService.GenerateSnapshotAsync(Input.PeriodStart, Input.PeriodEnd, Input.ReportType, userName);
        TempData["StatusMessage"] = "Executive snapshot generated.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Snapshots = await context.ExecutiveReportSnapshots
            .AsNoTracking()
            .OrderByDescending(snapshot => snapshot.PreparedAt)
            .Take(25)
            .ToListAsync();
    }
}

public class SnapshotInput
{
    public DateTime PeriodStart { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime PeriodEnd { get; set; } = DateTime.Today;
    public ExecutiveReportType ReportType { get; set; } = ExecutiveReportType.MonthlyOwnerReport;
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Executive;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive.Alerts;

public class IndexModel(ApplicationDbContext context, ExecutiveKPIService kpiService, ExecutiveAlertService alertService) : PageModel
{
    public IList<ExecutiveAlert> Alerts { get; private set; } = [];
    public bool IncludeResolved { get; private set; }

    public async Task OnGetAsync(bool includeResolved = false)
    {
        IncludeResolved = includeResolved;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        var summary = await kpiService.GetSummaryAsync(DateTime.Today, DateTime.Today);
        await alertService.GenerateAlertsAsync(summary);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        await alertService.ResolveAsync(id, User.Identity?.Name ?? "Executive");
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var query = context.ExecutiveAlerts.AsNoTracking();
        if (!IncludeResolved)
        {
            query = query.Where(alert => !alert.IsResolved);
        }
        Alerts = await query.OrderByDescending(alert => alert.Severity).ThenByDescending(alert => alert.AlertDate).Take(100).ToListAsync();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class CashFlowSnapshotsModel(ApplicationDbContext context, CashFlowReportService cashFlowReportService) : PageModel
{
    public IList<CashFlowReportSnapshot> Snapshots { get; private set; } = [];

    public CashFlowReportSnapshot? SelectedSnapshot { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(int? snapshotId)
    {
        await LoadAsync(snapshotId);
    }

    public async Task<IActionResult> OnPostGenerateAsync(DateTime startDate, DateTime endDate, CashFlowMethod method = CashFlowMethod.Direct)
    {
        if (endDate.Date < startDate.Date)
        {
            StatusMessage = "Date From must be before or equal Date To.";
            return RedirectToPage();
        }

        var snapshot = await cashFlowReportService.SaveSnapshotAsync(startDate, endDate, method, User.Identity?.Name);
        StatusMessage = $"Cash flow snapshot #{snapshot.Id} was generated.";
        return RedirectToPage(new { snapshotId = snapshot.Id });
    }

    private async Task LoadAsync(int? snapshotId)
    {
        Snapshots = await context.CashFlowReportSnapshots
            .AsNoTracking()
            .OrderByDescending(snapshot => snapshot.GeneratedAt)
            .Take(50)
            .ToListAsync();

        var selectedId = snapshotId ?? Snapshots.FirstOrDefault()?.Id;
        if (selectedId is not null)
        {
            SelectedSnapshot = await context.CashFlowReportSnapshots
                .AsNoTracking()
                .Include(snapshot => snapshot.Lines)
                    .ThenInclude(line => line.CashFlowCategory)
                .FirstOrDefaultAsync(snapshot => snapshot.Id == selectedId.Value);
        }
    }
}

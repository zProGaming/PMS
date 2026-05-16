using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class StatementOfCashFlowsModel(ApplicationDbContext context, CashFlowReportService cashFlowReportService) : PageModel
{
    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    public CashFlowMethod Method { get; private set; }

    public int? AccountingPeriodId { get; private set; }

    public CashFlowStatementResult Result { get; private set; } = default!;

    public SelectList PeriodOptions { get; private set; } = default!;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate, CashFlowMethod method = CashFlowMethod.Direct, int? accountingPeriodId = null)
    {
        await LoadReportAsync(startDate, endDate, method, accountingPeriodId);
    }

    public async Task<IActionResult> OnPostSnapshotAsync(DateTime startDate, DateTime endDate, CashFlowMethod method)
    {
        if (endDate.Date < startDate.Date)
        {
            StatusMessage = "Date From must be before or equal Date To.";
            return RedirectToPage(new { startDate, endDate, method });
        }

        var snapshot = await cashFlowReportService.SaveSnapshotAsync(startDate, endDate, method, User.Identity?.Name);
        StatusMessage = $"Cash flow snapshot #{snapshot.Id} was generated.";
        return RedirectToPage("/Accounting/Reports/CashFlowSnapshots", new { snapshotId = snapshot.Id });
    }

    private async Task LoadReportAsync(DateTime? startDate, DateTime? endDate, CashFlowMethod method, int? accountingPeriodId)
    {
        var today = DateTime.Today;
        StartDate = startDate?.Date ?? new DateTime(today.Year, today.Month, 1);
        EndDate = endDate?.Date ?? today;
        AccountingPeriodId = accountingPeriodId;

        if (AccountingPeriodId is not null)
        {
            var period = await context.AccountingPeriods.AsNoTracking().FirstOrDefaultAsync(item => item.Id == AccountingPeriodId.Value);
            if (period is not null)
            {
                StartDate = period.StartDate.Date;
                EndDate = period.EndDate.Date;
            }
        }

        if (EndDate < StartDate)
        {
            (StartDate, EndDate) = (EndDate, StartDate);
        }

        Method = method;
        Result = await cashFlowReportService.GenerateStatementAsync(StartDate, EndDate, Method);

        var periods = await context.AccountingPeriods
            .AsNoTracking()
            .OrderByDescending(period => period.StartDate)
            .Select(period => new { period.Id, Name = $"{period.PeriodName} ({period.Status})" })
            .ToListAsync();
        PeriodOptions = new SelectList(periods, "Id", "Name", AccountingPeriodId);
    }
}

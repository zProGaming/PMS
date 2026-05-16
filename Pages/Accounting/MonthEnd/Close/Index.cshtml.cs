using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.MonthEnd.Close;

public class IndexModel(ApplicationDbContext context, AccountsPayableService accountsPayableService) : PageModel
{
    public IList<AccountingPeriod> Periods { get; private set; } = [];
    public IList<MonthEndCloseChecklist> Checklist { get; private set; } = [];
    public SelectList PeriodOptions { get; private set; } = default!;
    public AccountingPeriod? SelectedPeriod { get; private set; }
    public int ProgressPercent { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int? AccountingPeriodId { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int id, MonthEndChecklistStatus status)
    {
        var item = await context.MonthEndCloseChecklists.FindAsync(id);
        if (item is not null)
        {
            item.Status = status;
            if (status == MonthEndChecklistStatus.Completed)
            {
                item.CompletedBy = User.Identity?.Name ?? "System";
                item.CompletedAt = DateTime.Now;
            }
            await context.SaveChangesAsync();
            AccountingPeriodId = item.AccountingPeriodId;
        }
        return RedirectToPage(new { AccountingPeriodId });
    }

    public async Task<IActionResult> OnPostCloseAsync(int accountingPeriodId, bool overrideIncomplete)
    {
        var errors = await accountsPayableService.CloseAccountingPeriodAsync(accountingPeriodId, User.Identity?.Name ?? "System", overrideIncomplete);
        StatusMessage = errors.Count == 0 ? "Accounting period closed." : string.Join(" ", errors);
        return RedirectToPage(new { AccountingPeriodId = accountingPeriodId });
    }

    private async Task LoadAsync()
    {
        Periods = await context.AccountingPeriods.AsNoTracking().OrderByDescending(period => period.StartDate).ToListAsync();
        AccountingPeriodId ??= Periods.FirstOrDefault(period => period.Status == AccountingPeriodStatus.Open)?.Id ?? Periods.FirstOrDefault()?.Id;
        if (AccountingPeriodId is not null)
        {
            await accountsPayableService.EnsureMonthEndChecklistAsync(AccountingPeriodId.Value);
            SelectedPeriod = await context.AccountingPeriods.AsNoTracking().FirstOrDefaultAsync(period => period.Id == AccountingPeriodId.Value);
            Checklist = await context.MonthEndCloseChecklists.AsNoTracking().Where(item => item.AccountingPeriodId == AccountingPeriodId.Value).OrderBy(item => item.Module).ThenBy(item => item.ChecklistItem).ToListAsync();
        }
        PeriodOptions = new SelectList(Periods, "Id", "PeriodName", AccountingPeriodId);
        ProgressPercent = Checklist.Count == 0 ? 0 : (int)Math.Round(Checklist.Count(item => item.Status is MonthEndChecklistStatus.Completed or MonthEndChecklistStatus.NotApplicable) * 100m / Checklist.Count);
    }
}

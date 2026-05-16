using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class MonthEndCloseReportModel(ApplicationDbContext context) : PageModel
{
    public IList<MonthEndCloseChecklist> Checklist { get; private set; } = [];
    public AccountingPeriod? Period { get; private set; }

    public async Task OnGetAsync(int? accountingPeriodId)
    {
        Period = accountingPeriodId is null
            ? await context.AccountingPeriods.AsNoTracking().OrderByDescending(period => period.StartDate).FirstOrDefaultAsync()
            : await context.AccountingPeriods.AsNoTracking().FirstOrDefaultAsync(period => period.Id == accountingPeriodId.Value);
        if (Period is not null)
        {
            Checklist = await context.MonthEndCloseChecklists.AsNoTracking()
                .Where(item => item.AccountingPeriodId == Period.Id)
                .OrderBy(item => item.Module)
                .ThenBy(item => item.ChecklistItem)
                .ToListAsync();
        }
    }
}

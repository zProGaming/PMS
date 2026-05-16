using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class GeneralLedgerModel(ApplicationDbContext context, AccountingReportService reportService) : PageModel
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int? GLAccountId { get; private set; }
    public IList<LedgerLineRow> Lines { get; private set; } = [];
    public SelectList AccountOptions { get; private set; } = default!;

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate, int? glAccountId)
    {
        StartDate = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        EndDate = endDate ?? DateTime.Today;
        GLAccountId = glAccountId;
        Lines = await reportService.GetLedgerLinesAsync(StartDate, EndDate, GLAccountId);
        var accounts = await context.GLAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountCode).ToListAsync();
        AccountOptions = new SelectList(accounts.Select(account => new { account.Id, Name = $"{account.AccountCode} - {account.AccountName}" }), "Id", "Name", GLAccountId);
    }
}

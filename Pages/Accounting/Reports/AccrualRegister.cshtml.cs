using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class AccrualRegisterModel(ApplicationDbContext context) : PageModel
{
    public IList<AccrualEntry> Accruals { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Accruals = await context.AccrualEntries.AsNoTracking()
            .Include(accrual => accrual.AccountingPeriod)
            .Include(accrual => accrual.DebitGLAccount)
            .Include(accrual => accrual.CreditGLAccount)
            .OrderByDescending(accrual => accrual.AccrualDate)
            .Take(250)
            .ToListAsync();
    }
}

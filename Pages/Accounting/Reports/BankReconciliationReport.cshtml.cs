using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class BankReconciliationReportModel(ApplicationDbContext context) : PageModel
{
    public IList<BankReconciliation> Reconciliations { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Reconciliations = await context.BankReconciliations.AsNoTracking()
            .Include(reconciliation => reconciliation.BankAccount)
            .OrderByDescending(reconciliation => reconciliation.ReconciliationDate)
            .Take(250)
            .ToListAsync();
    }
}

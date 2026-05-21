using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class TrialBalanceModel(AccountingReportService reportService) : PageModel
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public IList<AccountBalanceRow> Rows { get; private set; } = [];
    public decimal TotalDebits => Rows.Sum(row => row.DebitBalance);
    public decimal TotalCredits => Rows.Sum(row => row.CreditBalance);
    public decimal Difference => TotalDebits - TotalCredits;
    public bool IsBalanced => Difference == 0;
    public bool HasRows => Rows.Count > 0;
    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        StartDate = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        EndDate = endDate ?? DateTime.Today;
        if (EndDate < StartDate)
        {
            EndDate = StartDate;
        }

        Rows = await reportService.GetAccountBalancesAsync(StartDate, EndDate);
    }
}

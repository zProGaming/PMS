using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class TrialBalanceModel(AccountingReportService reportService) : PageModel
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public DateTime AsOfDate => EndDate;
    public IList<AccountBalanceRow> Rows { get; private set; } = [];
    public decimal TotalDebits => Rows.Sum(row => row.DebitBalance);
    public decimal TotalCredits => Rows.Sum(row => row.CreditBalance);
    public decimal Difference => Math.Round(TotalDebits - TotalCredits, 2, MidpointRounding.AwayFromZero);
    public bool IsBalanced => Difference == 0;
    public bool HasRows => Rows.Count > 0;
    public async Task OnGetAsync(DateTime? asOfDate, DateTime? endDate)
    {
        StartDate = DateTime.MinValue.Date;
        EndDate = (asOfDate ?? endDate ?? DateTime.Today).Date;

        Rows = await reportService.GetAccountBalancesAsync(StartDate, EndDate);
    }
}

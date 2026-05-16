using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class ProfitAndLossModel(AccountingReportService reportService) : PageModel
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal Revenue { get; private set; }
    public decimal CostOfSales { get; private set; }
    public decimal Expenses { get; private set; }
    public decimal OtherNet { get; private set; }
    public decimal GrossProfit => Revenue - CostOfSales;
    public decimal NetIncome => GrossProfit - Expenses + OtherNet;
    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        StartDate = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        EndDate = endDate ?? DateTime.Today;
        var rows = await reportService.GetAccountBalancesAsync(StartDate, EndDate);
        Revenue = reportService.CreditNormalAmount(rows, GLAccountType.Revenue);
        CostOfSales = reportService.DebitNormalAmount(rows, GLAccountType.CostOfSales);
        Expenses = reportService.DebitNormalAmount(rows, GLAccountType.Expense);
        OtherNet = reportService.CreditNormalAmount(rows, GLAccountType.OtherIncome) - reportService.DebitNormalAmount(rows, GLAccountType.OtherExpense);
    }
}

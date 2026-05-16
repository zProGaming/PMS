using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class BalanceSheetModel(AccountingReportService reportService) : PageModel
{
    public DateTime AsOfDate { get; private set; }
    public IList<AccountBalanceRow> Rows { get; private set; } = [];
    public IEnumerable<AccountBalanceRow> BalanceSheetRows => Rows.Where(row => row.AccountType is GLAccountType.Asset or GLAccountType.Liability or GLAccountType.Equity);
    public decimal Assets { get; private set; }
    public decimal Liabilities { get; private set; }
    public decimal Equity { get; private set; }
    public decimal CurrentPeriodEarnings { get; private set; }
    public decimal LiabilitiesAndEquity => Liabilities + Equity + CurrentPeriodEarnings;
    public decimal Difference => Assets - LiabilitiesAndEquity;
    public async Task OnGetAsync(DateTime? asOfDate)
    {
        AsOfDate = asOfDate ?? DateTime.Today;
        Rows = await reportService.GetAccountBalancesAsync(DateTime.MinValue.Date, AsOfDate);
        Assets = reportService.DebitNormalAmount(Rows, GLAccountType.Asset);
        Liabilities = reportService.CreditNormalAmount(Rows, GLAccountType.Liability);
        Equity = reportService.CreditNormalAmount(Rows, GLAccountType.Equity);
        CurrentPeriodEarnings =
            reportService.CreditNormalAmount(Rows, GLAccountType.Revenue, GLAccountType.OtherIncome) -
            reportService.DebitNormalAmount(Rows, GLAccountType.CostOfSales, GLAccountType.Expense, GLAccountType.OtherExpense);
    }
}

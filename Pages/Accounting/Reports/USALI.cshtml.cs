using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class USALIModel(AccountingReportService reportService) : PageModel
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public IList<USALIRow> Rows { get; private set; } = [];

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        StartDate = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        EndDate = endDate ?? DateTime.Today;
        var balances = await reportService.GetAccountBalancesAsync(StartDate, EndDate);
        var roomsRevenue = SumByReportLine(balances, "Rooms Revenue");
        var roomsExpense = SumByReportLine(balances, "Rooms Department Expenses");
        var fbRevenue = SumByReportLine(balances, "Food Revenue") + SumByReportLine(balances, "Beverage Revenue");
        var fbCost = SumByReportLine(balances, "F&B Cost of Sales");
        var fbExpense = SumByReportLine(balances, "F&B Department Expenses");
        var banquetRevenue = SumByReportLine(balances, "Banquet Revenue");
        var banquetCost = SumByReportLine(balances, "Banquet Cost of Sales");
        var banquetExpense = SumByReportLine(balances, "Banquet Department Expenses");
        var undistributed = reportService.DebitNormalAmount(balances, GLAccountType.Expense) - roomsExpense - fbExpense - banquetExpense;
        var departmentalProfit = roomsRevenue - roomsExpense + fbRevenue - fbCost - fbExpense + banquetRevenue - banquetCost - banquetExpense;
        var gop = departmentalProfit - undistributed;
        var other = reportService.CreditNormalAmount(balances, GLAccountType.OtherIncome) - reportService.DebitNormalAmount(balances, GLAccountType.OtherExpense);

        Rows =
        [
            new("Rooms Revenue", roomsRevenue, false),
            new("Rooms Department Expenses", roomsExpense, false),
            new("Rooms Department Profit", roomsRevenue - roomsExpense, true),
            new("F&B Revenue", fbRevenue, false),
            new("F&B Cost of Sales", fbCost, false),
            new("F&B Department Expenses", fbExpense, false),
            new("F&B Department Profit", fbRevenue - fbCost - fbExpense, true),
            new("Banquet Revenue", banquetRevenue, false),
            new("Banquet Cost of Sales", banquetCost, false),
            new("Banquet Department Profit", banquetRevenue - banquetCost - banquetExpense, true),
            new("Total Departmental Profit", departmentalProfit, true),
            new("Undistributed Operating Expenses", undistributed, false),
            new("Gross Operating Profit", gop, true),
            new("Non-Operating Income/Expenses", other, false),
            new("EBITDA-style Management Result", gop + other, true),
            new("Net Income Before Tax", gop + other, true)
        ];
    }

    private static decimal SumByReportLine(IEnumerable<AccountBalanceRow> balances, string reportLine)
    {
        return balances.Where(row => row.UsaliReportLineName == reportLine).Sum(row => row.NetAmount);
    }
}

public record USALIRow(string LineName, decimal Amount, bool IsSubtotal);

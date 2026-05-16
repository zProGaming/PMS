using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class DepartmentPLModel(AccountingReportService reportService) : PageModel
{
    public string DepartmentCode { get; private set; } = "ROOMS";
    public string DepartmentName { get; private set; } = "Rooms";
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal Revenue { get; private set; }
    public decimal CostOfSales { get; private set; }
    public decimal Expenses { get; private set; }
    public decimal DepartmentProfit => Revenue - CostOfSales - Expenses;

    public async Task OnGetAsync(string? departmentCode, DateTime? startDate, DateTime? endDate)
    {
        DepartmentCode = string.IsNullOrWhiteSpace(departmentCode) ? "ROOMS" : departmentCode;
        DepartmentName = DepartmentCode switch { "FB" => "Food & Beverage", "BNQ" => "Banquet", "OOD" => "Other Operated Departments", _ => "Rooms" };
        StartDate = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        EndDate = endDate ?? DateTime.Today;
        var rows = (await reportService.GetAccountBalancesAsync(StartDate, EndDate)).Where(row => row.UsaliDepartmentName != null && DepartmentName.Contains(row.UsaliDepartmentName, StringComparison.OrdinalIgnoreCase) || row.UsaliDepartmentName == DepartmentName).ToList();
        Revenue = rows.Where(row => row.AccountType == GLAccountType.Revenue).Sum(row => row.NetAmount);
        CostOfSales = rows.Where(row => row.AccountType == GLAccountType.CostOfSales).Sum(row => row.NetAmount);
        Expenses = rows.Where(row => row.AccountType == GLAccountType.Expense).Sum(row => row.NetAmount);
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Labor;

namespace Vantage.PMS.Pages.Labor.Reports;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<PayrollCostEntry> PayrollEntries { get; private set; } = [];
    public IList<GroupLaborRow> DepartmentRows { get; private set; } = [];
    public IList<GroupLaborRow> USALIRows { get; private set; } = [];
    public IList<BudgetReportRow> BudgetRows { get; private set; } = [];
    public IList<ServiceChargePool> ServiceChargePools { get; private set; } = [];
    public IList<JournalEntry> PayrollJournals { get; private set; } = [];

    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        StartDate = (startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        EndDate = (endDate ?? DateTime.Today).Date;
        var endExclusive = EndDate.AddDays(1);

        var entriesQuery = context.PayrollCostEntries
            .AsNoTracking()
            .Include(entry => entry.PayrollPeriod)
            .Include(entry => entry.EmployeeCostProfile)
            .Include(entry => entry.Department)
            .Include(entry => entry.USALIDepartment)
            .Where(entry => entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate < endExclusive &&
                entry.PayrollPeriod.EndDate >= StartDate &&
                entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled);

        PayrollEntries = await entriesQuery
            .OrderBy(entry => entry.PayrollPeriod!.StartDate)
            .ThenBy(entry => entry.Department != null ? entry.Department.Name : string.Empty)
            .Take(500)
            .ToListAsync();

        DepartmentRows = PayrollEntries
            .GroupBy(entry => entry.Department?.Name ?? "Unassigned")
            .Select(group => GroupRow(group.Key, group))
            .OrderByDescending(row => row.LaborCost)
            .ToList();
        USALIRows = PayrollEntries
            .GroupBy(entry => entry.USALIDepartment?.Name ?? "Unmapped")
            .Select(group => GroupRow(group.Key, group))
            .OrderByDescending(row => row.LaborCost)
            .ToList();

        var budgets = await context.DepartmentLaborBudgets
            .AsNoTracking()
            .Include(budget => budget.Department)
            .Where(budget => budget.Year >= StartDate.Year && budget.Year <= EndDate.Year)
            .ToListAsync();
        BudgetRows = budgets.Select(budget =>
        {
            var actual = PayrollEntries.Where(entry =>
                entry.DepartmentId == budget.DepartmentId &&
                entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate.Month == budget.Month &&
                entry.PayrollPeriod.StartDate.Year == budget.Year);
            return new BudgetReportRow
            {
                DepartmentName = budget.Department?.Name ?? "Unassigned",
                Month = budget.Month,
                Year = budget.Year,
                BudgetedCost = budget.BudgetedLaborCost,
                ActualCost = actual.Sum(entry => entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay),
                BudgetedHours = budget.BudgetedLaborHours,
                ActualHours = actual.Sum(entry => entry.RegularHours + entry.OvertimeHours + entry.NightDifferentialHours)
            };
        }).OrderBy(row => row.Year).ThenBy(row => row.Month).ThenBy(row => row.DepartmentName).ToList();

        ServiceChargePools = await context.ServiceChargePools
            .AsNoTracking()
            .Include(pool => pool.DistributionLines)
            .ThenInclude(line => line.EmployeeCostProfile)
            .Include(pool => pool.DistributionLines)
            .ThenInclude(line => line.Department)
            .Where(pool => pool.PeriodStart < endExclusive && pool.PeriodEnd >= StartDate)
            .OrderByDescending(pool => pool.PeriodEnd)
            .ToListAsync();

        PayrollJournals = await context.JournalEntries
            .AsNoTracking()
            .Include(entry => entry.Lines)
            .Where(entry =>
                entry.JournalDate >= StartDate &&
                entry.JournalDate < endExclusive &&
                (entry.SourceTransactionType == SourceTransactionType.PayrollCost ||
                    entry.SourceTransactionType == SourceTransactionType.ServiceChargeDistribution))
            .OrderByDescending(entry => entry.JournalDate)
            .ToListAsync();
    }

    private static GroupLaborRow GroupRow(string name, IEnumerable<PayrollCostEntry> entries)
    {
        var list = entries.ToList();
        var hours = list.Sum(entry => entry.RegularHours + entry.OvertimeHours + entry.NightDifferentialHours);
        var laborCost = list.Sum(entry => entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay);
        return new GroupLaborRow
        {
            Name = name,
            LaborCost = laborCost,
            PayrollHours = hours,
            GrossPay = list.Sum(entry => entry.GrossPay),
            NetPay = list.Sum(entry => entry.NetPay),
            ServiceChargeShare = list.Sum(entry => entry.ServiceChargeShare)
        };
    }
}

public class GroupLaborRow
{
    public string Name { get; set; } = string.Empty;
    public decimal LaborCost { get; set; }
    public decimal PayrollHours { get; set; }
    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }
    public decimal ServiceChargeShare { get; set; }
}

public class BudgetReportRow
{
    public string DepartmentName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal BudgetedCost { get; set; }
    public decimal ActualCost { get; set; }
    public decimal BudgetedHours { get; set; }
    public decimal ActualHours { get; set; }
    public decimal CostVariance => ActualCost - BudgetedCost;
    public decimal HoursVariance => ActualHours - BudgetedHours;
}

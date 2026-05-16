using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Labor;

public class IndexModel(ApplicationDbContext context, LaborCostingService laborCostingService) : PageModel
{
    public LaborDashboardMetrics Metrics { get; private set; } = new();

    public IList<DepartmentLaborRow> DepartmentLabor { get; private set; } = [];

    public IList<PayrollPeriod> RecentPayrollPeriods { get; private set; } = [];

    public IList<ServiceChargePool> PendingServiceChargePools { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        Metrics = await laborCostingService.GetDashboardMetricsAsync(today);
        DepartmentLabor = await context.PayrollCostEntries
            .AsNoTracking()
            .Where(entry => entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate < nextMonth &&
                entry.PayrollPeriod.EndDate >= monthStart &&
                entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled)
            .GroupBy(entry => new
            {
                DepartmentName = entry.Department != null ? entry.Department.Name : "Unassigned",
                USALIName = entry.USALIDepartment != null ? entry.USALIDepartment.Name : "Unmapped"
            })
            .Select(group => new DepartmentLaborRow
            {
                DepartmentName = group.Key.DepartmentName,
                USALIName = group.Key.USALIName,
                LaborCost = group.Sum(entry => entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay),
                LaborHours = group.Sum(entry => entry.RegularHours + entry.OvertimeHours + entry.NightDifferentialHours),
                Headcount = group.Select(entry => entry.EmployeeCostProfileId).Distinct().Count()
            })
            .OrderByDescending(row => row.LaborCost)
            .Take(10)
            .ToListAsync();

        RecentPayrollPeriods = await context.PayrollPeriods
            .AsNoTracking()
            .OrderByDescending(period => period.StartDate)
            .Take(6)
            .ToListAsync();

        PendingServiceChargePools = await context.ServiceChargePools
            .AsNoTracking()
            .Where(pool => pool.Status == ServiceChargePoolStatus.ForApproval || pool.Status == ServiceChargePoolStatus.Approved)
            .OrderByDescending(pool => pool.PeriodEnd)
            .Take(6)
            .ToListAsync();
    }
}

public class DepartmentLaborRow
{
    public string DepartmentName { get; set; } = string.Empty;

    public string USALIName { get; set; } = string.Empty;

    public decimal LaborCost { get; set; }

    public decimal LaborHours { get; set; }

    public int Headcount { get; set; }
}

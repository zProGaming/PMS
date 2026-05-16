using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive;

public class WeeklySummaryModel(ExecutiveReportingService reportingService, ExecutiveKPIService kpiService, DepartmentPerformanceService departmentService, ExecutiveAlertService alertService) : PageModel
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ExecutiveSummaryMetrics Summary { get; private set; } = new();
    public IList<ExecutiveTrendRow> Trend { get; private set; } = [];
    public IList<DepartmentPerformanceRow> Departments { get; private set; } = [];
    public IList<Models.Executive.ExecutiveAlert> Alerts { get; private set; } = [];

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        EndDate = endDate?.Date ?? DateTime.Today;
        StartDate = startDate?.Date ?? EndDate.AddDays(-6);
        Summary = await kpiService.GetSummaryAsync(StartDate, EndDate);
        Trend = await reportingService.GetSevenDayTrendAsync(EndDate);
        Departments = await departmentService.GetDepartmentPerformanceAsync(StartDate, EndDate);
        Alerts = await alertService.GetOpenAlertsAsync(8);
    }
}

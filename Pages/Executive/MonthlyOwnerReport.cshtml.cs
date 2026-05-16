using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive;

public class MonthlyOwnerReportModel(ExecutiveReportingService reportingService) : PageModel
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ExecutiveDashboardView Report { get; private set; } = default!;
    public string SummaryText { get; private set; } = string.Empty;

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        EndDate = endDate?.Date ?? DateTime.Today;
        StartDate = startDate?.Date ?? new DateTime(EndDate.Year, EndDate.Month, 1);
        Report = await reportingService.GetDashboardAsync(StartDate, EndDate);
        SummaryText = ExecutiveReportingService.BuildExecutiveSummaryText(Report.Summary);
    }
}

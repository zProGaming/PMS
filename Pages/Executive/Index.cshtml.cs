using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive;

public class IndexModel(ExecutiveReportingService executiveReportingService) : PageModel
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ExecutiveDashboardView Dashboard { get; private set; } = default!;

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        EndDate = endDate?.Date ?? DateTime.Today;
        StartDate = startDate?.Date ?? new DateTime(EndDate.Year, EndDate.Month, 1);
        Dashboard = await executiveReportingService.GetDashboardAsync(StartDate, EndDate);
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive;

public class DailyFlashModel(ExecutiveKPIService kpiService, ExecutiveAlertService alertService) : PageModel
{
    public DateTime BusinessDate { get; private set; }
    public ExecutiveSummaryMetrics Summary { get; private set; } = new();
    public IList<Models.Executive.ExecutiveAlert> Alerts { get; private set; } = [];

    public async Task OnGetAsync(DateTime? businessDate)
    {
        BusinessDate = businessDate?.Date ?? DateTime.Today;
        Summary = await kpiService.GetSummaryAsync(BusinessDate, BusinessDate);
        Alerts = await alertService.GetOpenAlertsAsync(10);
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.Executive;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive;

public class KPIScorecardModel(ExecutiveKPIService kpiService) : PageModel
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ExecutiveKPICategory? Category { get; private set; }
    public KPIStatus? Status { get; private set; }
    public IList<ExecutiveKpiScoreRow> Rows { get; private set; } = [];

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate, ExecutiveKPICategory? category, KPIStatus? status)
    {
        EndDate = endDate?.Date ?? DateTime.Today;
        StartDate = startDate?.Date ?? new DateTime(EndDate.Year, EndDate.Month, 1);
        Category = category;
        Status = status;
        Rows = await kpiService.GetScorecardAsync(StartDate, EndDate);
        if (Category is not null)
        {
            Rows = Rows.Where(row => row.Category == Category).ToList();
        }
        if (Status is not null)
        {
            Rows = Rows.Where(row => row.Status == Status).ToList();
        }
    }
}

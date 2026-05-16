using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive;

public class DepartmentPerformanceModel(DepartmentPerformanceService departmentService) : PageModel
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public IList<DepartmentPerformanceRow> Rows { get; private set; } = [];

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        EndDate = endDate?.Date ?? DateTime.Today;
        StartDate = startDate?.Date ?? new DateTime(EndDate.Year, EndDate.Month, 1);
        Rows = await departmentService.GetDepartmentPerformanceAsync(StartDate, EndDate);
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class UnmappedCashFlowItemsModel(CashFlowReportService cashFlowReportService) : PageModel
{
    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    public IList<CashMovementRow> Rows { get; private set; } = [];

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        var today = DateTime.Today;
        StartDate = startDate?.Date ?? new DateTime(today.Year, today.Month, 1);
        EndDate = endDate?.Date ?? today;
        if (EndDate < StartDate)
        {
            (StartDate, EndDate) = (EndDate, StartDate);
        }

        Rows = await cashFlowReportService.GetCashMovementsAsync(StartDate, EndDate, mapped: false);
    }
}

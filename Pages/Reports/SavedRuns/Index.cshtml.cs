using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Reports;

namespace Vantage.PMS.Pages.Reports.SavedRuns;

[Authorize(Policy = PmsPolicies.ReportAdministration)]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<SavedReportRun> Runs { get; private set; } = [];

    public ReportCategory? Category { get; private set; }

    public string? Search { get; private set; }

    public async Task OnGetAsync(ReportCategory? category, string? search)
    {
        Category = category;
        Search = search;

        var query = context.SavedReportRuns.AsNoTracking();
        if (Category is not null)
        {
            query = query.Where(run => run.ReportCategory == Category);
        }
        if (!string.IsNullOrWhiteSpace(Search))
        {
            query = query.Where(run => run.ReportName.Contains(Search) || run.ReportKey.Contains(Search));
        }

        Runs = await query
            .OrderByDescending(run => run.RunAt)
            .Take(300)
            .ToListAsync();
    }
}

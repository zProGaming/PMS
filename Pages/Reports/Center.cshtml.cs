using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.Reports;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Reports;

public class CenterModel(ReportCatalogService catalogService) : PageModel
{
    public IReadOnlyList<ReportCatalogEntry> Reports { get; private set; } = [];

    public ReportCategory? Category { get; private set; }

    public string? Search { get; private set; }

    public async Task OnGetAsync(ReportCategory? category, string? search)
    {
        Category = category;
        Search = search;
        IEnumerable<ReportCatalogEntry> query = catalogService.GetAuthorizedCatalog(User);

        if (Category is not null)
        {
            query = query.Where(report => report.ReportCategory == Category);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            query = query.Where(report =>
                report.ReportName.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                report.Description.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                ReportCatalogService.FormatCategory(report.ReportCategory).Contains(Search, StringComparison.OrdinalIgnoreCase));
        }

        Reports = query
            .OrderBy(report => report.ReportCategory)
            .ThenBy(report => report.ReportName)
            .ToList();
        await Task.CompletedTask;
    }
}

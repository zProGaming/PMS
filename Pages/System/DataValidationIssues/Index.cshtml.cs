using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.System.DataValidationIssues;

public class IndexModel(ApplicationDbContext context, DataValidationService validationService) : PageModel
{
    public IList<DataValidationIssue> Issues { get; private set; } = [];

    public int OpenIssues { get; private set; }

    public int CriticalIssues { get; private set; }

    public int HighIssues { get; private set; }

    [BindProperty(SupportsGet = true)]
    public bool IncludeResolved { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Module { get; set; }

    [BindProperty(SupportsGet = true)]
    public SystemSeverity? Severity { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostRunScanAsync()
    {
        var count = await validationService.RunValidationScanAsync(User.Identity?.Name ?? "System");
        StatusMessage = $"Validation scan completed. New issues found: {count}.";
        return RedirectToPage(new { IncludeResolved, Module, Severity });
    }

    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        await validationService.MarkResolvedAsync(id, User.Identity?.Name ?? "System");
        StatusMessage = "Validation issue marked as resolved.";
        return RedirectToPage(new { IncludeResolved, Module, Severity });
    }

    private async Task LoadAsync()
    {
        var baseQuery = context.DataValidationIssues.AsNoTracking();
        OpenIssues = await baseQuery.CountAsync(issue => !issue.IsResolved);
        CriticalIssues = await baseQuery.CountAsync(issue => !issue.IsResolved && issue.Severity == SystemSeverity.Critical);
        HighIssues = await baseQuery.CountAsync(issue => !issue.IsResolved && issue.Severity == SystemSeverity.High);

        var query = baseQuery;
        if (!IncludeResolved)
        {
            query = query.Where(issue => !issue.IsResolved);
        }

        if (!string.IsNullOrWhiteSpace(Module))
        {
            query = query.Where(issue => issue.Module == Module);
        }

        if (Severity is not null)
        {
            query = query.Where(issue => issue.Severity == Severity);
        }

        Issues = await query
            .OrderByDescending(issue => issue.Severity)
            .ThenByDescending(issue => issue.IssueDate)
            .Take(300)
            .ToListAsync();
    }
}

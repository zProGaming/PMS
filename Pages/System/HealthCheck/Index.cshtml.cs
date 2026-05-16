using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.System.HealthCheck;

public class IndexModel(ApplicationDbContext context, DataValidationService validationService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly DataValidationService _validationService = validationService;

    public IList<DataValidationIssue> Issues { get; set; } = new List<DataValidationIssue>();

    [BindProperty(SupportsGet = true)]
    public bool IncludeResolved { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostRunScanAsync()
    {
        var count = await _validationService.RunValidationScanAsync(User.Identity?.Name ?? "System");
        StatusMessage = $"Validation scan completed. New issues found: {count}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        await _validationService.MarkResolvedAsync(id, User.Identity?.Name ?? "System");
        StatusMessage = "Validation issue marked as resolved.";
        return RedirectToPage(new { IncludeResolved });
    }

    private async Task LoadAsync()
    {
        var query = _context.DataValidationIssues.AsNoTracking();
        if (!IncludeResolved)
        {
            query = query.Where(issue => !issue.IsResolved);
        }

        Issues = await query
            .OrderByDescending(issue => issue.Severity)
            .ThenByDescending(issue => issue.IssueDate)
            .Take(250)
            .ToListAsync();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.System.HealthCheck;

public class IndexModel(ApplicationDbContext context, DataValidationService validationService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly DataValidationService _validationService = validationService;

    public IList<DataValidationIssue> Issues { get; set; } = new List<DataValidationIssue>();

    public IList<ModuleHealthSummary> ModuleSummaries { get; set; } = new List<ModuleHealthSummary>();

    public PilotReadinessSnapshot Readiness { get; set; } = new();

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
        var allIssues = _context.DataValidationIssues.AsNoTracking();
        var openIssues = allIssues.Where(issue => !issue.IsResolved);

        var critical = await openIssues.CountAsync(issue => issue.Severity == SystemSeverity.Critical);
        var high = await openIssues.CountAsync(issue => issue.Severity == SystemSeverity.High);
        var medium = await openIssues.CountAsync(issue => issue.Severity == SystemSeverity.Medium);
        var low = await openIssues.CountAsync(issue => issue.Severity == SystemSeverity.Low);
        var info = await openIssues.CountAsync(issue => issue.Severity == SystemSeverity.Info);
        var resolved = await allIssues.CountAsync(issue => issue.IsResolved);
        var latestIssueDate = await openIssues.MaxAsync(issue => (DateTime?)issue.IssueDate);
        var readinessScore = Math.Max(0, 100 - (critical * 25) - (high * 10) - (medium * 3) - low);

        var businessDate = await _context.BusinessDateSettings
            .AsNoTracking()
            .OrderBy(setting => setting.Id)
            .Select(setting => (DateTime?)setting.CurrentBusinessDate)
            .FirstOrDefaultAsync();

        var postedOperationalJournals = await _context.JournalEntries
            .AsNoTracking()
            .CountAsync(entry => entry.Status == JournalEntryStatus.Posted && entry.SourceModule != SourceModule.Manual);

        var demoCloseJournals = await _context.JournalEntries
            .AsNoTracking()
            .CountAsync(entry => entry.Status == JournalEntryStatus.Posted && entry.JournalNumber.StartsWith("DEMO-CLOSE-"));

        var cleanBankReconciliations = await _context.BankReconciliations
            .AsNoTracking()
            .CountAsync(reconciliation =>
                reconciliation.Status == BankReconciliationStatus.Approved &&
                reconciliation.Difference == 0 &&
                reconciliation.Notes != null &&
                reconciliation.Notes.Contains("DEMO-CLOSE"));

        var openCashierShifts = await _context.CashierShifts
            .AsNoTracking()
            .CountAsync(shift => shift.Status == CashierShiftStatus.Open);

        Readiness = new PilotReadinessSnapshot(
            readinessScore,
            critical,
            high,
            medium,
            low,
            info,
            resolved,
            latestIssueDate,
            businessDate,
            postedOperationalJournals,
            demoCloseJournals,
            cleanBankReconciliations,
            openCashierShifts);

        var moduleSummaryRows = await openIssues
            .GroupBy(issue => issue.Module)
            .Select(group => new
            {
                Module = group.Key,
                Total = group.Count(),
                Critical = group.Count(issue => issue.Severity == SystemSeverity.Critical),
                High = group.Count(issue => issue.Severity == SystemSeverity.High),
                Medium = group.Count(issue => issue.Severity == SystemSeverity.Medium),
                Low = group.Count(issue => issue.Severity == SystemSeverity.Low)
            })
            .ToListAsync();

        ModuleSummaries = moduleSummaryRows
            .Select(row => new ModuleHealthSummary(row.Module, row.Total, row.Critical, row.High, row.Medium, row.Low))
            .OrderByDescending(summary => summary.Critical)
            .ThenByDescending(summary => summary.High)
            .ThenByDescending(summary => summary.Medium)
            .ThenBy(summary => summary.Module)
            .Take(20)
            .ToList();

        var query = allIssues;
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

public record PilotReadinessSnapshot(
    int Score = 100,
    int CriticalIssues = 0,
    int HighIssues = 0,
    int MediumIssues = 0,
    int LowIssues = 0,
    int InfoIssues = 0,
    int ResolvedIssues = 0,
    DateTime? LatestIssueDate = null,
    DateTime? BusinessDate = null,
    int PostedOperationalJournals = 0,
    int DemoCloseJournals = 0,
    int CleanBankReconciliations = 0,
    int OpenCashierShifts = 0);

public record ModuleHealthSummary(string Module, int Total, int Critical, int High, int Medium, int Low);

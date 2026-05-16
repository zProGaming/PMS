using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.ManagementAI;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.ManagementAI.Insights;

public class IndexModel(ApplicationDbContext context, ManagementInsightService insightService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly ManagementInsightService _insightService = insightService;

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public ManagementInsightSeverity? Severity { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? RelatedModule { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? Resolved { get; set; }

    public IList<ManagementInsight> Insights { get; set; } = new List<ManagementInsight>();

    public IList<string> Modules { get; set; } = new List<string>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        var resolved = await _insightService.ResolveInsightAsync(id, User.Identity?.Name ?? "System");
        StatusMessage = resolved ? "Insight marked as resolved." : "Insight was not found.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Modules = await _context.ManagementInsights
            .AsNoTracking()
            .Where(insight => insight.RelatedModule != "")
            .Select(insight => insight.RelatedModule)
            .Distinct()
            .OrderBy(module => module)
            .ToListAsync();

        var query = _context.ManagementInsights.AsNoTracking();

        if (DateFrom is not null)
        {
            query = query.Where(insight => insight.InsightDate >= DateFrom.Value.Date);
        }

        if (DateTo is not null)
        {
            var exclusiveEnd = DateTo.Value.Date.AddDays(1);
            query = query.Where(insight => insight.InsightDate < exclusiveEnd);
        }

        if (Severity is not null)
        {
            query = query.Where(insight => insight.Severity == Severity.Value);
        }

        if (!string.IsNullOrWhiteSpace(RelatedModule))
        {
            query = query.Where(insight => insight.RelatedModule == RelatedModule);
        }

        if (Resolved is not null)
        {
            query = query.Where(insight => insight.IsResolved == Resolved.Value);
        }

        Insights = await query
            .OrderByDescending(insight => insight.Severity)
            .ThenByDescending(insight => insight.InsightDate)
            .ThenByDescending(insight => insight.CreatedAt)
            .Take(250)
            .ToListAsync();
    }
}

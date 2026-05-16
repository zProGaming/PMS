using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.ManagementAI;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.ManagementAI;

public class IndexModel(
    ApplicationDbContext context,
    ManagementDailySummaryService summaryService,
    ManagementInsightService insightService,
    AIPlaceholderService aiPlaceholderService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly ManagementDailySummaryService _summaryService = summaryService;
    private readonly ManagementInsightService _insightService = insightService;
    private readonly AIPlaceholderService _aiPlaceholderService = aiPlaceholderService;

    public ManagementDailySummary? Summary { get; set; }

    public IList<ManagementInsight> CriticalInsights { get; set; } = new List<ManagementInsight>();

    public IList<ManagementInsight> HighPriorityInsights { get; set; } = new List<ManagementInsight>();

    public IList<string> RecommendedActions { get; set; } = new List<string>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDashboardAsync();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        var performedBy = User.Identity?.Name ?? "System";
        var summary = await _summaryService.GenerateOrUpdateTodaySummaryAsync(performedBy);
        var generatedInsights = await _insightService.GenerateInsightsForBusinessDateAsync(summary, performedBy);
        StatusMessage = $"Generated management summary for {summary.BusinessDate:d}. New insights created: {generatedInsights.Count}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        var performedBy = User.Identity?.Name ?? "System";
        _context.AIActionLogs.Add(new AIActionLog
        {
            ActionDate = DateTime.Now,
            ActionType = AIActionType.Exported,
            Module = "Management AI",
            Description = "Export summary placeholder was requested.",
            PerformedBy = performedBy,
            Notes = "External export templates are not implemented in the MVP."
        });
        await _context.SaveChangesAsync();

        StatusMessage = "Export placeholder logged. External export templates are disabled in the MVP.";
        return RedirectToPage();
    }

    private async Task LoadDashboardAsync()
    {
        var businessDate = await _summaryService.GetBusinessDateAsync();
        Summary = await _context.ManagementDailySummaries
            .AsNoTracking()
            .Where(summary => summary.BusinessDate == businessDate)
            .OrderByDescending(summary => summary.CreatedAt)
            .FirstOrDefaultAsync()
            ?? await _context.ManagementDailySummaries
                .AsNoTracking()
                .OrderByDescending(summary => summary.BusinessDate)
                .FirstOrDefaultAsync();

        CriticalInsights = await _context.ManagementInsights
            .AsNoTracking()
            .Where(insight => !insight.IsResolved && insight.Severity == ManagementInsightSeverity.Critical)
            .OrderByDescending(insight => insight.InsightDate)
            .ThenByDescending(insight => insight.CreatedAt)
            .Take(8)
            .ToListAsync();

        HighPriorityInsights = await _context.ManagementInsights
            .AsNoTracking()
            .Where(insight => !insight.IsResolved && insight.Severity == ManagementInsightSeverity.High)
            .OrderByDescending(insight => insight.InsightDate)
            .ThenByDescending(insight => insight.CreatedAt)
            .Take(8)
            .ToListAsync();

        if (Summary is null)
        {
            return;
        }

        var recommendations = new List<string>();
        recommendations.AddRange(await _aiPlaceholderService.GenerateOperationalRecommendationsAsync(Summary));
        recommendations.AddRange(await _aiPlaceholderService.GenerateFinancialRecommendationsAsync(Summary));
        recommendations.AddRange(await _aiPlaceholderService.GenerateGuestExperienceRecommendationsAsync(Summary));
        recommendations.AddRange(await _aiPlaceholderService.GenerateRevenueRecommendationsAsync(Summary));
        recommendations.AddRange(await _aiPlaceholderService.GenerateInventoryRecommendationsAsync(Summary));

        RecommendedActions = recommendations
            .Where(action => !string.IsNullOrWhiteSpace(action))
            .Distinct()
            .Take(10)
            .ToList();
    }
}

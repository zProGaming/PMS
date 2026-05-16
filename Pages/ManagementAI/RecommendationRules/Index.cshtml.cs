using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.ManagementAI;

namespace Vantage.PMS.Pages.ManagementAI.RecommendationRules;

[Authorize(Roles = $"{PmsRoles.SystemAdmin},{PmsRoles.GeneralManager}")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public AIRecommendationRule NewRule { get; set; } = new();

    public IList<AIRecommendationRule> Rules { get; set; } = new List<AIRecommendationRule>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRule.RuleName) ||
            string.IsNullOrWhiteSpace(NewRule.Module) ||
            string.IsNullOrWhiteSpace(NewRule.ConditionDescription) ||
            string.IsNullOrWhiteSpace(NewRule.RecommendationText))
        {
            StatusMessage = "Rule name, module, condition, and recommendation are required.";
            await LoadAsync();
            return Page();
        }

        NewRule.CreatedAt = DateTime.Now;
        _context.AIRecommendationRules.Add(NewRule);
        await _context.SaveChangesAsync();
        StatusMessage = "Recommendation rule created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        int id,
        string ruleName,
        string module,
        string conditionDescription,
        string recommendationText,
        ManagementInsightSeverity severity,
        bool isActive)
    {
        var rule = await _context.AIRecommendationRules.FindAsync(id);
        if (rule is null)
        {
            return NotFound();
        }

        rule.RuleName = ruleName ?? string.Empty;
        rule.Module = module ?? string.Empty;
        rule.ConditionDescription = conditionDescription ?? string.Empty;
        rule.RecommendationText = recommendationText ?? string.Empty;
        rule.Severity = severity;
        rule.IsActive = isActive;
        await _context.SaveChangesAsync();
        StatusMessage = "Recommendation rule updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var rule = await _context.AIRecommendationRules.FindAsync(id);
        if (rule is null)
        {
            return NotFound();
        }

        _context.AIRecommendationRules.Remove(rule);
        await _context.SaveChangesAsync();
        StatusMessage = "Recommendation rule deleted.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Rules = await _context.AIRecommendationRules
            .AsNoTracking()
            .OrderBy(rule => rule.Module)
            .ThenBy(rule => rule.RuleName)
            .ToListAsync();
    }
}

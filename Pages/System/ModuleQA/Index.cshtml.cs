using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Pages.System.ModuleQA;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<QATestChecklistItem> Items { get; private set; } = [];

    public IReadOnlyList<ModuleQaSummary> ModuleSummaries { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, QATestChecklistStatus status, string? notes)
    {
        var item = await context.QATestChecklistItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = status;
        item.Notes = notes;
        item.TestedBy = User.Identity?.Name ?? "System";
        item.TestedAt = DateTime.Now;
        await context.SaveChangesAsync();

        StatusMessage = "Module QA status updated.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Items = await context.QATestChecklistItems
            .AsNoTracking()
            .OrderBy(item => item.Module)
            .ThenBy(item => item.TestName)
            .ToListAsync();

        ModuleSummaries = Items
            .GroupBy(item => item.Module)
            .Select(group => new ModuleQaSummary(
                group.Key,
                group.Count(),
                group.Count(item => item.Status == QATestChecklistStatus.Passed),
                group.Count(item => item.Status == QATestChecklistStatus.Failed),
                group.Count(item => item.Status == QATestChecklistStatus.NeedsFix),
                group.Count(item => item.Status == QATestChecklistStatus.NotTested)))
            .OrderBy(summary => summary.Module)
            .ToList();
    }

    public record ModuleQaSummary(
        string Module,
        int Total,
        int Passed,
        int Failed,
        int NeedsFix,
        int NotTested)
    {
        public int ProgressPercent => Total == 0 ? 0 : (int)Math.Round(Passed * 100m / Total);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Pages.System.QAChecklist;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<QATestChecklistItem> Items { get; set; } = new List<QATestChecklistItem>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, QATestChecklistStatus status, string? notes)
    {
        var item = await _context.QATestChecklistItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = status;
        item.Notes = notes;
        item.TestedBy = User.Identity?.Name ?? "System";
        item.TestedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        StatusMessage = "QA checklist item updated.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Items = await _context.QATestChecklistItems
            .AsNoTracking()
            .OrderBy(item => item.Module)
            .ThenBy(item => item.TestName)
            .ToListAsync();
    }
}

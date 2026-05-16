using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.JournalEntries;

public class DetailsModel(ApplicationDbContext context, AccountingPostingService postingService) : PageModel
{
    public JournalEntry? JournalEntry { get; private set; }
    public IList<string> Errors { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        JournalEntry = await LoadEntryAsync(id);
        return JournalEntry is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostPostAsync(int id)
    {
        Errors = await postingService.PostJournalEntryAsync(id, User.Identity?.Name ?? "System");
        JournalEntry = await LoadEntryAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostReverseAsync(int id, string reason)
    {
        Errors = await postingService.ReverseJournalEntryAsync(id, User.Identity?.Name ?? "System", reason);
        JournalEntry = await LoadEntryAsync(id);
        return Page();
    }

    private async Task<JournalEntry?> LoadEntryAsync(int id)
    {
        return await context.JournalEntries
            .Include(entry => entry.Lines)
            .ThenInclude(line => line.GLAccount)
            .Include(entry => entry.Lines)
            .ThenInclude(line => line.USALIDepartment)
            .FirstOrDefaultAsync(entry => entry.Id == id);
    }
}

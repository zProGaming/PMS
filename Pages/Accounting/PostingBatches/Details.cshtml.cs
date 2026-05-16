using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.PostingBatches;

public class DetailsModel(ApplicationDbContext context, AccountingPostingService postingService) : PageModel
{
    public PostingBatch? Batch { get; private set; }
    public IList<string> Errors { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Batch = await LoadBatchAsync(id);
        return Batch is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostProcessAsync(int id)
    {
        Errors = await postingService.ProcessPostingBatchAsync(id, User.Identity?.Name ?? "System");
        Batch = await LoadBatchAsync(id);
        return Page();
    }

    private async Task<PostingBatch?> LoadBatchAsync(int id)
    {
        return await context.PostingBatches
            .Include(batch => batch.Items)
            .ThenInclude(item => item.JournalEntry)
            .FirstOrDefaultAsync(batch => batch.Id == id);
    }
}

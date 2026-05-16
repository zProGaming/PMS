using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.PostingBatches;

public class IndexModel(ApplicationDbContext context, AccountingPostingService postingService) : PageModel
{
    public IList<PostingBatch> Batches { get; private set; } = [];
    [BindProperty] public SourceModule SourceModule { get; set; } = SourceModule.Finance;
    [BindProperty] public DateTime StartDate { get; set; } = DateTime.Today;
    [BindProperty] public DateTime EndDate { get; set; } = DateTime.Today;

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (EndDate < StartDate)
        {
            ModelState.AddModelError(nameof(EndDate), "End date must be after start date.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var batch = await postingService.CreatePostingBatchAsync(SourceModule, StartDate, EndDate, User.Identity?.Name ?? "System");
        return RedirectToPage("Details", new { id = batch.Id });
    }

    private async Task LoadAsync()
    {
        Batches = await context.PostingBatches.AsNoTracking().Include(batch => batch.Items).OrderByDescending(batch => batch.CreatedAt).Take(100).ToListAsync();
    }
}

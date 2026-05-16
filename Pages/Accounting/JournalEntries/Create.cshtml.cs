using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.JournalEntries;

public class CreateModel(ApplicationDbContext context, AccountingPostingService postingService) : PageModel
{
    [BindProperty] public DateTime JournalDate { get; set; } = DateTime.Today;
    [BindProperty] public string Description { get; set; } = string.Empty;
    [BindProperty] public List<ManualJournalLineInput> Lines { get; set; } = [new(), new(), new(), new()];
    public SelectList AccountOptions { get; private set; } = default!;
    public SelectList DepartmentOptions { get; private set; } = default!;

    public async Task OnGetAsync() => await LoadOptionsAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var validLines = Lines.Where(line => line.GLAccountId > 0 && (line.DebitAmount > 0 || line.CreditAmount > 0)).ToList();
        var journalEntry = new JournalEntry
        {
            JournalDate = JournalDate,
            Description = Description,
            CreatedBy = User.Identity?.Name ?? "System",
            Lines = validLines.Select(line => new JournalEntryLine
            {
                GLAccountId = line.GLAccountId,
                USALIDepartmentId = line.USALIDepartmentId,
                Description = line.Description,
                DebitAmount = line.DebitAmount,
                CreditAmount = line.CreditAmount
            }).ToList()
        };

        if (string.IsNullOrWhiteSpace(Description))
        {
            ModelState.AddModelError(nameof(Description), "Description is required.");
        }

        if (!postingService.ValidateJournalEntryBalance(journalEntry))
        {
            ModelState.AddModelError(string.Empty, "Journal entry must have at least two valid lines and total debits must equal total credits. A line cannot have both debit and credit.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await postingService.CreateManualJournalEntryAsync(journalEntry);
        return RedirectToPage("Details", new { id = journalEntry.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var accounts = await context.GLAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountCode).ToListAsync();
        AccountOptions = new SelectList(accounts.Select(account => new { account.Id, Name = $"{account.AccountCode} - {account.AccountName}" }), "Id", "Name");
        DepartmentOptions = new SelectList(await context.USALIDepartments.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.SortOrder).ToListAsync(), "Id", "Name");
    }

    public class ManualJournalLineInput
    {
        public int GLAccountId { get; set; }
        public int? USALIDepartmentId { get; set; }
        public string? Description { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
    }
}

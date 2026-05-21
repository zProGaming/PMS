using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Banking.BankTransactions;

public class IndexModel(ApplicationDbContext context, AccountingPostingService postingService) : PageModel
{
    public IList<BankTransaction> Transactions { get; private set; } = [];
    public SelectList BankAccountOptions { get; private set; } = default!;
    public SelectList OffsetAccountOptions { get; private set; } = default!;

    [BindProperty]
    public BankTransaction Input { get; set; } = new();

    [BindProperty]
    public int? OffsetGLAccountId { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (Input.BankAccountId <= 0)
        {
            ModelState.AddModelError("Input.BankAccountId", "Bank account is required.");
        }

        var bankAccount = Input.BankAccountId <= 0
            ? null
            : await context.BankAccounts.AsNoTracking().FirstOrDefaultAsync(account => account.Id == Input.BankAccountId && account.IsActive);
        if (bankAccount?.GLAccountId is null)
        {
            ModelState.AddModelError("Input.BankAccountId", "Bank account must have a mapped GL account before manual bank transactions can be posted.");
        }

        if (OffsetGLAccountId is null or <= 0)
        {
            ModelState.AddModelError(nameof(OffsetGLAccountId), "Offset GL account is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.Description))
        {
            ModelState.AddModelError("Input.Description", "Description is required.");
        }

        if ((Input.DebitAmount <= 0 && Input.CreditAmount <= 0) || (Input.DebitAmount > 0 && Input.CreditAmount > 0))
        {
            ModelState.AddModelError(string.Empty, "Enter either a debit or credit amount.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var amount = Input.DebitAmount > 0 ? Input.DebitAmount : Input.CreditAmount;
        var journal = new JournalEntry
        {
            JournalDate = Input.TransactionDate.Date,
            SourceModule = SourceModule.Manual,
            SourceTransactionType = SourceTransactionType.ManualJournal,
            SourceReferenceNumber = Input.ReferenceNumber,
            Description = $"Manual bank transaction - {Input.Description}",
            CreatedBy = User.Identity?.Name ?? "System"
        };

        if (Input.DebitAmount > 0)
        {
            journal.Lines.Add(new JournalEntryLine { GLAccountId = bankAccount!.GLAccountId!.Value, DebitAmount = amount, Description = Input.Description });
            journal.Lines.Add(new JournalEntryLine { GLAccountId = OffsetGLAccountId!.Value, CreditAmount = amount, Description = Input.Description });
        }
        else
        {
            journal.Lines.Add(new JournalEntryLine { GLAccountId = OffsetGLAccountId!.Value, DebitAmount = amount, Description = Input.Description });
            journal.Lines.Add(new JournalEntryLine { GLAccountId = bankAccount!.GLAccountId!.Value, CreditAmount = amount, Description = Input.Description });
        }

        var entry = await postingService.CreateManualJournalEntryAsync(journal);
        var postErrors = await postingService.PostJournalEntryAsync(entry.Id, User.Identity?.Name ?? "System");
        if (postErrors.Count > 0)
        {
            foreach (var error in postErrors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await LoadAsync();
            return Page();
        }

        Input.SourceModule = SourceModule.Manual;
        Input.SourceReferenceId = entry.Id;
        Input.JournalEntryId = entry.Id;
        context.BankTransactions.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Transactions = await context.BankTransactions.AsNoTracking().Include(transaction => transaction.BankAccount).Include(transaction => transaction.JournalEntry).OrderByDescending(transaction => transaction.TransactionDate).ThenByDescending(transaction => transaction.Id).Take(250).ToListAsync();
        BankAccountOptions = new SelectList(await context.BankAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountName).ToListAsync(), "Id", "AccountName");
        OffsetAccountOptions = new SelectList(await context.GLAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountCode).Select(account => new { account.Id, Name = account.AccountCode + " - " + account.AccountName }).ToListAsync(), "Id", "Name");
    }
}
